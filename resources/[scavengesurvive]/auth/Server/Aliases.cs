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
            "WITH mine AS (" +
            "SELECT c1.type, c1.value FROM connection_log l1 JOIN connection_identifiers c1 ON c1.log_id = l1.id WHERE l1.account_id = ? " +
            "UNION SELECT type, value FROM account_identifiers WHERE account_id = ?), " +
            "mytok AS (SELECT token FROM account_tokens WHERE account_id = ?) " +
            "SELECT a.id AS account_id, a.name AS name, GROUP_CONCAT(DISTINCT x.kind ORDER BY x.kind) AS matched, " +
            "EXISTS(SELECT 1 FROM bans b WHERE b.active = 1 AND (b.expires_at IS NULL OR b.expires_at > NOW()) " +
            "AND (b.account_id = a.id OR b.name = a.name)) AS banned " +
            "FROM (" +
            "SELECT l2.account_id AS account_id, c2.type AS kind FROM mine m " +
            "JOIN connection_identifiers c2 ON c2.type = m.type AND c2.value = m.value " +
            "JOIN connection_log l2 ON l2.id = c2.log_id WHERE l2.account_id IS NOT NULL AND l2.account_id <> ? " +
            "UNION ALL " +
            "SELECT a2.account_id, a2.type FROM mine m " +
            "JOIN account_identifiers a2 ON a2.type = m.type AND a2.value = m.value WHERE a2.account_id <> ? " +
            "UNION ALL " +
            "SELECT t2.account_id, 'token' FROM mytok m JOIN account_tokens t2 ON t2.token = m.token WHERE t2.account_id <> ?" +
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

            object[] args = { accountId, accountId, accountId, accountId, accountId, accountId };
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
            static readonly string[] Names = { "f022a", "f022b", "f022c", "f022d", "f022e" };
            const string NameList = "('f022a','f022b','f022c','f022d','f022e')";
            readonly Action<string> _done;
            readonly int[] _ids = new int[5];
            readonly List<Action<Action>> _steps = new List<Action<Action>>();
            int _hasB, _bannedB, _matchedB, _hasC, _cleanC, _matchedC, _hasD, _ipOnly, _noSelf = 1, _empty;
            int _banAccount, _aliasHit, _ipClear, _tokenClear, _copied, _noIp, _direct, _ipStill;

            public ProbeRun(Action<string> done)
            {
                _done = done;
            }

            public void Start()
            {
                Aliases.Step = "start";
                _steps.Add(Wipe);
                for (int i = 0; i < Names.Length; i++)
                {
                    int n = i;
                    _steps.Add(next => MySql.Insert("INSERT INTO accounts (name, password_hash) VALUES (?, ?)",
                        new object[] { Names[n], new string('x', 60) },
                        new Action<dynamic>(id =>
                        {
                            _ids[n] = (int)Connections.ToId(id);
                            if (_ids[n] <= 0) { Fail("account"); return; }
                            next();
                        })));
                }

                // A: discord shared with B, ip shared with D, token shared with C, plus a connection with no identifier rows.
                _steps.Add(next => Open(0, Set("f022-license-a", "f022-discord-ab", "203.0.113.22"), next));
                _steps.Add(next => Open(1, Set("f022-license-b", "f022-discord-ab", "203.0.113.23"), next));
                _steps.Add(next => Open(3, Set("f022-license-d", null, "203.0.113.22"), next));
                _steps.Add(next => Open(0, null, next));
                _steps.Add(next => MySql.Update("INSERT INTO account_tokens (account_id, token) VALUES (?, 'f022-token'), (?, 'f022-token')",
                    new object[] { _ids[0], _ids[2] }, new Action<dynamic>(u => next())));
                // B is banned offline by name: no captured set, nothing in ban_identifiers.
                _steps.Add(next => Bans.Place(Names[1], 0, "f022", 0, "", 0, null, null, new Action<BanWrite>(w =>
                {
                    if (!w.Ok) { Fail("ban"); return; }
                    MySql.Scalar("SELECT account_id FROM bans WHERE id = ?", new object[] { w.BanId }, new Action<dynamic>(acc =>
                    {
                        _banAccount = (int)Connections.ToId(acc) == _ids[1] ? 1 : 0;
                        next();
                    }));
                })));
                _steps.Add(CheckFind);
                _steps.Add(next => Match(Set("f022-license-e", "f022-discord-ab", null), null, hit =>
                {
                    _aliasHit = hit.Code == "CLEAR" ? 1 : 0;
                    next();
                }));
                _steps.Add(next => Match(Set(null, null, "203.0.113.23"), null, hit => { _ipClear = hit.Code == "CLEAR" ? 1 : 0; next(); }));
                _steps.Add(next => Match(null, new[] { "f022-token" }, hit => { _tokenClear = hit.Code == "CLEAR" ? 1 : 0; next(); }));
                // D is banned offline with stored identifiers: the ban copies license and token, never ip.
                _steps.Add(next => MySql.Update("INSERT INTO account_identifiers (account_id, type, value) VALUES (?, 'license', 'f022-license-d'), (?, 'ip', '203.0.113.22')",
                    new object[] { _ids[3], _ids[3] }, new Action<dynamic>(u => next())));
                _steps.Add(next => MySql.Update("INSERT INTO account_tokens (account_id, token) VALUES (?, 'f022-token-d')",
                    new object[] { _ids[3] }, new Action<dynamic>(u => next())));
                _steps.Add(next => Bans.Place(Names[3], 0, "f022", 0, "", 0, null, null, new Action<BanWrite>(w =>
                {
                    if (!w.Ok) { Fail("banD"); return; }
                    MySql.Query("SELECT type, value FROM ban_identifiers WHERE ban_id = ?", new object[] { w.BanId }, new Action<dynamic>(rows =>
                    {
                        int license = 0, token = 0, ip = 0, total = 0;
                        var list = rows as System.Collections.IEnumerable;
                        if (list != null)
                            foreach (object item in list)
                            {
                                dynamic row = item;
                                total++;
                                string type = MySql.Text(row, "type");
                                if (type == "license") license++;
                                if (type == "token") token++;
                                if (type == "ip") ip++;
                            }

                        _copied = license == 1 && token == 1 && total == 2 ? 1 : 0;
                        _noIp = ip == 0 ? 1 : 0;
                        next();
                    }));
                })));
                _steps.Add(next => Match(Set("f022-license-d", null, null), null, hit =>
                {
                    _direct = hit.Banned && hit.Code == "BANNED" ? 1 : 0;
                    next();
                }));
                _steps.Add(next => Match(Set(null, null, "203.0.113.22"), null, hit => { _ipStill = hit.Code == "CLEAR" ? 1 : 0; next(); }));
                _steps.Add(Wipe);
                _steps.Add(next => MySql.Scalar(
                    "SELECT (SELECT COUNT(*) FROM accounts WHERE name IN " + NameList + ") + (SELECT COUNT(*) FROM bans WHERE name IN " + NameList + ") + " +
                    "(SELECT COUNT(*) FROM connection_log WHERE name IN " + NameList + ") + (SELECT COUNT(*) FROM account_tokens WHERE token LIKE 'f022-token%')",
                    new object[0],
                    new Action<dynamic>(left =>
                    {
                        int remaining = 1;
                        try { remaining = Convert.ToInt32(left, CultureInfo.InvariantCulture); } catch (Exception) { }
                        Finish(remaining == 0 ? 1 : 0);
                    })));
                Run(0);
            }

            void Fail(string step)
            {
                Wipe(() => _done("failed step=" + step));
            }

            void Run(int i)
            {
                Aliases.Step = "step" + i;
                if (i >= _steps.Count) return;
                _steps[i](() => Run(i + 1));
            }

            static void Wipe(Action next)
            {
                MySql.Update("DELETE FROM bans WHERE name IN " + NameList, new object[0], new Action<dynamic>(u1 =>
                    MySql.Update("DELETE FROM connection_log WHERE name IN " + NameList, new object[0], new Action<dynamic>(u2 =>
                        MySql.Update("DELETE FROM accounts WHERE name IN " + NameList, new object[0], new Action<dynamic>(u3 => next()))))));
            }

            static Dictionary<string, string> Set(string license, string discord, string ip)
            {
                var set = new Dictionary<string, string>();
                if (license != null) set["license"] = license;
                if (discord != null) set["discord"] = discord;
                if (ip != null) set["ip"] = ip;
                return set;
            }

            void Open(int who, Dictionary<string, string> set, Action next)
            {
                Connections.Open(_ids[who], Names[who], set, new Action<long>(log =>
                {
                    if (log <= 0) { Fail("open"); return; }
                    next();
                }));
            }

            static void Match(Dictionary<string, string> set, string[] tokens, Action<BanHit> done)
            {
                Bans.Match(set, tokens, done);
            }

            void CheckFind(Action next)
            {
                Aliases.Find(_ids[0], new Action<List<Alias>>(list =>
                {
                    foreach (Alias alias in list)
                    {
                        if (alias.AccountId == _ids[0]) _noSelf = 0;
                        if (alias.AccountId == _ids[1]) { _hasB = 1; _bannedB = alias.Banned ? 1 : 0; _matchedB = alias.Matched == "discord" ? 1 : 0; }
                        if (alias.AccountId == _ids[2]) { _hasC = 1; _cleanC = alias.Banned ? 0 : 1; _matchedC = alias.Matched == "token" ? 1 : 0; }
                        if (alias.AccountId == _ids[3]) { _hasD = 1; _ipOnly = alias.Matched == "ip" ? 1 : 0; }
                    }

                    Aliases.Find(0, new Action<List<Alias>>(none =>
                    {
                        _empty = none.Count == 0 ? 1 : 0;
                        next();
                    }));
                }));
            }

            void Finish(int cleaned)
            {
                bool ok = _hasB == 1 && _bannedB == 1 && _matchedB == 1 && _hasC == 1 && _cleanC == 1 && _matchedC == 1
                    && _hasD == 1 && _ipOnly == 1 && _noSelf == 1 && _empty == 1 && _banAccount == 1
                    && _aliasHit == 1 && _ipClear == 1 && _tokenClear == 1
                    && _copied == 1 && _noIp == 1 && _direct == 1 && _ipStill == 1 && cleaned == 1;
                _done((ok ? "ok" : "failed") + " history=" + _hasB + " banned=" + _bannedB + " token=" + _hasC + " ip=" + _ipOnly
                    + " self=" + _noSelf + " empty=" + _empty + " banacct=" + _banAccount
                    + " multi=" + _aliasHit + " ipclear=" + _ipClear + " tokenclear=" + _tokenClear
                    + " copied=" + _copied + " noip=" + _noIp + " direct=" + _direct + " ipstill=" + _ipStill + " cleaned=" + cleaned);
            }
        }
    }
}
