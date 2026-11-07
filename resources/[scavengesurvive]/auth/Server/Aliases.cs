using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Server
{
    sealed class Alias
    {
        public int AccountId;
        public string Name;
        public string Matched;
        public bool Banned;
    }

    // F022: another account is an alias when it shares an identifier or a token with this one.
    // Identifier overlap is read from connection history (connection_identifiers) and from the
    // current account_identifiers rows. Token overlap is account_tokens. No GPCI, no password hash.
    static class Aliases
    {
        public static string Step = "";

        const string FindSql =
            "SELECT a.id AS account_id, a.name AS name, GROUP_CONCAT(DISTINCT x.kind ORDER BY x.kind) AS matched, " +
            "EXISTS(SELECT 1 FROM bans b WHERE b.active = 1 AND (b.expires_at IS NULL OR b.expires_at > NOW()) " +
            "AND (b.account_id = a.id OR b.name = a.name)) AS banned " +
            "FROM (" +
            "SELECT l2.account_id AS account_id, c2.type AS kind FROM connection_log l1 " +
            "JOIN connection_identifiers c1 ON c1.log_id = l1.id " +
            "JOIN connection_identifiers c2 ON c2.type = c1.type AND c2.value = c1.value " +
            "JOIN connection_log l2 ON l2.id = c2.log_id " +
            "WHERE l1.account_id = ? AND l2.account_id IS NOT NULL AND l2.account_id <> ? " +
            "UNION ALL " +
            "SELECT l2.account_id, c2.type FROM account_identifiers a1 " +
            "JOIN connection_identifiers c2 ON c2.type = a1.type AND c2.value = a1.value " +
            "JOIN connection_log l2 ON l2.id = c2.log_id " +
            "WHERE a1.account_id = ? AND l2.account_id IS NOT NULL AND l2.account_id <> ? " +
            "UNION ALL " +
            "SELECT a2.account_id, a2.type FROM account_identifiers a1 " +
            "JOIN account_identifiers a2 ON a2.type = a1.type AND a2.value = a1.value " +
            "WHERE a1.account_id = ? AND a2.account_id <> ? " +
            "UNION ALL " +
            "SELECT t2.account_id, 'token' FROM account_tokens t1 " +
            "JOIN account_tokens t2 ON t2.token = t1.token " +
            "WHERE t1.account_id = ? AND t2.account_id <> ?" +
            ") x JOIN accounts a ON a.id = x.account_id " +
            "GROUP BY a.id, a.name ORDER BY banned DESC, a.name LIMIT 50";

        public static void Find(int accountId, Action<List<Alias>> done)
        {
            var list = new List<Alias>();
            if (accountId <= 0)
            {
                done(list);
                return;
            }

            object[] args = { accountId, accountId, accountId, accountId, accountId, accountId, accountId, accountId };
            MySql.Query(FindSql, args, new Action<dynamic>(rows =>
            {
                var enumerable = rows as IEnumerable;
                if (enumerable != null)
                {
                    foreach (object item in enumerable)
                    {
                        dynamic row = item;
                        var alias = new Alias();
                        alias.AccountId = MySql.Int(row, "account_id");
                        alias.Name = MySql.Text(row, "name") ?? "";
                        alias.Matched = MySql.Text(row, "matched") ?? "";
                        alias.Banned = MySql.Int(row, "banned") == 1;
                        list.Add(alias);
                    }
                }

                done(list);
            }));
        }

        public static void Probe(Action<string> done)
        {
            new ProbeRun(done).Start();
        }

        sealed class ProbeRun
        {
            const string Prefix = "f022";
            readonly Action<string> _done;
            readonly int[] _ids = new int[4];
            int _hasB, _hasC, _hasD, _noSelf, _bannedB, _cleanC, _matchedB, _matchedC, _ipOnly, _empty;

            public ProbeRun(Action<string> done)
            {
                _done = done;
            }

            static readonly string[] Names = { "f022a", "f022b", "f022c", "f022d" };

            public void Start()
            {
                Step = "start";
                Wipe(() => Create(0));
            }

            void Wipe(Action next)
            {
                MySql.Update("DELETE FROM bans WHERE name LIKE 'f022%'", new object[0], new Action<dynamic>(u1 =>
                    MySql.Update("DELETE FROM connection_log WHERE name LIKE 'f022%'", new object[0], new Action<dynamic>(u2 =>
                        MySql.Update("DELETE FROM accounts WHERE name LIKE 'f022%'", new object[0], new Action<dynamic>(u3 => next()))))));
            }

            void Create(int i)
            {
                Step = "account" + i;
                if (i == Names.Length) { Seed(); return; }
                MySql.Insert("INSERT INTO accounts (name, password_hash) VALUES (?, ?)",
                    new object[] { Names[i], new string('x', 60) },
                    new Action<dynamic>(id =>
                    {
                        _ids[i] = (int)Connections.ToId(id);
                        if (_ids[i] <= 0) { Finish("account"); return; }
                        Create(i + 1);
                    }));
            }

            // A and B share discord (history), A and D share only an ip (reported, as ip), A and C share a token.
            // B is banned by account id. A also has a connection with no identifier rows.
            void Seed()
            {
                Step = "seed";
                var a = new Dictionary<string, string>();
                a["license"] = "f022-license-a"; a["discord"] = "f022-discord-ab"; a["ip"] = "203.0.113.22";
                var b = new Dictionary<string, string>();
                b["license"] = "f022-license-b"; b["discord"] = "f022-discord-ab";
                var d = new Dictionary<string, string>();
                d["license"] = "f022-license-d"; d["ip"] = "203.0.113.22";
                Connections.Open(_ids[0], Names[0], a, new Action<long>(x =>
                    Connections.Open(_ids[1], Names[1], b, new Action<long>(y =>
                        Connections.Open(_ids[3], Names[3], d, new Action<long>(z =>
                            Connections.Open(_ids[0], Names[0], null, new Action<long>(w =>
                            {
                                if (x <= 0 || y <= 0 || z <= 0 || w <= 0) { Finish("open"); return; }
                                MySql.Update("INSERT INTO account_tokens (account_id, token) VALUES (?, 'f022-token'), (?, 'f022-token')",
                                    new object[] { _ids[0], _ids[2] }, new Action<dynamic>(t =>
                                    MySql.Update("INSERT INTO bans (account_id, name, reason) VALUES (?, ?, 'f022')",
                                        new object[] { _ids[1], Names[1] }, new Action<dynamic>(bn => Check()))));
                            }))))))));
            }

            void Check()
            {
                Step = "check";
                Aliases.Find(_ids[0], new Action<List<Alias>>(list =>
                {
                    foreach (Alias alias in list)
                    {
                        if (alias.AccountId == _ids[0]) _noSelf = -1;
                        if (alias.AccountId == _ids[1]) { _hasB = 1; _bannedB = alias.Banned ? 1 : 0; _matchedB = alias.Matched == "discord" ? 1 : 0; }
                        if (alias.AccountId == _ids[2]) { _hasC = 1; _cleanC = alias.Banned ? 0 : 1; _matchedC = alias.Matched == "token" ? 1 : 0; }
                        if (alias.AccountId == _ids[3]) { _hasD = 1; _ipOnly = alias.Matched == "ip" ? 1 : 0; }
                    }

                    if (_noSelf == 0) _noSelf = 1;
                    Aliases.Find(0, new Action<List<Alias>>(none =>
                    {
                        _empty = none.Count == 0 ? 1 : 0;
                        Wipe(() => MySql.Scalar(
                            "SELECT (SELECT COUNT(*) FROM accounts WHERE name LIKE 'f022%') + (SELECT COUNT(*) FROM bans WHERE name LIKE 'f022%') + " +
                            "(SELECT COUNT(*) FROM connection_log WHERE name LIKE 'f022%') + (SELECT COUNT(*) FROM account_tokens WHERE token = 'f022-token')",
                            new object[0],
                            new Action<dynamic>(left =>
                            {
                                int remaining = 1;
                                try { remaining = Convert.ToInt32(left, CultureInfo.InvariantCulture); } catch (Exception) { }
                                Finish(null, remaining == 0 ? 1 : 0);
                            })));
                    }));
                }));
            }

            void Finish(string failed, int cleaned = 0)
            {
                if (failed != null) { _done("failed step=" + failed); return; }
                // D is reported through the shared ip, which is a weaker match, so staff see the type.
                bool ok = _hasB == 1 && _bannedB == 1 && _matchedB == 1 && _hasC == 1 && _cleanC == 1 && _matchedC == 1
                    && _hasD == 1 && _ipOnly == 1 && _noSelf == 1 && _empty == 1 && cleaned == 1;
                _done((ok ? "ok" : "failed") + " history=" + _hasB + " banned=" + _bannedB + " token=" + _hasC
                    + " ip=" + _ipOnly + " self=" + _noSelf + " empty=" + _empty + " cleaned=" + cleaned);
            }
        }
    }
}
