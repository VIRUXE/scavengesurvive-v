using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Shared;

namespace Server
{
    public class Main : ServerScript
    {
        const int HeartbeatMs = 60000;
        const int CallWindowMs = 300000;
        const int HttpTimeoutMs = 30000;
        const int EntityAlarm = 1500;

        readonly List<LokiBody.Entry> _buf = new List<LokiBody.Entry>();
        readonly Dictionary<int, string> _http = new Dictionary<int, string>();
        readonly List<Sample> _calls = new List<Sample>();
        readonly Dictionary<string, long> _alertAt = new Dictionary<string, long>();

        string _level = "info";
        string _url = "";
        string _tenant = "";
        string _user = "";
        string _pass = "";
        string _bearer = "";
        string _host = "fivem";
        string _env = "dev";
        string _webhook = "";
        int _flushMs = 1000;
        int _maxLines = 20000;
        int _slowMs = 50;
        bool _push;

        int _seq;
        int _dropped;
        int _backoffS;
        long _retryAt;
        long _outageSince;
        long _lastPushReport;
        long _started;
        long _nextHeart;
        int _lokiId;
        long _lokiSince;
        List<LokiBody.Entry> _lokiEntries;

        struct Sample
        {
            public long At;
            public double Ms;
        }

        public Main()
        {
            Exports.Add("event", new Action<string, object, string>(OnEvent));
            EventHandlers["__cfx_internal:httpResponse"] += new Action<object, object, object, object, object>(OnHttp);
            EventHandlers["onResourceStart"] += new Action<string>(OnResourceStart);
            EventHandlers["onResourceStop"] += new Action<string>(OnResourceStop);
            ReadConfig();
            long now = NowMs();
            _started = now;
            _nextHeart = now + HeartbeatMs;
            Tick += Loop;
            var fields = new Dictionary<string, object>();
            fields["name"] = API.GetCurrentResourceName();
            Emit("system.resourceStarted", fields, "info");
            Debug.WriteLine("logs server started. stdout json. loki " + LokiState() + ". webhook " + (_webhook.Length > 0 ? "on" : "off") + ". level " + _level + ".");
        }

