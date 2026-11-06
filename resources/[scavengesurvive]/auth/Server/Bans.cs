using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Server
{
    sealed class BanWrite
    {
        public bool Ok;
        public string Code;
        public int BanId;
        public string ExpiresAt;
    }

    sealed class BanHit
    {
        public bool Banned;
        public string Code;
        public int BanId;
        public int AccountId;
        public string Name;
        public string Reason;
        public string ByName;
        public string Created;
        public string Expires;
        public string MatchedType;
    }

    static class Bans
    {
        const string ActiveSql =
            "SELECT id FROM bans WHERE name = ? AND active = 1 AND (expires_at IS NULL OR expires_at > NOW()) LIMIT 1";

        const string InsertSql =
            "INSERT INTO bans (account_id, name, ip, reason, by_account_id, by_name, expires_at) " +
            "SELECT ?, ?, ?, ?, ?, ?, IF(? > 0, NOW() + INTERVAL ? SECOND, NULL) FROM DUAL " +
            "WHERE NOT EXISTS (SELECT 1 FROM bans WHERE name = ? AND active = 1 AND (expires_at IS NULL OR expires_at > NOW()))";

        const string IdentifierSql =
            "INSERT INTO ban_identifiers (ban_id, type, value) " +
            "SELECT ?, jt.type, jt.value FROM JSON_TABLE(?, '$[*]' COLUMNS (" +
            "type VARCHAR(16) PATH '$.type', value VARCHAR(255) PATH '$.value')) jt " +
            "WHERE jt.type IN ('steam','discord','xbl','live','license','license2','fivem','ip','token') " +
            "AND jt.value IS NOT NULL AND CHAR_LENGTH(jt.value) BETWEEN 1 AND 255";

        const string ActionSql =
            "INSERT INTO staff_actions (actor_account_id, actor_name, action, target_name, target_account_id, details) " +
            "SELECT ?, ?, 'ban', b.name, b.account_id, CONCAT(b.reason, ' (', " +
            "IF(b.expires_at IS NULL, 'forever', CONCAT('until ', DATE_FORMAT(b.expires_at, '%Y-%m-%d %H:%i'))), ')') " +
            "FROM bans b WHERE b.id = ?";

        const string ExpiresSql =
            "SELECT DATE_FORMAT(expires_at, '%Y-%m-%d %H:%i:%s') AS expires_at FROM bans WHERE id = ?";

        const string MatchSql =
            "SELECT b.id, b.account_id, b.name, b.reason, b.by_name, " +
            "DATE_FORMAT(b.created_at, '%Y-%m-%d %H:%i') AS created, " +
            "DATE_FORMAT(b.expires_at, '%Y-%m-%d %H:%i') AS expires, bi.type AS matched_type " +
            "FROM JSON_TABLE(?, '$[*]' COLUMNS (type VARCHAR(16) PATH '$.type', value VARCHAR(255) PATH '$.value')) jt " +
            "INNER JOIN ban_identifiers bi ON bi.type = jt.type AND bi.value = jt.value " +
            "INNER JOIN bans b ON b.id = bi.ban_id " +
            "WHERE b.active = 1 AND (b.expires_at IS NULL OR b.expires_at > NOW()) " +
            "AND jt.type IN ('steam','discord','xbl','live','license','license2','fivem','ip','token') " +
            "AND jt.value IS NOT NULL AND CHAR_LENGTH(jt.value) BETWEEN 1 AND 255 " +
            "ORDER BY b.created_at DESC, b.id DESC LIMIT 1";

        public static void Place(string name, int accountId, string reason, int byAccountId, string byName, int seconds, Dictionary<string, string> identifiers, string[] tokens, Action<BanWrite> done)
        {
            name = name == null ? "" : name.Trim();
            if (name.Length < 1 || name.Length > 24)
            {
                done(Write("BAD_NAME"));
                return;
            }

            if (reason == null)
                reason = "";
            if (reason.Length > 128)
                reason = reason.Substring(0, 128);

            byName = byName == null ? "" : byName.Trim();
            if (byName.Length == 0)
                byName = "Server";
            if (byName.Length > 24)
                byName = byName.Substring(0, 24);

            if (seconds < 0)
                seconds = 0;

            var keys = Keys(identifiers, tokens);
            string ip = null;
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i].Type == "ip" && keys[i].Value.Length <= 45)
                    ip = keys[i].Value;
            }

            object account = accountId > 0 ? (object)accountId : null;
            object actor = byAccountId > 0 ? (object)byAccountId : null;
            MySql.Insert(
                InsertSql,
                new object[] { account, name, ip, reason, actor, byName, seconds, seconds, name },
                new Action<dynamic>(id =>
                {
                    int banId = ToId(id);
                    if (banId <= 0)
                    {
                        MySql.Single(ActiveSql, new object[] { name }, new Action<dynamic>(row =>
                        {
                            if (row != null)
                                done(Write("ALREADY_BANNED"));
                            else
                                done(Write("DB_ERROR"));
                        }));
                        return;
                    }

                    var queries = new object[]
                    {
                        new object[] { IdentifierSql, new object[] { banId, Json(keys) } },
                        new object[] { ActionSql, new object[] { actor, byName, banId } }
                    };
                    MySql.Transaction(queries, new object[0], new Action<dynamic>(committed =>
                    {
                        if (!Truth(committed))
                        {
                            MySql.Update(
                                "DELETE FROM bans WHERE id = ?",
                                new object[] { banId },
                                new Action<dynamic>(unused => done(Write("DB_ERROR"))));
                            return;
                        }

                        MySql.Single(ExpiresSql, new object[] { banId }, new Action<dynamic>(row =>
                        {
                            var result = Write("OK");
                            result.Ok = true;
                            result.BanId = banId;
                            result.ExpiresAt = row == null ? null : MySql.Text(row, "expires_at");
                            done(result);
                        }));
                    }));
                }));
        }

        public static void Match(Dictionary<string, string> identifiers, string[] tokens, Action<BanHit> done)
        {
            var keys = Keys(identifiers, tokens);
            if (keys.Count == 0)
            {
                done(Hit("CLEAR"));
                return;
            }

            MySql.Single(MatchSql, new object[] { Json(keys) }, new Action<dynamic>(row =>
            {
                if (row == null)
                {
                    done(Hit("CLEAR"));
                    return;
                }

                int banId = MySql.Int(row, "id");
                if (banId <= 0)
                {
                    done(Hit("DB_ERROR"));
                    return;
                }

                string expires = MySql.Text(row, "expires");
                done(new BanHit
                {
                    Banned = true,
                    Code = "BANNED",
                    BanId = banId,
                    AccountId = MySql.Int(row, "account_id"),
                    Name = MySql.Text(row, "name"),
                    Reason = MySql.Text(row, "reason") ?? "",
                    ByName = MySql.Text(row, "by_name") ?? "Server",
                    Created = MySql.Text(row, "created") ?? "",
                    Expires = string.IsNullOrEmpty(expires) ? null : expires,
                    MatchedType = MySql.Text(row, "matched_type")
                });
            }));
        }

        public static string Message(BanHit hit)
        {
            string by = hit == null || string.IsNullOrEmpty(hit.ByName) ? "Server" : hit.ByName;
            string created = hit == null || hit.Created == null ? "" : hit.Created;
            string expires = hit == null || string.IsNullOrEmpty(hit.Expires) ? "Never" : hit.Expires;
            string reason = hit == null || hit.Reason == null ? "" : hit.Reason;
            return "You are banned (since " + created + " by " + by + ").\nReason: " + reason + "\nUnban: " + expires;
        }

        public static void Probe(Action<string> done)
        {
            new ProbeRun(done).Start();
        }

        static List<Pair> Keys(Dictionary<string, string> identifiers, string[] tokens)
        {
            var list = new List<Pair>();
            var seen = new HashSet<string>();
            if (identifiers != null)
            {
                for (int i = 0; i < Identifier.Types.Length; i++)
                {
                    string type = Identifier.Types[i];
                    string value;
                    if (!identifiers.TryGetValue(type, out value))
                        continue;
                    Add(list, seen, type, value);
                }
            }

            if (tokens != null)
            {
                for (int i = 0; i < tokens.Length; i++)
                    Add(list, seen, "token", tokens[i]);
            }

            return list;
        }

        static void Add(List<Pair> list, HashSet<string> seen, string type, string value)
        {
            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(value) || value.Length > 255)
                return;
            if (!seen.Add(type + "\n" + value))
                return;
            list.Add(new Pair { Type = type, Value = value });
        }

        static string Json(List<Pair> keys)
        {
            var body = new StringBuilder();
            body.Append('[');
            for (int i = 0; i < keys.Count; i++)
            {
                if (i > 0)
                    body.Append(',');
                body.Append("{\"type\":").Append(Json(keys[i].Type)).Append(",\"value\":").Append(Json(keys[i].Value)).Append('}');
            }

            body.Append(']');
            return body.ToString();
        }

        static string Json(string value)
        {
            if (value == null)
                return "\"\"";
            var escaped = new StringBuilder(value.Length + 2);
            escaped.Append('"');
            foreach (char c in value)
            {
                if (c == '\\' || c == '"')
                    escaped.Append('\\').Append(c);
                else if (c == '\n')
                    escaped.Append("\\n");
                else if (c == '\r')
                    escaped.Append("\\r");
                else if (c == '\t')
                    escaped.Append("\\t");
                else
                    escaped.Append(c);
            }

            escaped.Append('"');
            return escaped.ToString();
        }

        static bool Truth(dynamic result)
        {
            if (result is bool)
                return (bool)result;
            if (result == null)
                return false;
            try
            {
                return Convert.ToInt32(result, CultureInfo.InvariantCulture) != 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static int ToId(object value)
        {
            if (value == null)
                return 0;
            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        static BanWrite Write(string code)
        {
            return new BanWrite { Code = code };
        }

        static BanHit Hit(string code)
        {
            return new BanHit { Code = code };
        }

        sealed class Pair
        {
            public string Type;
            public string Value;
        }

        sealed class ProbeRun
        {
            const string NameA = "f019a";
            const string NameB = "f019b";
            const string Token = "f019-token-a";
            const string Reason = "f019 probe";
            const string ReasonB = "f019 hour";

            readonly Action<string> _done;
            readonly Dictionary<string, string> _ids = new Dictionary<string, string>();
            readonly string[] _tokens = new string[] { Token, Token, "" };
            bool _finished;
            int _ban;
            int _timed;

            public ProbeRun(Action<string> done)
            {
                _done = done;
                for (int i = 0; i < Identifier.Types.Length; i++)
                    _ids[Identifier.Types[i]] = "f019-a-" + Identifier.Types[i];
            }

            public void Start()
            {
                Wipe(() => Place(NameA, 19, Reason, 2, "F019", 0, _ids, _tokens, new Action<BanWrite>(First)));
            }

            void First(BanWrite result)
            {
                if (result == null || !result.Ok || result.Code != "OK" || result.BanId <= 0 || result.ExpiresAt != null)
                {
                    Fail("place");
                    return;
                }

                _ban = result.BanId;
                MySql.Scalar(
                    "SELECT COUNT(*) FROM ban_identifiers WHERE ban_id = ?",
                    new object[] { _ban },
                    new Action<dynamic>(count =>
                    {
                        if (ToId(count) != Identifier.Types.Length + 1)
                        {
                            Fail("identifiers");
                            return;
                        }

                        MySql.Single(
                            "SELECT ip, active, reason, by_name, by_account_id, account_id FROM bans WHERE id = ?",
                            new object[] { _ban },
                            new Action<dynamic>(row =>
                            {
                                if (row == null || MySql.Text(row, "ip") != _ids["ip"] || MySql.Int(row, "active") != 1
                                    || MySql.Text(row, "reason") != Reason || MySql.Text(row, "by_name") != "F019"
                                    || MySql.Int(row, "by_account_id") != 2 || MySql.Int(row, "account_id") != 19)
                                {
                                    Fail("row");
                                    return;
                                }

                                MySql.Single(
                                    "SELECT details FROM staff_actions WHERE action = 'ban' AND target_name = ? AND target_account_id = 19",
                                    new object[] { NameA },
                                    new Action<dynamic>(action =>
                                    {
                                        string details = action == null ? null : MySql.Text(action, "details");
                                        if (details != Reason + " (forever)")
                                        {
                                            Fail("action");
                                            return;
                                        }

                                        Place(NameA, 19, Reason, 2, "F019", 0, _ids, _tokens, new Action<BanWrite>(Again));
                                    }));
                            }));
                    }));
            }

            void Again(BanWrite result)
            {
                if (result == null || result.Ok || result.Code != "ALREADY_BANNED")
                {
                    Fail("duplicate");
                    return;
                }

                MySql.Scalar(
                    "SELECT COUNT(*) FROM staff_actions WHERE action = 'ban' AND target_name = ?",
                    new object[] { NameA },
                    new Action<dynamic>(count =>
                    {
                        if (ToId(count) != 1)
                        {
                            Fail("duplicate");
                            return;
                        }

                        var licenseOnly = new Dictionary<string, string>();
                        licenseOnly["license"] = _ids["license"];
                        Match(licenseOnly, null, new Action<BanHit>(license =>
                        {
                            if (!Same(license, _ban, "license"))
                            {
                                Fail("license");
                                return;
                            }

                            var other = new Dictionary<string, string>();
                            other["license"] = "f019-other-license";
                            Match(other, new string[] { Token }, new Action<BanHit>(token =>
                            {
                                if (!Same(token, _ban, "token"))
                                {
                                    Fail("token");
                                    return;
                                }

                                var ipOnly = new Dictionary<string, string>();
                                ipOnly["ip"] = _ids["ip"];
                                Match(ipOnly, null, new Action<BanHit>(ip =>
                                {
                                    if (!Same(ip, _ban, "ip"))
                                    {
                                        Fail("ip");
                                        return;
                                    }

                                    var miss = new Dictionary<string, string>();
                                    miss["license"] = "f019-clear-license";
                                    Match(miss, new string[] { "f019-clear-token" }, new Action<BanHit>(clear =>
                                    {
                                        if (clear == null || clear.Banned || clear.Code != "CLEAR")
                                        {
                                            Fail("clear");
                                            return;
                                        }

                                        Timed();
                                    }));
                                }));
                            }));
                        }));
                    }));
            }

            void Timed()
            {
                var ids = new Dictionary<string, string>();
                ids["license"] = "f019-b-license";
                Place(NameB, 0, ReasonB, 0, "", 3600, ids, null, new Action<BanWrite>(result =>
                {
                    if (result == null || !result.Ok || result.BanId <= 0 || string.IsNullOrEmpty(result.ExpiresAt))
                    {
                        Fail("timed");
                        return;
                    }

                    _timed = result.BanId;
                    MySql.Single(
                        "SELECT details FROM staff_actions WHERE action = 'ban' AND target_name = ?",
                        new object[] { NameB },
                        new Action<dynamic>(action =>
                        {
                            string details = action == null ? null : MySql.Text(action, "details");
                            if (details == null || details.IndexOf(ReasonB + " (until ", StringComparison.Ordinal) != 0)
                            {
                                Fail("until");
                                return;
                            }

                            var idsB = new Dictionary<string, string>();
                            idsB["license"] = "f019-b-license";
                            Match(idsB, null, new Action<BanHit>(hit =>
                            {
                                if (!Same(hit, _timed, "license"))
                                {
                                    Fail("timed_hit");
                                    return;
                                }

                                MySql.Update(
                                    "UPDATE bans SET expires_at = NOW() - INTERVAL 1 MINUTE WHERE id = ?",
                                    new object[] { _timed },
                                    new Action<dynamic>(unused =>
                                    {
                                        Match(idsB, null, new Action<BanHit>(expired =>
                                        {
                                            if (expired == null || expired.Banned || expired.Code != "CLEAR")
                                            {
                                                Fail("expired");
                                                return;
                                            }

                                            MySql.Update(
                                                "UPDATE bans SET active = 0 WHERE id = ?",
                                                new object[] { _ban },
                                                new Action<dynamic>(unused2 =>
                                                {
                                                    Match(_ids, _tokens, new Action<BanHit>(lifted =>
                                                    {
                                                        if (lifted == null || lifted.Banned || lifted.Code != "CLEAR")
                                                        {
                                                            Fail("inactive");
                                                            return;
                                                        }

                                                        string message = Message(new BanHit
                                                        {
                                                            ByName = "F019",
                                                            Created = "2026-11-06 12:00",
                                                            Reason = Reason,
                                                            Expires = null
                                                        });
                                                        if (message.IndexOf("You are banned (since 2026-11-06 12:00 by F019).", StringComparison.Ordinal) != 0
                                                            || message.IndexOf("Unban: Never", StringComparison.Ordinal) < 0)
                                                        {
                                                            Fail("message");
                                                            return;
                                                        }

                                                        Cleanup(true);
                                                    }));
                                                }));
                                        }));
                                    }));
                            }));
                        }));
                }));
            }

            static bool Same(BanHit hit, int banId, string type)
            {
                return hit != null && hit.Banned && hit.Code == "BANNED" && hit.BanId == banId && hit.MatchedType == type;
            }

            void Fail(string step)
            {
                Cleanup(false, step);
            }

            void Wipe(Action next)
            {
                MySql.Update(
                    "DELETE FROM staff_actions WHERE target_name IN (?, ?)",
                    new object[] { NameA, NameB },
                    new Action<dynamic>(unused =>
                    {
                        MySql.Update(
                            "DELETE FROM bans WHERE name IN (?, ?)",
                            new object[] { NameA, NameB },
                            new Action<dynamic>(unused2 => next()));
                    }));
            }

            void Cleanup(bool ok, string step = null)
            {
                Wipe(() =>
                {
                    MySql.Scalar(
                        "SELECT COUNT(*) FROM bans WHERE name IN (?, ?)",
                        new object[] { NameA, NameB },
                        new Action<dynamic>(bans =>
                        {
                            MySql.Scalar(
                                "SELECT COUNT(*) FROM ban_identifiers WHERE value LIKE 'f019-%'",
                                new object[0],
                                new Action<dynamic>(ids =>
                                {
                                    MySql.Scalar(
                                        "SELECT COUNT(*) FROM staff_actions WHERE target_name IN (?, ?)",
                                        new object[] { NameA, NameB },
                                        new Action<dynamic>(actions =>
                                        {
                                            int left = ToId(bans) + ToId(ids) + ToId(actions);
                                            if (!ok)
                                            {
                                                Finish("failed step=" + step + " left=" + left.ToString(CultureInfo.InvariantCulture));
                                                return;
                                            }

                                            if (left != 0)
                                            {
                                                Finish("failed step=cleanup left=" + left.ToString(CultureInfo.InvariantCulture));
                                                return;
                                            }

                                            Finish("ok place=1 identifiers=9 forever=1 duplicate=1 license=1 token=1 ip=1 clear=1 timed=1 expired=1 inactive=1 cleaned=1");
                                        }));
                                }));
                        }));
                });
            }

            void Finish(string line)
            {
                if (_finished)
                    return;
                _finished = true;
                _done(line);
            }

            static int ToId(object value)
            {
                if (value == null)
                    return 0;
                try
                {
                    return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }
    }
}
