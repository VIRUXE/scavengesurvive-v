using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Server
{
    static class Whitelist
    {
        public const int DefaultKickSeconds = 300;
        public const int AutoLevel = 2;
        public const string RefuseText = "Whitelist active.\nYou are not in the whitelist.";
        public const string KickReason = "Not in whitelist";
        public const string ActiveLine = " >  Whitelist active.";
        public const string MissingLine = " >  You are not in the whitelist.";
        public const string AutoOffLine = " >  Auto-whitelist: Deactivated the whitelist.";

        const string SettingsSql =
            "SELECT skey, svalue FROM settings WHERE skey IN " +
            "('server.whitelist', 'server.whitelist_auto', 'server.whitelist_kick_s')";

        const string OneSql = "SELECT name FROM whitelist WHERE name = ? LIMIT 1";

        const string ListedSql =
            "SELECT w.name AS name FROM JSON_TABLE(?, '$[*]' COLUMNS (name VARCHAR(24) PATH '$')) jt " +
            "INNER JOIN whitelist w ON w.name = jt.name";

        const string DeleteProbeSql = "DELETE FROM whitelist WHERE name IN ('f020a', 'f020b')";

        const string InsertProbeSql = "INSERT INTO whitelist (name, added_by) VALUES ('f020a', 'Server')";

        const string CountProbeSql =
            "SELECT COUNT(*) AS n FROM whitelist WHERE name IN ('f020a', 'f020b')";

        static readonly Dictionary<string, long> Ends = new Dictionary<string, long>();
        static readonly List<Action> Waiting = new List<Action>();

        static bool ready;
        static Action<string, int> showCountdown;
        static Action<string, string> tell;
        static Action<string, string> drop;
        static Action<string, Dictionary<string, object>> note;

        public static bool Enabled { get; private set; }
        public static bool Auto { get; private set; }
        public static int KickSeconds { get; private set; }

        public static void Bind(
            Action<string, int> countdown,
            Action<string, string> chat,
            Action<string, string> kick,
            Action<string, Dictionary<string, object>> log)
        {
            showCountdown = countdown;
            tell = chat;
            drop = kick;
            note = log;
        }

        public static void Load()
        {
            MySql.Query(SettingsSql, new Action<dynamic>(result =>
            {
                object boxed = result;
                string whitelist = null;
                string auto = null;
                string kick = null;
                EachRow(boxed, row =>
                {
                    string key = MySql.Text(row, "skey");
                    string value = MySql.Text(row, "svalue");
                    if (key == "server.whitelist")
                        whitelist = value;
                    else if (key == "server.whitelist_auto")
                        auto = value;
                    else if (key == "server.whitelist_kick_s")
                        kick = value;
                });

                Enabled = EnabledFrom(whitelist);
                Auto = EnabledFrom(auto);
                KickSeconds = KickSecondsFrom(kick);
                ready = true;
                ApplyAuto(StaffCount(null), null);
                if (Enabled)
                    StartCountdowns();
                Drain();
            }));
        }

        public static bool Allows(bool enabled, bool listed, int adminLevel)
        {
            if (!enabled)
                return true;
            return listed || adminLevel > 0;
        }

        public static bool EnabledFrom(string value)
        {
            return value == "1" || value == "true" || value == "on";
        }

        public static int KickSecondsFrom(string value)
        {
            int parsed;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                parsed = DefaultKickSeconds;
            if (parsed <= 0)
                return DefaultKickSeconds;
            return parsed;
        }

        // null means the switch stays as it is.
        public static bool? AutoNext(bool auto, bool enabled, int staffOnline)
        {
            if (!auto)
                return null;
            if (enabled && staffOnline > 0)
                return false;
            if (!enabled && staffOnline == 0)
                return true;
            return null;
        }

        public static string CountdownText(int seconds)
        {
            if (seconds < 0)
                seconds = 0;
            int minutes = seconds / 60;
            int remain = seconds % 60;
            return string.Format(
                CultureInfo.InvariantCulture,
                "Not whitelisted\nTime remaining: {0:00}:{1:00}",
                minutes,
                remain);
        }

        public static List<string> Targets(IList<WhitelistCandidate> candidates, ICollection<string> listed)
        {
            var targets = new List<string>();
            if (candidates == null)
                return targets;
            for (int i = 0; i < candidates.Count; i++)
            {
                WhitelistCandidate candidate = candidates[i];
                if (candidate == null || candidate.AdminLevel != 0)
                    continue;
                if (Listed(listed, candidate.Name))
                    continue;
                targets.Add(candidate.Key);
            }

            return targets;
        }

        public static List<string> DueKeys(Dictionary<string, long> ends, long now, bool enabled)
        {
            var due = new List<string>();
            if (!enabled || ends == null || ends.Count == 0)
                return due;

            var keys = new List<string>(ends.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                long at;
                if (!ends.TryGetValue(key, out at) || now < at)
                    continue;
                ends.Remove(key);
                due.Add(key);
            }

            return due;
        }

        public static void Allow(string name, int adminLevel, Action<bool> done)
        {
            WhenReady(() =>
            {
                if (!Enabled || adminLevel > 0)
                {
                    done(true);
                    return;
                }

                if (string.IsNullOrEmpty(name))
                {
                    done(false);
                    return;
                }

                MySql.Single(OneSql, new object[] { name }, new Action<dynamic>(row =>
                {
                    object boxed = row;
                    done(boxed != null);
                }));
            });
        }

        public static bool SetEnabled(bool flag)
        {
            SetEnabled(flag, false, StaffCount(null));
            return Enabled;
        }

        public static bool SetAuto(bool flag)
        {
            Auto = flag;
            ApplyAuto(StaffCount(null), null);
            return Auto;
        }

        public static void NameChanged(string name, bool listed)
        {
            if (string.IsNullOrEmpty(name))
                return;

            List<Session> sessions = Sessions.Copy();
            for (int i = 0; i < sessions.Count; i++)
            {
                Session session = sessions[i];
                if (session == null || !session.HasAccount)
                    continue;
                if (!string.Equals(session.Username, name, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (listed)
                    Stop(session.Source, false);
                else if (Enabled && session.AdminLevel == 0)
                    Start(session);
            }
        }

        public static void Joined(Session session)
        {
            if (session == null || !session.HasAccount)
                return;
            string announce = session.AdminLevel >= AutoLevel ? session.Source : null;
            ApplyAuto(StaffCount(null), announce);
        }

        public static void Left(Session session)
        {
            if (session == null)
                return;
            Stop(session.Source, true);
            if (session.HasAccount)
                ApplyAuto(StaffCount(session.Source), null);
        }

        public static void Remap(string oldSource, string newSource)
        {
            if (string.IsNullOrEmpty(oldSource) || string.IsNullOrEmpty(newSource))
                return;
            long ends;
            if (!Ends.TryGetValue(oldSource, out ends))
                return;
            Ends.Remove(oldSource);
            Ends[newSource] = ends;
        }

        public static int SecondsLeft(string source, long now)
        {
            long ends;
            if (string.IsNullOrEmpty(source) || !Ends.TryGetValue(source, out ends))
                return -1;
            long left = (ends - now + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond;
            if (left < 0)
                left = 0;
            if (left > int.MaxValue)
                left = int.MaxValue;
            return (int)left;
        }

        public static void Tick(long now)
        {
            if (!ready)
                return;

            List<string> due = DueKeys(Ends, now, Enabled);
            for (int i = 0; i < due.Count; i++)
            {
                Session session = Sessions.Get(due[i]);
                if (session == null || !session.HasAccount || session.AdminLevel > 0)
                    continue;

                if (tell != null)
                {
                    tell(session.Source, ActiveLine);
                    tell(session.Source, MissingLine);
                }

                if (note != null)
                {
                    var fields = new Dictionary<string, object>();
                    fields["player"] = session.Source;
                    fields["account_id"] = session.AccountId;
                    fields["name"] = session.Username ?? "";
                    fields["ok"] = false;
                    fields["code"] = "NOT_WHITELISTED";
                    fields["reason"] = "whitelistCountdown";
                    note("session.kicked", fields);
                }

                if (drop != null)
                    drop(session.Source, KickReason);
            }
        }

        public static void Probe(Action<string> done)
        {
            WhenReady(() =>
            {
                var state = new ProbeState();
                state.Done = done;
                state.Allows = CheckAllows() ? 1 : 0;
                state.Seconds = CheckSeconds() ? 1 : 0;
                state.Text = CheckText() ? 1 : 0;
                state.Targets = CheckTargets() ? 1 : 0;
                state.Auto = CheckAuto() ? 1 : 0;
                state.Due = CheckDue() ? 1 : 0;
                state.SavedEnabled = Enabled;
                state.SavedAuto = Auto;
                state.SavedKick = KickSeconds;
                state.Quiet = HasAccount();
                Clean(state);
            });
        }

        static void Clean(ProbeState state)
        {
            MySql.Update(DeleteProbeSql, new object[0], new Action<dynamic>(unused =>
            {
                MySql.Update(InsertProbeSql, new object[0], new Action<dynamic>(inserted =>
                {
                    object boxed = inserted;
                    int affected = boxed == null ? 0 : Convert.ToInt32(boxed);
                    state.Inserted = affected > 0 ? 1 : 0;
                    MySql.Single(OneSql, new object[] { "F020A" }, new Action<dynamic>(row =>
                    {
                        object found = row;
                        state.Listed = found != null ? 1 : 0;
                        MySql.Single(OneSql, new object[] { "f020b" }, new Action<dynamic>(missing =>
                        {
                            object absent = missing;
                            state.Missing = absent == null ? 1 : 0;
                            FinishProbe(state);
                        }));
                    }));
                }));
            }));
        }

        static void FinishProbe(ProbeState state)
        {
            MySql.Update(DeleteProbeSql, new object[0], new Action<dynamic>(unused =>
            {
                MySql.Single(CountProbeSql, new object[0], new Action<dynamic>(row =>
                {
                    object boxed = row;
                    int left = boxed == null ? -1 : MySql.Int(boxed, "n");
                    state.Cleaned = left == 0 ? 1 : 0;
                    if (!state.Quiet)
                    {
                        bool turnedOn = SetEnabled(true);
                        state.Toggle = turnedOn && Enabled ? 1 : 0;
                        SetEnabled(false);
                    }
                    else
                    {
                        state.Toggle = 1;
                    }

                    Auto = state.SavedAuto;
                    KickSeconds = state.SavedKick;
                    if (Enabled != state.SavedEnabled)
                        SetEnabled(state.SavedEnabled);
                    state.Restored = Enabled == state.SavedEnabled && Auto == state.SavedAuto && KickSeconds == state.SavedKick ? 1 : 0;
                    state.Done(
                        "ok allows=" + state.Allows.ToString(CultureInfo.InvariantCulture)
                        + " kick_s=" + state.Seconds.ToString(CultureInfo.InvariantCulture)
                        + " text=" + state.Text.ToString(CultureInfo.InvariantCulture)
                        + " targets=" + state.Targets.ToString(CultureInfo.InvariantCulture)
                        + " auto=" + state.Auto.ToString(CultureInfo.InvariantCulture)
                        + " due=" + state.Due.ToString(CultureInfo.InvariantCulture)
                        + " listed=" + state.Listed.ToString(CultureInfo.InvariantCulture)
                        + " casefold=" + state.Listed.ToString(CultureInfo.InvariantCulture)
                        + " missing=" + state.Missing.ToString(CultureInfo.InvariantCulture)
                        + " inserted=" + state.Inserted.ToString(CultureInfo.InvariantCulture)
                        + " cleaned=" + state.Cleaned.ToString(CultureInfo.InvariantCulture)
                        + " toggle=" + state.Toggle.ToString(CultureInfo.InvariantCulture)
                        + " restored=" + state.Restored.ToString(CultureInfo.InvariantCulture));
                }));
            }));
        }

        static bool CheckAllows()
        {
            return Allows(false, false, 0)
                && !Allows(true, false, 0)
                && Allows(true, true, 0)
                && Allows(true, false, 1);
        }

        static bool CheckSeconds()
        {
            return KickSecondsFrom("120") == 120
                && KickSecondsFrom(null) == DefaultKickSeconds
                && KickSecondsFrom("0") == DefaultKickSeconds
                && KickSecondsFrom("x") == DefaultKickSeconds;
        }

        static bool CheckText()
        {
            return CountdownText(300) == "Not whitelisted\nTime remaining: 05:00"
                && CountdownText(9) == "Not whitelisted\nTime remaining: 00:09"
                && CountdownText(-2) == "Not whitelisted\nTime remaining: 00:00";
        }

        static bool CheckTargets()
        {
            var candidates = new List<WhitelistCandidate>();
            candidates.Add(new WhitelistCandidate { Key = "a", Name = "Bob", AdminLevel = 0 });
            candidates.Add(new WhitelistCandidate { Key = "b", Name = "alice", AdminLevel = 0 });
            candidates.Add(new WhitelistCandidate { Key = "c", Name = "Cara", AdminLevel = 2 });
            var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            listed.Add("Alice");
            List<string> targets = Targets(candidates, listed);
            if (targets.Count != 1 || targets[0] != "a")
                return false;
            targets = Targets(candidates, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            return targets.Count == 2 && targets[0] == "a" && targets[1] == "b";
        }

        static bool CheckAuto()
        {
            return AutoNext(true, true, 1) == false
                && AutoNext(true, false, 0) == true
                && AutoNext(false, false, 0) == null
                && AutoNext(true, true, 0) == null;
        }

        static bool CheckDue()
        {
            var ends = new Dictionary<string, long>();
            ends["soon"] = 10;
            ends["later"] = 50;
            List<string> due = DueKeys(ends, 10, true);
            if (due.Count != 1 || due[0] != "soon" || ends.ContainsKey("soon") || !ends.ContainsKey("later"))
                return false;
            due = DueKeys(ends, 100, false);
            return due.Count == 0 && ends.ContainsKey("later");
        }

        static bool HasAccount()
        {
            List<Session> sessions = Sessions.Copy();
            for (int i = 0; i < sessions.Count; i++)
            {
                if (sessions[i] != null && sessions[i].HasAccount)
                    return true;
            }

            return false;
        }

        static void SetEnabled(bool flag, bool auto, int staffOnline)
        {
            if (flag == Enabled)
                return;
            Enabled = flag;
            if (note != null)
            {
                var fields = new Dictionary<string, object>();
                fields["on"] = flag;
                fields["auto"] = auto;
                fields["staff_online"] = staffOnline;
                fields["ok"] = true;
                fields["code"] = flag ? "ON" : "OFF";
                note("session.whitelistToggled", fields);
            }

            if (flag)
                StartCountdowns();
            else
                StopAll(false);
        }

        static void ApplyAuto(int staffOnline, string announceSource)
        {
            bool? next = AutoNext(Auto, Enabled, staffOnline);
            if (next == null)
                return;
            SetEnabled(next.Value, true, staffOnline);
            if (next == false && !string.IsNullOrEmpty(announceSource) && tell != null)
                tell(announceSource, AutoOffLine);
        }

        static void StartCountdowns()
        {
            var candidates = new List<WhitelistCandidate>();
            var names = new List<string>();
            List<Session> sessions = Sessions.Copy();
            for (int i = 0; i < sessions.Count; i++)
            {
                Session session = sessions[i];
                if (session == null || !session.HasAccount || session.AdminLevel != 0 || Ends.ContainsKey(session.Source))
                    continue;
                candidates.Add(new WhitelistCandidate
                {
                    Key = session.Source,
                    Name = session.Username,
                    AdminLevel = session.AdminLevel
                });
                names.Add(session.Username ?? "");
            }

            if (names.Count == 0)
                return;

            MySql.Query(ListedSql, new object[] { JsonNames(names) }, new Action<dynamic>(result =>
            {
                if (!Enabled)
                    return;
                object boxed = result;
                var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                EachRow(boxed, row =>
                {
                    string name = MySql.Text(row, "name");
                    if (!string.IsNullOrEmpty(name))
                        listed.Add(name);
                });

                List<string> targets = Targets(candidates, listed);
                for (int i = 0; i < targets.Count; i++)
                {
                    Session session = Sessions.Get(targets[i]);
                    if (session != null && session.HasAccount && session.AdminLevel == 0)
                        Start(session);
                }

                if (note != null)
                {
                    var fields = new Dictionary<string, object>();
                    fields["players"] = targets.Count;
                    fields["kick_s"] = KickSeconds;
                    fields["ok"] = true;
                    fields["code"] = "COUNTDOWN";
                    note("session.whitelistCountdownStarted", fields);
                }
            }));
        }

        static void Start(Session session)
        {
            if (session == null || Ends.ContainsKey(session.Source) || session.AdminLevel > 0)
                return;
            Ends[session.Source] = DateTime.UtcNow.Ticks + (long)KickSeconds * TimeSpan.TicksPerSecond;
            if (showCountdown != null)
                showCountdown(session.Source, KickSeconds);
        }

        static void Stop(string source, bool quiet)
        {
            if (string.IsNullOrEmpty(source) || !Ends.ContainsKey(source))
                return;
            Ends.Remove(source);
            if (!quiet && showCountdown != null)
                showCountdown(source, -1);
        }

        static void StopAll(bool quiet)
        {
            var sources = new List<string>(Ends.Keys);
            for (int i = 0; i < sources.Count; i++)
                Stop(sources[i], quiet);
        }

        static int StaffCount(string except)
        {
            int count = 0;
            List<Session> sessions = Sessions.Copy();
            for (int i = 0; i < sessions.Count; i++)
            {
                Session session = sessions[i];
                if (session == null || !session.HasAccount || session.Source == except)
                    continue;
                if (session.AdminLevel >= AutoLevel)
                    count++;
            }

            return count;
        }

        static bool Listed(ICollection<string> listed, string name)
        {
            if (listed == null || string.IsNullOrEmpty(name))
                return false;
            foreach (string entry in listed)
            {
                if (string.Equals(entry, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        static void WhenReady(Action next)
        {
            if (next == null)
                return;
            if (ready)
                next();
            else
                Waiting.Add(next);
        }

        static void Drain()
        {
            var pending = new List<Action>(Waiting);
            Waiting.Clear();
            for (int i = 0; i < pending.Count; i++)
                pending[i]();
        }

        static void EachRow(object result, Action<dynamic> row)
        {
            if (result == null || row == null || result is string)
                return;

            IDictionary<string, object> one = result as IDictionary<string, object>;
            if (one != null)
            {
                row(result);
                return;
            }

            System.Collections.IEnumerable rows = result as System.Collections.IEnumerable;
            if (rows == null)
                return;
            foreach (object item in rows)
            {
                if (item != null)
                    row(item);
            }
        }

        static string JsonNames(List<string> names)
        {
            var json = new StringBuilder();
            json.Append('[');
            for (int i = 0; i < names.Count; i++)
            {
                if (i > 0)
                    json.Append(',');
                json.Append('"');
                string name = names[i] ?? "";
                for (int c = 0; c < name.Length; c++)
                {
                    char ch = name[c];
                    if (ch == '\\' || ch == '"')
                        json.Append('\\');
                    json.Append(ch);
                }

                json.Append('"');
            }

            json.Append(']');
            return json.ToString();
        }

        sealed class ProbeState
        {
            public Action<string> Done;
            public int Allows;
            public int Seconds;
            public int Text;
            public int Targets;
            public int Auto;
            public int Due;
            public int Listed;
            public int Missing;
            public int Inserted;
            public int Cleaned;
            public int Toggle;
            public int Restored;
            public bool SavedEnabled;
            public bool SavedAuto;
            public int SavedKick;
            public bool Quiet;
        }
    }

    sealed class WhitelistCandidate
    {
        public string Key;
        public string Name;
        public int AdminLevel;
    }
}
