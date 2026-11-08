using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Server
{
    public class Main : ServerScript
    {
        public Main()
        {
            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(OnConnecting);
            EventHandlers["playerJoining"] += new Action<Player, string>(OnJoining);
            EventHandlers["playerDropped"] += new Action<Player, string>(OnDropped);
            EventHandlers["onResourceStop"] += new Action<string>(OnStopped);
            EventHandlers["player:spawned"] += new Action<Player>(OnSpawned);
            Tick += Pump;
            API.RegisterCommand("authcapture", new Action<int, List<object>, string>(OnCapture), true);
            API.RegisterCommand("authrecord", new Action<int, List<object>, string>(OnRecord), true);
            API.RegisterCommand("authpass", new Action<int, List<object>, string>(OnPass), true);
            API.RegisterCommand("authresume", new Action<int, List<object>, string>(OnResumeProbe), true);
            API.RegisterCommand("authban", new Action<int, List<object>, string>(OnBanProbe), true);
            API.RegisterCommand("authconnlog", new Action<int, List<object>, string>(OnConnLogProbe), true);
            API.RegisterCommand("authalias", new Action<int, List<object>, string>(OnAliasProbe), true);
            API.RegisterCommand("authwhitelist", new Action<int, List<object>, string>(OnWhitelistProbe), true);
            API.RegisterCommand("authlang", new Action<int, List<object>, string>(OnLangProbe), true);
            API.RegisterCommand("lang", new Action<int, List<object>, string>(OnLang), false);
            Exports.Add("getAccountId", new Func<string, object>(GetAccountId));
            Exports.Add("getAdminLevel", new Func<string, object>(GetAdminLevel));
            Exports.Add("getLanguage", new Func<string, object>(GetLanguage));
            Language.Bind(Translate);
            Exports.Add("setWhitelistEnabled", new Func<bool, bool>(Whitelist.SetEnabled));
            Exports.Add("isWhitelistEnabled", new Func<bool>(() => Whitelist.Enabled));
            Exports.Add("setWhitelistAuto", new Func<bool, bool>(Whitelist.SetAuto));
            Exports.Add("isWhitelistAuto", new Func<bool>(() => Whitelist.Auto));
            Exports.Add("whitelistNameChanged", new Action<string, bool>(Whitelist.NameChanged));
            Whitelist.Bind(ShowCountdown, Tell, Kick, Note);
            Debug.WriteLine("auth server started. bans, license auto-resume, language, the card, then the whitelist.");
        }

        bool whitelistLoaded;

        async Task Pump()
        {
            MainThread.Drain();
            if (!whitelistLoaded)
            {
                whitelistLoaded = true;
                Whitelist.Load();
            }

            Whitelist.Tick(DateTime.UtcNow.Ticks);
            await Delay(50);
        }

        void ShowCountdown(string source, int seconds)
        {
            TriggerClientEvent("whitelistCountdownChanged", source, seconds);
        }

        void Tell(string source, string line)
        {
            TriggerClientEvent("chat:addMessage", source, new Dictionary<string, object>
            {
                { "color", new[] { 255, 255, 0 } },
                { "args", new[] { line } }
            });
        }

        static void Kick(string source, string reason)
        {
            try
            {
                API.DropPlayer(source, reason);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[auth] drop failed: " + ex.GetType().Name);
            }
        }

        static object GetAccountId(string source)
        {
            Session session = Sessions.Get(source);
            if (session == null || !session.HasAccount || session.AccountId <= 0)
                return null;
            return session.AccountId;
        }

        static object GetAdminLevel(string source)
        {
            Session session = Sessions.Get(source);
            if (session == null || !session.HasAccount)
                return null;
            return session.AdminLevel;
        }

        static object GetLanguage(string source)
        {
            Session session = Sessions.Get(source);
            if (session == null || !session.HasAccount || string.IsNullOrEmpty(session.Language))
                return "en";
            return session.Language;
        }

        string Translate(string language, string english)
        {
            if (API.GetResourceState("data") != "started")
                return english;
            try
            {
                object value = Exports["data"].langTextFor(language, english);
                string text = value == null ? "" : Convert.ToString(value, CultureInfo.InvariantCulture);
                return string.IsNullOrEmpty(text) ? english : text;
            }
            catch (Exception)
            {
                return english;
            }
        }

        // FiveM requires one tick after defer before update, presentCard, or done.
        // Identifiers and tokens are read on that tick, once. playerJoining only remaps.
        async void OnConnecting([FromSource] Player player, string name, dynamic setKickReason, dynamic deferrals)
        {
            string source = player.Handle;
            string playerName = name ?? "";
            deferrals.defer();
            try
            {
                await Delay(0);
                Session session = Sessions.Begin(source, playerName);
                if (!Identifier.HasLicense(session.Identifiers))
                {
                    var refused = new Dictionary<string, object>();
                    refused["player"] = source;
                    refused["name"] = playerName;
                    refused["ok"] = false;
                    refused["code"] = "no_license";
                    refused["identifier_count"] = session.Identifiers.Count;
                    refused["token_count"] = session.Tokens.Length;
                    Note("session.refused", refused);
                    Sessions.End(source);
                    deferrals.done("A Rockstar license is required to join.");
                    return;
                }

                Note("session.connected", ConnectedFields(session));
                deferrals.update("Checking your account status...");
                await Delay(0);
                CheckBan(deferrals, source);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[auth] connect failed: " + ex.GetType().Name);
                Sessions.End(source);
                try
                {
                    deferrals.done("Connection closed.");
                }
                catch (Exception)
                {
                }
            }
        }

        void OnCapture(int source, List<object> args, string raw)
        {
            if (args != null && args.Count > 0)
            {
                string id = Convert.ToString(args[0]);
                try
                {
                    string[] tokens;
                    Dictionary<string, string> identifiers = Identifier.Read(id, out tokens);
                    Debug.WriteLine("[auth] capture player " + Identifier.Summary(identifiers, tokens));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[auth] capture player failed: " + ex.GetType().Name);
                }
                return;
            }

            Dictionary<string, string> sample = Identifier.Take(new string[]
            {
                "license:first",
                "license:second",
                "license2:l2",
                "discord:d",
                "fivem:f",
                "steam:s",
                "xbl:x",
                "live:v",
                "ip:203.0.113.8",
                "notatype:no",
                "license:",
                ":bad"
            });
            string[] sampleTokens = Identifier.TakeTokens(new string[] { "tok-a", "tok-a", "", "tok-b" });
            string license;
            sample.TryGetValue("license", out license);
            Debug.WriteLine("[auth] capture sample " + Identifier.Summary(sample, sampleTokens)
                + " license_value=" + (license ?? ""));
        }

        async void OnRecord(int source, List<object> args, string raw)
        {
            string line = null;
            try
            {
                Links.Probe(result => line = result);
                for (int i = 0; i < 80 && line == null; i++)
                    await Delay(100);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[auth] authrecord failed: " + ex.GetType().Name);
                return;
            }

            if (line == null)
            {
                Debug.WriteLine("[auth] authrecord failed step=timeout");
                MySql.Update(
                    "DELETE FROM accounts WHERE name IN ('f016a', 'f016b')",
                    new object[0],
                    new Action<dynamic>(unused => { }));
                return;
            }

            Debug.WriteLine("[auth] authrecord " + line);
        }

        async void OnPass(int source, List<object> args, string raw)
        {
            string line = null;
            try
            {
                Accounts.Probe(result => line = result);
                for (int i = 0; i < 200 && line == null; i++)
                    await Delay(100);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[auth] authpass failed: " + ex.GetType().Name);
                return;
            }

            if (line == null)
            {
                Debug.WriteLine("[auth] authpass failed step=timeout");
                Accounts.Forget("f017-probe");
                MySql.Update(
                    "DELETE FROM accounts WHERE name = ?",
                    new object[] { "f017a" },
                    new Action<dynamic>(unused => { }));
                return;
            }

            Debug.WriteLine("[auth] authpass " + line);
        }

        async void OnResumeProbe(int source, List<object> args, string raw)
        {
            string line = null;
            try
            {
                Accounts.ResumeProbe(result => line = result);
                for (int i = 0; i < 80 && line == null; i++)
                    await Delay(100);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[auth] authresume failed: " + ex.GetType().Name);
                return;
            }

            if (line == null)
            {
                Debug.WriteLine("[auth] authresume failed step=timeout");
                MySql.Update(
                    "DELETE FROM accounts WHERE name IN ('f018a', 'f018b')",
                    new object[0],
                    new Action<dynamic>(unused =>
                    {
                        MySql.Update(
                            "INSERT INTO settings (skey, svalue) VALUES ('auth.license_autologin', '1') " +
                            "ON DUPLICATE KEY UPDATE svalue = '1'",
                            new object[0],
                            new Action<dynamic>(unused2 =>
                            {
                                MySql.Update(
                                    "UPDATE settings SET svalue = '0' WHERE skey = 'player.auto_ip_login'",
                                    new object[0],
                                    new Action<dynamic>(unused3 => { }));
                            }));
                    }));
                return;
            }

            Debug.WriteLine("[auth] authresume " + line);
        }

        async void OnBanProbe(int source, List<object> args, string raw)
        {
            string line = null;
            try
            {
                Bans.Probe(result => line = result);
                for (int i = 0; i < 120 && line == null; i++)
                    await Delay(100);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[auth] authban failed: " + ex.GetType().Name);
                return;
            }

            if (line == null)
            {
                Debug.WriteLine("[auth] authban failed step=timeout");
                MySql.Update(
                    "DELETE FROM staff_actions WHERE target_name IN ('f019a', 'f019b')",
                    new object[0],
                    new Action<dynamic>(unused =>
                    {
                        MySql.Update(
                            "DELETE FROM bans WHERE name IN ('f019a', 'f019b')",
                            new object[0],
                            new Action<dynamic>(unused2 => { }));
                    }));
                return;
            }

            Debug.WriteLine("[auth] authban " + line);
        }

        async void OnWhitelistProbe(int source, List<object> args, string raw)
        {
            string line = null;
            try
            {
                Whitelist.Probe(result => line = result);
                for (int i = 0; i < 80 && line == null; i++)
                    await Delay(100);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[auth] authwhitelist failed: " + ex.GetType().Name);
                return;
            }

            if (line == null)
            {
                Debug.WriteLine("[auth] authwhitelist failed step=timeout");
                MySql.Update(
                    "DELETE FROM whitelist WHERE name IN ('f020a', 'f020b')",
                    new object[0],
                    new Action<dynamic>(unused => { }));
                return;
            }

            Debug.WriteLine("[auth] authwhitelist " + line);
        }

        static Dictionary<string, object> ConnectedFields(Session session)
        {
            var fields = new Dictionary<string, object>();
            fields["player"] = session.Source;
            fields["name"] = session.Name ?? "";
            fields["ok"] = true;
            for (int i = 0; i < Identifier.Types.Length; i++)
            {
                string type = Identifier.Types[i];
                string value;
                if (session.Identifiers.TryGetValue(type, out value) && !string.IsNullOrEmpty(value))
                    fields[type] = value;
            }

            fields["token_count"] = session.Tokens.Length;
            return fields;
        }

        void Note(string kind, Dictionary<string, object> fields)
        {
            Debug.WriteLine("[auth] " + kind + " token_count=" + Field(fields, "token_count")
                + " code=" + Field(fields, "code"));
            if (API.GetResourceState("logs") != "started")
                return;

            try
            {
                dynamic logs = Exports["logs"];
                logs.@event(kind, fields, "info");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[auth] logs export failed: " + ex.GetType().Name);
            }
        }

        static string Field(Dictionary<string, object> fields, string key)
        {
            object value;
            if (fields == null || !fields.TryGetValue(key, out value) || value == null)
                return "";
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        void OnJoining([FromSource] Player player, string oldSource)
        {
            if (!string.IsNullOrEmpty(oldSource))
            {
                Sessions.Remap(oldSource, player.Handle);
                Whitelist.Remap(oldSource, player.Handle);
            }

            Session session = Sessions.Get(player.Handle);
            int left = Whitelist.SecondsLeft(player.Handle, DateTime.UtcNow.Ticks);
            if (left >= 0)
                ShowCountdown(player.Handle, left);
            if (session == null || !session.HasAccount) return;

            player.State.Set("username", session.Username, true);
            player.State.Set("lang", string.IsNullOrEmpty(session.Language) ? "en" : session.Language, true);
            TriggerEvent("playerAuthenticated", player.Handle);
            Debug.WriteLine("(" + player.Handle + ") " + session.Name + " [" + session.Username + "] has logged in.");
        }

        void OnSpawned([FromSource] Player player)
        {
            Session session = Sessions.Get(player.Handle);
            if (session != null) session.Spawned = true;
        }

        void OnDropped([FromSource] Player player, string reason)
        {
            string source = player.Handle;
            Session session = Sessions.Get(source);
            if (session != null)
                Debug.WriteLine("(" + source + ") " + session.Name + " dropped");
            Whitelist.Left(session);
            CloseLog(session);
            Sessions.End(source);
        }

        void OnStopped(string resource)
        {
            if (resource != API.GetCurrentResourceName()) return;
            foreach (Session session in Sessions.Copy())
                CloseLog(session);
        }

        static void CloseLog(Session session)
        {
            if (session == null || session.Left) return;
            session.Left = true;
            if (session.LogId > 0)
                Connections.Close(session.LogId, null);
        }

        void OpenLog(Session session)
        {
            Connections.Open(session.AccountId, session.Username, session.Identifiers, opened =>
            {
                session.LogId = opened;
                if (session.Left)
                    Connections.Close(opened, null);
            }, logId =>
            {
                var fields = new Dictionary<string, object>();
                fields["player"] = session.Source;
                fields["account_id"] = session.AccountId;
                fields["ok"] = logId > 0;
                fields["code"] = logId > 0 ? "logged" : "log_failed";
                fields["log_id"] = logId;
                Note("session.logged", fields);
                if (logId > 0) ReportAliases(session);
            });
        }

        // F022: count and kinds only. Names and identifier values stay out of the log line.
        void ReportAliases(Session session)
        {
            Aliases.Find(session.AccountId, list =>
            {
                if (list.Count == 0) return;
                int banned = 0;
                var kinds = new SortedDictionary<string, bool>();
                foreach (Alias alias in list)
                {
                    if (alias.Banned) banned++;
                    foreach (string kind in alias.Matched.Split(','))
                        if (kind.Length > 0) kinds[kind] = true;
                }

                var fields = new Dictionary<string, object>();
                fields["player"] = session.Source;
                fields["account_id"] = session.AccountId;
                fields["aliases"] = list.Count;
                fields["banned_aliases"] = banned;
                fields["matched"] = string.Join(",", new List<string>(kinds.Keys).ToArray());
                Note("session.aliases", fields);
            });
        }

        async void OnAliasProbe(int source, List<object> args, string raw)
        {
            string line = null;
            try
            {
                Aliases.Probe(result => line = result);
                for (int i = 0; i < 120 && line == null; i++)
                    await Delay(100);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[auth] authalias failed: " + ex.GetType().Name);
                return;
            }

            if (line == null)
            {
                Debug.WriteLine("[auth] authalias failed step=timeout at=" + Aliases.Step);
                return;
            }

            Debug.WriteLine("[auth] authalias " + line);
        }

        async void OnConnLogProbe(int source, List<object> args, string raw)
        {
            string line = null;
            try
            {
                Connections.Probe(result => line = result);
                for (int i = 0; i < 120 && line == null; i++)
                    await Delay(100);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[auth] authconnlog failed: " + ex.GetType().Name);
                return;
            }

            if (line == null)
            {
                Debug.WriteLine("[auth] authconnlog failed step=timeout at=" + Connections.Step);
                MySql.Update(
                    "DELETE FROM connection_log WHERE name = 'f021a'",
                    new object[0],
                    new Action<dynamic>(unused =>
                    {
                        MySql.Update(
                            "DELETE FROM accounts WHERE name = 'f021a'",
                            new object[0],
                            new Action<dynamic>(unused2 => { }));
                    }));
                return;
            }

            Debug.WriteLine("[auth] authconnlog " + line);
        }

        void Continue(dynamic deferrals, string source, AccountResult result, string method)
        {
            Session session = Sessions.Get(source);
            if (session == null)
            {
                try
                {
                    deferrals.done("Connection closed.");
                }
                catch (Exception)
                {
                }

                return;
            }

            string stored = result == null ? null : result.Lang;
            if (Language.NeedsOffer(stored, session.Language))
            {
                session.AwaitingLanguage = true;
                session.PendingId = result.AccountId;
                session.PendingName = result.Name;
                session.PendingAdmin = result.AdminLevel;
                session.PendingMethod = method;
                PresentLanguage(deferrals, source);
                return;
            }

            string known = Language.Normalize(stored);
            if (known.Length > 0)
                session.Language = known;
            else if (string.IsNullOrEmpty(session.Language))
                session.Language = "en";
            Accept(deferrals, source, result.AccountId, result.Name, result.AdminLevel, method);
        }

        void PresentLanguage(dynamic deferrals, string source)
        {
            deferrals.presentCard(Language.Card(), new Action<dynamic, string>((data, raw) =>
            {
                Session session = Sessions.Get(source);
                if (session == null || !session.AwaitingLanguage)
                {
                    Close(deferrals, source, "Connection closed.");
                    return;
                }

                if (data == null)
                {
                    var fields = new Dictionary<string, object>();
                    fields["player"] = source;
                    fields["ok"] = false;
                    fields["code"] = "LEFT";
                    fields["reason"] = "dialogLeft";
                    if (session.PendingId > 0)
                        fields["account_id"] = session.PendingId;
                    Note("session.kicked", fields);
                    Close(deferrals, source, "Left without logging in");
                    return;
                }

                string code = Language.Normalize(CardField(data, "language"));
                if (code.Length == 0)
                {
                    PresentLanguage(deferrals, source);
                    return;
                }

                session.AwaitingLanguage = false;
                session.Language = code;
                session.SaveLanguage = true;
                Accept(deferrals, source, session.PendingId, session.PendingName, session.PendingAdmin, session.PendingMethod);
            }));
        }

        void OnLang(int source, List<object> args, string raw)
        {
            if (source <= 0)
                return;

            string id = source.ToString(CultureInfo.InvariantCulture);
            Session session = Sessions.Get(id);
            string current = session != null && !string.IsNullOrEmpty(session.Language) ? session.Language : "en";
            if (session == null || !session.HasAccount)
            {
                Tell(id, " >  " + Language.Sentence(current, "Log in first."));
                return;
            }

            string given = args != null && args.Count > 0 ? Convert.ToString(args[0], CultureInfo.InvariantCulture) : "";
            string code = Language.Normalize(given);
            if (code.Length == 0)
            {
                Tell(id, " >  " + Language.Format(current, "Usage: /lang <%s> (now %s)", Language.List, current));
                return;
            }

            session.Language = code;
            Language.Save(session.AccountId, code, true, null);
            PublishLang(id, code);
            Tell(id, " >  " + Language.Format(code, "Language set to %s.", code));
            var fields = new Dictionary<string, object>();
            fields["player"] = id;
            fields["account_id"] = session.AccountId;
            fields["lang"] = code;
            fields["via"] = "command";
            Note("session.languageSet", fields);
        }

        void PublishLang(string source, string code)
        {
            try
            {
                Player player = Players[source];
                if (player != null)
                    player.State.Set("lang", code, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[auth] lang state failed: " + ex.GetType().Name);
            }
        }

        async void OnLangProbe(int source, List<object> args, string raw)
        {
            string line = null;
            try
            {
                Language.Probe(result => line = result);
                for (int i = 0; i < 120 && line == null; i++)
                    await Delay(100);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[auth] authlang failed: " + ex.GetType().Name);
                return;
            }

            if (line == null)
            {
                Debug.WriteLine("[auth] authlang failed step=timeout at=" + Language.Step);
                MySql.Update(
                    "DELETE FROM accounts WHERE name IN ('f023a', 'f023b')",
                    new object[0],
                    new Action<dynamic>(unused => { }));
                return;
            }

            Debug.WriteLine("[auth] authlang " + line);
        }

        void Present(dynamic deferrals, string source, string message, string name)
        {
            deferrals.presentCard(Card(message, name), new Action<dynamic, string>((data, raw) =>
            {
                if (data == null)
                {
                    Close(deferrals, source, "Connection closed.");
                    return;
                }

                string action = CardField(data, "action");
                string nextName = CardField(data, "name");
                string nextPass = CardField(data, "password");
                if (action != "login" && action != "register")
                {
                    Close(deferrals, source, "Connection closed.");
                    return;
                }

                if (string.IsNullOrEmpty(nextName) || string.IsNullOrEmpty(nextPass))
                {
                    Present(deferrals, source, "Name and password cannot be empty.", nextName);
                    return;
                }

                try
                {
                    deferrals.update("Checking your account...");
                }
                catch (Exception)
                {
                }

                Func<bool> present = () => Sessions.Get(source) != null;
                Action<AccountResult> after = result => OnAccount(deferrals, source, action, nextName, result);
                if (action == "login")
                {
                    Session session = Sessions.Get(source);
                    string license = null;
                    if (session != null && session.Identifiers != null)
                        session.Identifiers.TryGetValue("license", out license);
                    Accounts.Login(license ?? source, nextName, nextPass, IpOf(session), present, after);
                }
                else
                {
                    Session session = Sessions.Get(source);
                    Accounts.Register(nextName, nextPass, IpOf(session), present, after);
                }
            }));
        }

        void OnAccount(dynamic deferrals, string source, string action, string name, AccountResult result)
        {
            if (Sessions.Get(source) == null)
            {
                try
                {
                    deferrals.done("Connection closed.");
                }
                catch (Exception)
                {
                }

                return;
            }

            string code = result == null ? "DB_ERROR" : result.Code;
            if (result != null && result.Ok && code == "OK")
            {
                Continue(deferrals, source, result, action);
                return;
            }

            var fields = new Dictionary<string, object>();
            fields["player"] = source;
            fields["ok"] = false;
            fields["code"] = code ?? "DB_ERROR";
            fields["name"] = name ?? "";
            if (result != null && result.AccountId > 0)
                fields["account_id"] = result.AccountId;
            if (result != null && result.Attempts > 0)
                fields["attempts"] = result.Attempts;

            if (code == "BAD_PASSWORD" && result.Attempts < Accounts.MaxAttempts)
            {
                Note("session.refused", fields);
                Present(deferrals, source, "Incorrect password! " + result.Attempts.ToString(CultureInfo.InvariantCulture) + " out of 5 tries", name);
                return;
            }

            if (code == "BAD_PASSWORD" || code == "TOO_MANY_ATTEMPTS")
            {
                fields["reason"] = "dialogLeft";
                Note("session.kicked", fields);
                Close(deferrals, source, "Left without logging in");
                return;
            }

            if (code == "INACTIVE")
            {
                fields["reason"] = "inactive";
                Note("session.refused", fields);
                Close(deferrals, source, "Account inactive");
                return;
            }

            if (code == "DB_ERROR" || code == "GONE")
            {
                fields["reason"] = "accountLoadFailed";
                Note("session.kicked", fields);
                Close(deferrals, source, "Account load failed");
                return;
            }

            Note("session.refused", fields);
            Present(deferrals, source, CardMessage(code), name);
        }

        static string CardMessage(string code)
        {
            if (code == "BAD_NAME")
                return "Name must be 1 to 22 letters, digits, or punctuation.";
            if (code == "WEAK_PASSWORD")
                return "Password must be between 4 and 32 characters.";
            if (code == "EXISTS")
                return "This name is already registered.";
            if (code == "NO_ACCOUNT")
                return "Account not found.";
            return "Account load failed";
        }

        void CheckBan(dynamic deferrals, string source)
        {
            Session session = Sessions.Get(source);
            if (session == null)
            {
                Close(deferrals, source, "Connection closed.");
                return;
            }

            Bans.Match(session.Identifiers, session.Tokens, hit => OnBan(deferrals, source, hit));
        }

        void OnBan(dynamic deferrals, string source, BanHit hit)
        {
            Session session = Sessions.Get(source);
            if (session == null)
            {
                try
                {
                    deferrals.done("Connection closed.");
                }
                catch (Exception)
                {
                }

                return;
            }

            string code = hit == null ? "DB_ERROR" : hit.Code;
            if (code == "DB_ERROR")
            {
                var fields = new Dictionary<string, object>();
                fields["player"] = source;
                fields["ok"] = false;
                fields["code"] = "DB_ERROR";
                fields["reason"] = "banLookupFailed";
                Note("session.kicked", fields);
                Close(deferrals, source, "Account load failed");
                return;
            }

            if (hit != null && hit.Banned)
            {
                var fields = ConnectedFields(session);
                fields["ok"] = false;
                fields["code"] = "BANNED";
                fields["ban_id"] = hit.BanId;
                fields["ban_reason"] = hit.Reason ?? "";
                fields["by_name"] = hit.ByName ?? "Server";
                fields["matched_type"] = hit.MatchedType ?? "";
                fields["created"] = hit.Created ?? "";
                fields["expires"] = string.IsNullOrEmpty(hit.Expires) ? "Never" : hit.Expires;
                if (hit.AccountId > 0)
                    fields["account_id"] = hit.AccountId;
                Note("session.banned", fields);
                Close(deferrals, source, Bans.Message(hit));
                return;
            }

            TryResume(deferrals, source);
        }

        void TryResume(dynamic deferrals, string source)
        {
            Session session = Sessions.Get(source);
            if (session == null)
            {
                Close(deferrals, source, "Connection closed.");
                return;
            }

            string license = null;
            if (session.Identifiers != null)
                session.Identifiers.TryGetValue("license", out license);

            Accounts.Resume(license, IpOf(session), () => Sessions.Get(source) != null, result =>
            {
                OnResume(deferrals, source, result);
            });
        }

        void OnResume(dynamic deferrals, string source, AccountResult result)
        {
            if (Sessions.Get(source) == null)
            {
                try
                {
                    deferrals.done("Connection closed.");
                }
                catch (Exception)
                {
                }

                return;
            }

            string code = result == null ? "DB_ERROR" : result.Code;
            if (result != null && result.Ok && code == "OK")
            {
                Continue(deferrals, source, result, "license");
                return;
            }

            var fields = new Dictionary<string, object>();
            fields["player"] = source;
            fields["ok"] = false;
            fields["code"] = code ?? "DB_ERROR";
            if (result != null && result.AccountId > 0)
                fields["account_id"] = result.AccountId;

            if (code == "INACTIVE")
            {
                fields["reason"] = "inactive";
                Note("session.refused", fields);
                Close(deferrals, source, "Account inactive");
                return;
            }

            if (code == "DB_ERROR" || code == "GONE")
            {
                fields["reason"] = "accountLoadFailed";
                Note("session.kicked", fields);
                Close(deferrals, source, "Account load failed");
                return;
            }

            Present(deferrals, source, null, null);
        }

        static string IpOf(Session session)
        {
            if (session == null || session.Identifiers == null)
                return null;
            string ip;
            session.Identifiers.TryGetValue("ip", out ip);
            return ip;
        }

        static string CardField(dynamic data, string key)
        {
            if (data == null || key == null)
                return "";

            IDictionary<string, object> dict = data as IDictionary<string, object>;
            if (dict != null)
            {
                object value;
                if (!dict.TryGetValue(key, out value) || value == null)
                    return "";
                return Convert.ToString(value);
            }

            try
            {
                object value = data[key];
                return value == null ? "" : Convert.ToString(value);
            }
            catch (Exception)
            {
                return "";
            }
        }

        void Close(dynamic deferrals, string source, string reason)
        {
            Sessions.End(source);
            try
            {
                deferrals.done(reason);
            }
            catch (Exception)
            {
            }
        }

        void Accept(dynamic deferrals, string source, int id, string username, int adminLevel, string method)
        {
            Session session = Sessions.Get(source);
            if (session == null)
            {
                try
                {
                    deferrals.done("Connection closed.");
                }
                catch (Exception)
                {
                }

                return;
            }

            Links.Record(id, session.Identifiers, session.Tokens, new Action<RecordResult>(result =>
            {
                var fields = new Dictionary<string, object>();
                fields["player"] = source;
                fields["account_id"] = id;
                fields["identifier_count"] = result == null ? 0 : result.IdentifierCount;
                fields["token_count"] = result == null ? 0 : result.TokenCount;
                fields["taken_count"] = result == null ? 0 : result.TakenCount;
                if (result == null || !result.Ok)
                {
                    fields["ok"] = false;
                    fields["code"] = "write_failed";
                    Note("session.recorded", fields);
                    Present(deferrals, source, "Could not store this sign-in. Try again.", username);
                    return;
                }

                fields["ok"] = true;
                fields["code"] = "recorded";
                Note("session.recorded", fields);
                Whitelist.Allow(username, adminLevel, allowed =>
                {
                    if (Sessions.Get(source) == null)
                    {
                        try
                        {
                            deferrals.done("Connection closed.");
                        }
                        catch (Exception)
                        {
                        }

                        return;
                    }

                    if (!allowed)
                    {
                        var refused = new Dictionary<string, object>();
                        refused["player"] = source;
                        refused["account_id"] = id;
                        refused["name"] = username ?? "";
                        refused["ok"] = false;
                        refused["code"] = "NOT_WHITELISTED";
                        Note("session.refused", refused);
                        Close(deferrals, source, Whitelist.RefuseText);
                        return;
                    }

                    session.HasAccount = true;
                    session.AccountId = id;
                    session.Username = username;
                    session.AdminLevel = adminLevel;
                    if (string.IsNullOrEmpty(session.Language))
                        session.Language = "en";
                    if (session.SaveLanguage)
                    {
                        session.SaveLanguage = false;
                        string picked = session.Language;
                        Language.Save(id, picked, false, null);
                        var chosen = new Dictionary<string, object>();
                        chosen["player"] = source;
                        chosen["account_id"] = id;
                        chosen["lang"] = picked;
                        chosen["via"] = "dialog";
                        Note("session.languageSet", chosen);
                    }
                    var authed = new Dictionary<string, object>();
                    authed["player"] = source;
                    authed["account_id"] = id;
                    authed["name"] = username ?? "";
                    authed["method"] = method == "register" || method == "license" ? method : "login";
                    authed["ok"] = true;
                    authed["code"] = "OK";
                    Note("session.authenticated", authed);
                    deferrals.done();
                    Whitelist.Joined(session);
                    OpenLog(session);
                });
            }));
        }

        static string Card(string message, string name)
        {
            var body = new StringBuilder();
            body.Append("{\"type\":\"AdaptiveCard\",\"version\":\"1.3\",\"body\":[");
            body.Append("{\"type\":\"TextBlock\",\"text\":\"Scavenge and Survive\",\"weight\":\"Bolder\",\"size\":\"Large\"},");
            body.Append("{\"type\":\"TextBlock\",\"wrap\":true,\"text\":\"Welcome to a world where survival is the only thing that matters. Scavenge for resources, build your shelter, and fight for your life in a post-apocalyptic world.\"},");
            body.Append("{\"type\":\"TextBlock\",\"wrap\":true");
            if (!string.IsNullOrEmpty(message)) body.Append(",\"color\":\"Attention\"");
            body.Append(",\"text\":").Append(Json(message ?? "Please login or register to continue.")).Append("},");
            body.Append("{\"type\":\"Input.Text\",\"id\":\"name\",\"label\":\"Name\",\"placeholder\":\"Enter your name\",\"value\":").Append(Json(name ?? "")).Append("},");
            body.Append("{\"type\":\"Input.Text\",\"id\":\"password\",\"label\":\"Password\",\"placeholder\":\"Enter your password\",\"style\":\"password\"}");
            body.Append("],\"actions\":[");
            body.Append("{\"type\":\"Action.Submit\",\"title\":\"Login\",\"data\":{\"action\":\"login\"}},");
            body.Append("{\"type\":\"Action.Submit\",\"title\":\"Register\",\"data\":{\"action\":\"register\"}}");
            body.Append("]}");
            return body.ToString();
        }

        static string Json(string value)
        {
            if (value == null) return "\"\"";
            var escaped = new StringBuilder(value.Length + 2);
            escaped.Append('"');
            foreach (char c in value)
            {
                if (c == '\\' || c == '"') escaped.Append('\\').Append(c);
                else if (c == '\n') escaped.Append("\\n");
                else if (c == '\r') escaped.Append("\\r");
                else if (c == '\t') escaped.Append("\\t");
                else escaped.Append(c);
            }
            escaped.Append('"');
            return escaped.ToString();
        }
    }
}
