using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Server
{
    // F021: one connection_log row per authenticated join, the identifier set in
    // connection_identifiers, left_at on drop. Tokens are never written here.
    static class Connections
    {
        public static string Step = "";
        const string LogSql = "INSERT INTO connection_log (account_id, name, ip) VALUES (?, ?, ?)";

        const string IdentifierSql =
            "INSERT INTO connection_identifiers (log_id, type, value) " +
            "SELECT ?, jt.type, jt.value FROM JSON_TABLE(?, '$[*]' COLUMNS (" +
            "type VARCHAR(16) PATH '$.type', value VARCHAR(255) PATH '$.value')) jt " +
            "WHERE jt.type IN ('steam','discord','xbl','live','license','license2','fivem','ip') " +
            "AND jt.value IS NOT NULL AND CHAR_LENGTH(jt.value) BETWEEN 1 AND 255";

        public static void Open(int accountId, string name, Dictionary<string, string> identifiers, Action<long> done)
        {
            Open(accountId, name, identifiers, null, done);
        }

        // opened fires with the log id as soon as the row exists, before the identifier write.
        // oxmysql does not call back when a statement fails, so the caller must keep the id from here.
        public static void Open(int accountId, string name, Dictionary<string, string> identifiers, Action<long> opened, Action<long> done)
        {
            var set = new Dictionary<string, string>();
            if (identifiers != null)
            {
                for (int i = 0; i < Identifier.Types.Length; i++)
                {
                    string type = Identifier.Types[i];
                    string value;
                    if (identifiers.TryGetValue(type, out value) && !string.IsNullOrEmpty(value) && value.Length <= 255)
                        set[type] = value;
                }
            }

            string ip;
            set.TryGetValue("ip", out ip);
            object ipValue = !string.IsNullOrEmpty(ip) && ip.Length <= 45 ? (object)ip : null;
            string shown = name ?? "";
            if (shown.Length > 24) shown = shown.Substring(0, 24);

            MySql.Insert(LogSql, new object[] { accountId, shown, ipValue }, new Action<dynamic>(inserted =>
            {
                long logId = ToId(inserted);
                if (logId <= 0)
                {
                    done(0);
                    return;
                }

                if (opened != null) opened(logId);
                MySql.Update(IdentifierSql, new object[] { logId, Links.IdentifierJson(set) }, new Action<dynamic>(written =>
                {
                    if (written == null)
                    {
                        MySql.Update("DELETE FROM connection_log WHERE id = ?", new object[] { logId },
                            new Action<dynamic>(unused => done(0)));
                        return;
                    }

                    done(logId);
                }));
            }));
        }

        public static void Close(long logId, Action<bool> done)
        {
            if (logId <= 0)
            {
                if (done != null) done(false);
                return;
            }

            MySql.Update(
                "UPDATE connection_log SET left_at = NOW() WHERE id = ? AND left_at IS NULL",
                new object[] { logId },
                new Action<dynamic>(changed =>
                {
                    if (done != null) done(ToId(changed) > 0);
                }));
        }

        internal static long ToId(dynamic value)
        {
            if (value == null) return 0;
            try
            {
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static void Probe(Action<string> done)
        {
            new ProbeRun(done).Start();
        }

        sealed class ProbeRun
        {
            const string Name = "f021a";
            readonly Action<string> _done;
            readonly Dictionary<string, string> _ids = new Dictionary<string, string>();
            int _account;
            long _log;
            long _longLog;
            long _early;
            int _identifiers = -1;
            int _ip;
            int _noToken;
            int _noSerial;
            int _closed;
            int _idempotent;
            int _longIp;

            public ProbeRun(Action<string> done)
            {
                _done = done;
                _ids["license"] = "f021-license";
                _ids["license2"] = "f021-license2";
                _ids["discord"] = "f021-discord";
                _ids["fivem"] = "f021-fivem";
                _ids["steam"] = "f021-steam";
                _ids["xbl"] = "f021-xbl";
                _ids["live"] = "f021-live";
                _ids["ip"] = "203.0.113.21";
            }

            public void Start()
            {
                Step = "start";
                MySql.Update("DELETE FROM accounts WHERE name = ?", new object[] { Name }, new Action<dynamic>(unused =>
                {
                    MySql.Insert("INSERT INTO accounts (name, password_hash) VALUES (?, ?)",
                        new object[] { Name, new string('x', 60) },
                        new Action<dynamic>(id =>
                        {
                            Step = "account";
                            _account = (int)Connections.ToId(id);
                            if (_account <= 0) { Finish("account"); return; }
                            Connections.Open(_account, Name, _ids, new Action<long>(early => _early = early), new Action<long>(logId =>
                            {
                                Step = "opened";
                                _log = logId;
                                Check();
                            }));
                        }));
                }));
            }

            void Check()
            {
                Step = "check";
                if (_log <= 0) { Finish("open"); return; }
                MySql.Query("SELECT name, ip, left_at FROM connection_log WHERE id = ?", new object[] { _log }, new Action<dynamic>(rows =>
                {
                    object list = rows;
                    var first = First(list);
                    if (first != null && MySql.Text(first, "name") == Name
                        && MySql.Text(first, "ip") == "203.0.113.21" && MySql.Field(first, "left_at") == null)
                        _ip = 1;
                    MySql.Query("SELECT COUNT(*) AS n FROM connection_identifiers WHERE log_id = ?", new object[] { _log }, new Action<dynamic>(count =>
                    {
                        object cl = count;
                        _identifiers = MySql.Int(First(cl), "n");
                        MySql.Query(
                            "SELECT (SELECT COUNT(*) FROM connection_identifiers WHERE value LIKE 'f021-tok%') + " +
                            "(SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() " +
                            "AND table_name = 'connection_log' AND column_name = 'serial') AS bad",
                            new object[0],
                            new Action<dynamic>(bad =>
                            {
                                object bl = bad;
                                int n = MySql.Int(First(bl), "bad");
                                _noToken = n == 0 ? 1 : 0;
                                _noSerial = _noToken;
                                CloseTwice();
                            }));
                    }));
                }));
            }

            void CloseTwice()
            {
                Step = "close";
                Connections.Close(_log, new Action<bool>(first =>
                {
                    Connections.Close(_log, new Action<bool>(second =>
                    {
                        _closed = first ? 1 : 0;
                        _idempotent = !second ? 1 : 0;
                        LongIp();
                    }));
                }));
            }

            void LongIp()
            {
                Step = "longip";
                var ids = new Dictionary<string, string>(_ids);
                ids["ip"] = new string('9', 50);
                Connections.Open(_account, Name, ids, new Action<long>(logId =>
                {
                    _longLog = logId;
                    if (logId <= 0) { Cleanup(); return; }
                    MySql.Query("SELECT ip FROM connection_log WHERE id = ?", new object[] { logId }, new Action<dynamic>(rows =>
                    {
                        object list = rows;
                        _longIp = MySql.Text(First(list), "ip") == null ? 1 : 0;
                        Cleanup();
                    }));
                }));
            }

            void Cleanup()
            {
                Step = "cleanup";
                MySql.Update("DELETE FROM connection_log WHERE account_id = ?", new object[] { _account }, new Action<dynamic>(unused =>
                {
                    MySql.Update("DELETE FROM accounts WHERE id = ?", new object[] { _account }, new Action<dynamic>(unused2 =>
                    {
                        MySql.Scalar(
                            "SELECT (SELECT COUNT(*) FROM connection_log WHERE name = ?) + (SELECT COUNT(*) FROM accounts WHERE name = ?)",
                            new object[] { Name, Name },
                            new Action<dynamic>(left =>
                            {
                                int remaining = 1;
                                try { remaining = Convert.ToInt32(left, CultureInfo.InvariantCulture); } catch (Exception) { }
                                Finish(null, remaining == 0 ? 1 : 0);
                            }));
                    }));
                }));
            }

            void Finish(string failed, int cleaned = 0)
            {
                if (failed != null)
                {
                    _done("failed step=" + failed);
                    return;
                }

                bool ok = _log > 0 && _identifiers == 8 && _ip == 1 && _noToken == 1 && _noSerial == 1
                    && _closed == 1 && _idempotent == 1 && _longIp == 1 && _early == _log && cleaned == 1;
                _done((ok ? "ok" : "failed") + " open=" + (_log > 0 ? 1 : 0) + " early=" + (_early == _log ? 1 : 0) + " identifiers=" + _identifiers
                    + " tokens=" + (_noToken == 1 ? 0 : 1) + " ip=" + _ip + " longip=" + _longIp
                    + " closed=" + _closed + " idempotent=" + _idempotent + " cleaned=" + cleaned);
            }

            static dynamic First(object rows)
            {
                var list = rows as System.Collections.IEnumerable;
                if (list == null) return null;
                foreach (object row in list) return row;
                return null;
            }
        }
    }
}