        async Task Loop()
        {
            while (true)
            {
                await Delay(_flushMs);
                try
                {
                    Flush(false);
                    long now = NowMs();
                    if (now >= _nextHeart)
                    {
                        _nextHeart = now + HeartbeatMs;
                        Heartbeat(now);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[logs] tick failed: " + ex.GetType().Name);
                }
            }
        }

        void OnEvent(string kind, object fields, string level)
        {
            try
            {
                Accept(kind, fields, level, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[logs] event failed: " + ex.GetType().Name);
            }
        }

        void OnResourceStart(string name)
        {
            if (string.IsNullOrEmpty(name) || name == API.GetCurrentResourceName())
            {
                return;
            }

            var fields = new Dictionary<string, object>();
            fields["name"] = name;
            Emit("system.resourceStarted", fields, "info");
        }

        void OnResourceStop(string name)
        {
            if (name == API.GetCurrentResourceName())
            {
                Flush(true);
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var fields = new Dictionary<string, object>();
            fields["name"] = name;
            Emit("system.resourceStopped", fields, "info");
        }

        void Emit(string kind, IDictionary<string, object> fields, string level)
        {
            Accept(kind, fields, level, "logs");
        }

        void Accept(string kind, object fields, string level, string resource)
        {
            if (string.IsNullOrEmpty(kind))
            {
                return;
            }

            ReadConfig();
            level = LogPolicy.NormalizeLevel(level);
            Dictionary<string, object> raw = Coerce(fields);
            ExpandPlayers(raw);
            if (kind == "db.callFinished")
            {
                NoteCall(raw);
            }

            if (LogPolicy.Rank(level) < LogPolicy.Rank(_level))
            {
                return;
            }

            Dictionary<string, object> safe = LogPolicy.Fields(kind, raw);
            if (string.IsNullOrEmpty(resource))
            {
                resource = API.GetInvokingResource();
                if (string.IsNullOrEmpty(resource))
                {
                    resource = "logs";
                }
            }

            string line = LogShape.Build(level, resource, kind, safe);
            Debug.WriteLine(line);
            if (_push)
            {
                Enqueue(level, resource, kind, line, Meta(safe));
            }

            if (kind == "db.connectFailed" || kind == "db.queryFailed")
            {
                Alert("database", kind, 60000);
            }
        }

        // auth.getAccountId and character.getCharacterId are not in yet.
        // A numeric server id still becomes the player name when the ped has one.
        void ExpandPlayers(Dictionary<string, object> fields)
        {
            var keys = new List<string>(fields.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                if (!LogPolicy.IsPlayerKey(key))
                {
                    continue;
                }

                string serverId;
                string knownName;
                if (!TryPlayer(fields[key], out serverId, out knownName))
                {
                    continue;
                }

                string name = knownName;
                if (string.IsNullOrEmpty(name) && serverId != null)
                {
                    name = API.GetPlayerName(serverId);
                }

                if (!string.IsNullOrEmpty(name) && name != "**Invalid**")
                {
                    fields[key] = name;
                }
                else if (serverId != null)
                {
                    fields[key] = serverId;
                }

                if (serverId == null)
                {
                    continue;
                }

                string prefix = key.ToLowerInvariant() == "player" ? "" : key.ToLowerInvariant() + "_";
                PutIfEmpty(fields, prefix + "account_id", Ask("auth", "getAccountId", serverId));
                PutIfEmpty(fields, prefix + "character_id", Ask("character", "getCharacterId", serverId));
            }
        }

        static bool TryPlayer(object value, out string serverId, out string name)
        {
            serverId = null;
            name = null;
            var player = value as Player;
            if (player != null)
            {
                serverId = player.Handle;
                name = player.Name;
                return !string.IsNullOrEmpty(serverId) || !string.IsNullOrEmpty(name);
            }

            if (value is string)
            {
                string text = (string)value;
                int n;
                if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out n) && n >= 0)
                {
                    serverId = n.ToString(CultureInfo.InvariantCulture);
                    return true;
                }

                return false;
            }

            if (value is int || value is long || value is short || value is byte || value is uint)
            {
                long n = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                if (n < 0)
                {
                    return false;
                }

                serverId = n.ToString(CultureInfo.InvariantCulture);
                return true;
            }

            if (value is double || value is float || value is decimal)
            {
                double n = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (n < 0 || n != Math.Floor(n) || n > int.MaxValue)
                {
                    return false;
                }

                serverId = ((long)n).ToString(CultureInfo.InvariantCulture);
                return true;
            }

            return false;
        }

        object Ask(string resource, string exportName, string serverId)
        {
            if (API.GetResourceState(resource) != "started")
            {
                return null;
            }

            // Missing exports throw. Auth and character do not publish these yet.
            try
            {
                if (exportName == "getAccountId")
                {
                    return Exports[resource].getAccountId(serverId);
                }

                return Exports[resource].getCharacterId(serverId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        static void PutIfEmpty(Dictionary<string, object> fields, string key, object value)
        {
            if (fields.ContainsKey(key) || value == null || value is bool)
            {
                return;
            }

            string text = value as string;
            if (text != null && text.Length == 0)
            {
                return;
            }

            fields[key] = value;
        }

        void NoteCall(Dictionary<string, object> fields)
        {
            object raw;
            if (!fields.TryGetValue("ms", out raw))
            {
                return;
            }

            double ms;
            if (!TryDouble(raw, out ms))
            {
                return;
            }

            _calls.Add(new Sample { At = NowMs(), Ms = ms });
            TrimCalls(NowMs());
        }

        void Heartbeat(long now)
        {
            int peds = CountPool(SafePool(API.GetAllPeds));
            int vehicles = CountPool(SafePool(API.GetAllVehicles));
            int objects = CountPool(SafePool(API.GetAllObjects));
            int entities = peds + vehicles + objects;
            var fields = new Dictionary<string, object>();
            fields["players"] = API.GetNumPlayerIndices();
            fields["peds"] = peds;
            fields["vehicles"] = vehicles;
            fields["objects"] = objects;
            fields["entities"] = entities;
            fields["queued_lines"] = _buf.Count;
            fields["uptime_s"] = (now - _started) / 1000;
            Emit("system.heartbeat", fields, "info");
            if (entities > EntityAlarm)
            {
                Alert("entities", "networked entities " + entities.ToString(CultureInfo.InvariantCulture) + " over " + EntityAlarm.ToString(CultureInfo.InvariantCulture), CallWindowMs);
            }

            double p95;
            if (OverP95(now, out p95))
            {
                Alert("db-p95", "db.callFinished p95 " + p95.ToString("0.#", CultureInfo.InvariantCulture) + " ms over " + _slowMs.ToString(CultureInfo.InvariantCulture), CallWindowMs);
            }
        }

        bool OverP95(long now, out double p95)
        {
            p95 = 0;
            TrimCalls(now);
            if (_calls.Count < 2)
            {
                return false;
            }

            long span = _calls[_calls.Count - 1].At - _calls[0].At;
            if (span < CallWindowMs)
            {
                return false;
            }

            var values = new double[_calls.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = _calls[i].Ms;
            }

            Array.Sort(values);
            int index = (int)Math.Ceiling(values.Length * 0.95) - 1;
            if (index < 0)
            {
                index = 0;
            }

            if (index >= values.Length)
            {
                index = values.Length - 1;
            }

            p95 = values[index];
            return p95 > _slowMs;
        }

        void TrimCalls(long now)
        {
            long cut = now - CallWindowMs;
            int drop = 0;
            while (drop < _calls.Count && _calls[drop].At < cut)
            {
                drop++;
            }

            if (drop > 0)
            {
                _calls.RemoveRange(0, drop);
            }

            if (_calls.Count > 4000)
            {
                _calls.RemoveRange(0, _calls.Count - 4000);
            }
        }

        void Alert(string name, string text, int everyMs)
        {
            long now = NowMs();
            long last;
            if (_alertAt.TryGetValue(name, out last) && now - last < everyMs)
            {
                return;
            }

            _alertAt[name] = now;
            var fields = new Dictionary<string, object>();
            fields["alert"] = name;
            fields["msg"] = text;
            Emit("system.alertRaised", fields, "warn");
            if (_webhook.Length == 0)
            {
                return;
            }

            string body = "{\"content\":" + LokiBody.Quote(text) + "}";
            var headers = new Dictionary<string, string>();
            headers["Content-Type"] = "application/json";
            int id = Post(_webhook, body, headers);
            if (id == -1)
            {
                Debug.WriteLine("[logs] webhook post failed");
                return;
            }

            _http[id] = "webhook";
        }

        void Enqueue(string level, string resource, string kind, string line, Dictionary<string, string> meta)
        {
            _seq++;
            _buf.Add(new LokiBody.Entry
            {
                Ns = LokiBody.Timestamp(NowMs(), _seq),
                Line = line,
                Level = level,
                Resource = resource,
                Kind = kind,
                Meta = meta,
            });
            int over = _buf.Count - _maxLines;
            if (over > 0)
            {
                _buf.RemoveRange(0, over);
                _dropped += over;
            }

            if (_buf.Count >= LokiBody.BatchSize)
            {
                Flush(false);
            }
        }

        // A failed push is console text only. A JSON line would re-enter the buffer.
        void Flush(bool force)
        {
            ReadConfig();
            long now = NowMs();
            if (_lokiId != 0 && (force || now - _lokiSince >= HttpTimeoutMs))
            {
                _http.Remove(_lokiId);
                _lokiId = 0;
                List<LokiBody.Entry> timedOut = _lokiEntries;
                _lokiEntries = null;
                if (timedOut != null)
                {
                    OnLoki(timedOut, 0);
                    now = NowMs();
                }
            }

            if (!_push || _buf.Count == 0)
            {
                return;
            }

            if (!force && (_lokiId != 0 || now < _retryAt))
            {
                return;
            }

            int n = _buf.Count < LokiBody.BatchSize ? _buf.Count : LokiBody.BatchSize;
            List<LokiBody.Entry> entries = _buf.GetRange(0, n);
            _buf.RemoveRange(0, n);
            string body = LokiBody.Body(entries, _host, _env);
            int id = Post(_url + "/loki/api/v1/push", body, LokiBody.Headers(_bearer, _user, _pass, _tenant));
            if (id == -1)
            {
                OnLoki(entries, 0);
                return;
            }

            _lokiId = id;
            _lokiEntries = entries;
            _lokiSince = now;
            _http[id] = "loki";
        }

        void OnHttp(object token, object status, object body, object headers, object errorData)
        {
            int id = ToInt(token);
            string purpose;
            if (!_http.TryGetValue(id, out purpose))
            {
                return;
            }

            _http.Remove(id);
            if (purpose != "loki")
            {
                return;
            }

            List<LokiBody.Entry> entries = _lokiEntries;
            _lokiId = 0;
            _lokiEntries = null;
            if (entries == null)
            {
                return;
            }

            OnLoki(entries, ToInt(status));
        }

        void OnLoki(List<LokiBody.Entry> entries, int status)
        {
            string verdict = LokiBody.Classify(status);
            if (verdict == "ok")
            {
                long now = NowMs();
                int outageS = _outageSince > 0 ? (int)((now - _outageSince) / 1000) : 0;
                bool wasDown = _outageSince > 0;
                _backoffS = 0;
                _retryAt = 0;
                _outageSince = 0;
                if (wasDown)
                {
                    Debug.WriteLine("[logs] push recovered after " + outageS.ToString(CultureInfo.InvariantCulture) + " s");
                }

                if (_dropped > 0)
                {
                    int dropped = _dropped;
                    _dropped = 0;
                    var fields = new Dictionary<string, object>();
                    fields["dropped"] = dropped;
                    fields["outage_s"] = outageS;
                    Emit("system.logDropped", fields, "warn");
                }

                if (_buf.Count >= LokiBody.BatchSize)
                {
                    Flush(false);
                }

                return;
            }

            if (verdict == "drop")
            {
                Debug.WriteLine("[logs] Loki refused a batch of " + entries.Count.ToString(CultureInfo.InvariantCulture) + " lines (HTTP " + status.ToString(CultureInfo.InvariantCulture) + "), dropped");
                return;
            }

            _buf.InsertRange(0, entries);
            long tick = NowMs();
            if (_outageSince == 0)
            {
                _outageSince = tick;
            }

            _backoffS = verdict == "config" ? LokiBody.BackoffMaxS : LokiBody.NextBackoff(_backoffS);
            _retryAt = tick + (_backoffS * 1000L);
            if (tick - _lastPushReport >= HeartbeatMs)
            {
                _lastPushReport = tick;
                string why = status > 0 ? "HTTP " + status.ToString(CultureInfo.InvariantCulture) : "no connection";
                Debug.WriteLine("[logs] push failed (" + why + "), " + _buf.Count.ToString(CultureInfo.InvariantCulture) + " lines queued, retry in " + _backoffS.ToString(CultureInfo.InvariantCulture) + " s");
            }
        }

        int Post(string url, string body, Dictionary<string, string> headers)
        {
            try
            {
                var headerObj = new Dictionary<string, object>();
                foreach (var pair in headers)
                {
                    headerObj[pair.Key] = pair.Value;
                }

                var request = new Dictionary<string, object>();
                request["url"] = url;
                request["method"] = "POST";
                request["data"] = body ?? "";
                request["headers"] = headerObj;
                request["followLocation"] = true;
                return API.PerformHttpRequestInternalEx(request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[logs] http request failed: " + ex.GetType().Name);
                return -1;
            }
        }

        void ReadConfig()
        {
            _level = LogPolicy.NormalizeLevel(API.GetConvar("logs_level", "info"));
            _url = (API.GetConvar("logs_url", "") ?? "").Trim().TrimEnd('/');
            _tenant = API.GetConvar("logs_tenant", "") ?? "";
            _user = API.GetConvar("logs_user", "") ?? "";
            _pass = API.GetConvar("logs_pass", "") ?? "";
            _bearer = API.GetConvar("logs_bearer", "") ?? "";
            _host = API.GetConvar("logs_host", "fivem");
            if (string.IsNullOrEmpty(_host))
            {
                _host = "fivem";
            }

            _env = API.GetConvar("logs_env", "dev");
            if (string.IsNullOrEmpty(_env))
            {
                _env = "dev";
            }

            _webhook = (API.GetConvar("logs_webhook", "") ?? "").Trim();
            _flushMs = Clamp(API.GetConvarInt("logs_flush_ms", 1000), 200, 60000);
            _maxLines = Clamp(API.GetConvarInt("logs_max_lines", 20000), 500, 100000);
            _slowMs = Clamp(API.GetConvarInt("logs_slow_ms", 50), 1, 60000);
            _push = _url.StartsWith("http://") || _url.StartsWith("https://");
            if (!_webhook.StartsWith("http://") && !_webhook.StartsWith("https://"))
            {
                _webhook = "";
            }
        }

        string LokiState()
        {
            if (!_push)
            {
                return "off";
            }

            return "on, " + LokiBody.DescribeAuth(_bearer, _user, _pass, _tenant);
        }

        static Dictionary<string, string> Meta(Dictionary<string, object> fields)
        {
            var meta = new Dictionary<string, string>();
            foreach (var pair in fields)
            {
                if (!LogShape.IsMetaKey(pair.Key) || pair.Value == null)
                {
                    continue;
                }

                string text = Convert.ToString(pair.Value, CultureInfo.InvariantCulture);
                if (text.Length > LogShape.MaxString)
                {
                    text = text.Substring(0, LogShape.MaxString);
                }

                meta[pair.Key] = text;
            }

            return meta;
        }

        static Dictionary<string, object> Coerce(object fields)
        {
            var raw = new Dictionary<string, object>();
            if (fields == null)
            {
                return raw;
            }

            var map = fields as IDictionary<string, object>;
            if (map != null)
            {
                foreach (var pair in map)
                {
                    if (pair.Key != null)
                    {
                        raw[pair.Key] = pair.Value;
                    }
                }

                return raw;
            }

            var untyped = fields as IDictionary;
            if (untyped == null)
            {
                return raw;
            }

            foreach (DictionaryEntry entry in untyped)
            {
                string key = entry.Key as string;
                if (key != null)
                {
                    raw[key] = entry.Value;
                }
            }

            return raw;
        }

        static object SafePool(Func<object> read)
        {
            try
            {
                return read();
            }
            catch (Exception)
            {
                return null;
            }
        }

        static int CountPool(object pool)
        {
            if (pool == null)
            {
                return 0;
            }

            var collection = pool as ICollection;
            if (collection != null)
            {
                return collection.Count;
            }

            var enumerable = pool as IEnumerable;
            if (enumerable == null)
            {
                return 0;
            }

            int count = 0;
            foreach (object ignored in enumerable)
            {
                count++;
            }

            return count;
        }

        static bool TryDouble(object value, out double number)
        {
            number = 0;
            if (value == null || value is bool)
            {
                return false;
            }

            try
            {
                number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return false;
            }

            return !double.IsNaN(number) && !double.IsInfinity(number);
        }

        static int ToInt(object value)
        {
            if (value == null || value is bool)
            {
                return 0;
            }

            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        static long NowMs()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }
    }
}
