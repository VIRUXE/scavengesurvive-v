using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Server
{
    sealed class RecordResult
    {
        public bool Ok;
        public int IdentifierCount;
        public int TokenCount;
        public int TakenCount;
    }

    static class Links
    {
        const string IdentifierSql =
            "INSERT INTO account_identifiers (account_id, type, value) " +
            "SELECT p.account_id, p.type, p.value FROM (" +
            "SELECT ? AS account_id, jt.type, jt.value FROM JSON_TABLE(?, '$[*]' COLUMNS (" +
            "type VARCHAR(16) PATH '$.type', value VARCHAR(255) PATH '$.value')) jt) p " +
            "LEFT JOIN account_identifiers owned ON owned.type = p.type AND owned.value = p.value AND owned.account_id <> p.account_id " +
            "WHERE owned.account_id IS NULL AND p.type IN ('steam','discord','xbl','live','license','license2','fivem','ip') " +
            "AND p.value IS NOT NULL AND CHAR_LENGTH(p.value) BETWEEN 1 AND 255 " +
            "ON DUPLICATE KEY UPDATE value = VALUES(value), times_used = account_identifiers.times_used + 1";

        const string TokenSql =
            "INSERT INTO account_tokens (account_id, token) " +
            "SELECT ?, jt.token FROM JSON_TABLE(?, '$[*]' COLUMNS (token VARCHAR(255) PATH '$')) jt " +
            "WHERE jt.token IS NOT NULL AND CHAR_LENGTH(jt.token) BETWEEN 1 AND 255 " +
            "ON DUPLICATE KEY UPDATE times_used = account_tokens.times_used + 1";

        public static void Record(int accountId, Dictionary<string, string> identifiers, string[] tokens, Action<RecordResult> done)
        {
            var attempted = new Dictionary<string, string>();
            if (identifiers != null)
            {
                for (int i = 0; i < Identifier.Types.Length; i++)
                {
                    string type = Identifier.Types[i];
                    string value;
                    if (!identifiers.TryGetValue(type, out value)) continue;
                    if (string.IsNullOrEmpty(value) || value.Length > 255) continue;
                    attempted[type] = value;
                }
            }

            var tokenList = new List<string>();
            var seen = new HashSet<string>();
            if (tokens != null)
            {
                for (int i = 0; i < tokens.Length; i++)
                {
                    string token = tokens[i];
                    if (string.IsNullOrEmpty(token) || token.Length > 255 || !seen.Add(token)) continue;
                    tokenList.Add(token);
                }
            }

            if (accountId <= 0)
            {
                done(new RecordResult());
                return;
            }

            string identifierJson = IdentifierJson(attempted);
            string tokenJson = TokenJson(tokenList);
            var queries = new object[]
            {
                new object[] { IdentifierSql, new object[] { accountId, identifierJson } },
                new object[] { TokenSql, new object[] { accountId, tokenJson } }
            };

            MySql.Transaction(queries, new object[0], new Action<dynamic>(committed =>
            {
                object committedValue = committed;
                if (!Truth(committedValue))
                {
                    done(new RecordResult());
                    return;
                }

                MySql.Query(
                    "SELECT type, value FROM account_identifiers WHERE account_id = ?",
                    new object[] { accountId },
                    new Action<dynamic>(idRows =>
                    {
                        MySql.Query(
                            "SELECT token FROM account_tokens WHERE account_id = ?",
                            new object[] { accountId },
                            new Action<dynamic>(tokenRows =>
                            {
                                object idList = idRows;
                                object tokenListRows = tokenRows;
                                var stored = new Dictionary<string, string>();
                                EachRow(idList, row =>
                                {
                                    string type = MySql.Text(row, "type");
                                    string value = MySql.Text(row, "value");
                                    if (!string.IsNullOrEmpty(type) && value != null)
                                        stored[type] = value;
                                });

                                var haveTokens = new HashSet<string>();
                                EachRow(tokenListRows, row =>
                                {
                                    string token = MySql.Text(row, "token");
                                    if (!string.IsNullOrEmpty(token))
                                        haveTokens.Add(token);
                                });

                                int matched = 0;
                                int taken = 0;
                                foreach (KeyValuePair<string, string> pair in attempted)
                                {
                                    string value;
                                    if (stored.TryGetValue(pair.Key, out value) && value == pair.Value)
                                        matched++;
                                    else
                                        taken++;
                                }

                                int tokenCount = 0;
                                for (int i = 0; i < tokenList.Count; i++)
                                {
                                    if (haveTokens.Contains(tokenList[i]))
                                        tokenCount++;
                                }

                                done(new RecordResult
                                {
                                    Ok = true,
                                    IdentifierCount = matched,
                                    TokenCount = tokenCount,
                                    TakenCount = taken
                                });
                            }));
                    }));
            }));
        }

        public static void Probe(Action<string> done)
        {
            new ProbeRun(done).Start();
        }

        internal static string IdentifierJson(Dictionary<string, string> attempted)
        {
            var body = new StringBuilder();
            body.Append('[');
            bool first = true;
            for (int i = 0; i < Identifier.Types.Length; i++)
            {
                string type = Identifier.Types[i];
                string value;
                if (!attempted.TryGetValue(type, out value)) continue;
                if (!first) body.Append(',');
                first = false;
                body.Append("{\"type\":").Append(Json(type)).Append(",\"value\":").Append(Json(value)).Append('}');
            }

            body.Append(']');
            return body.ToString();
        }

        static string TokenJson(List<string> tokens)
        {
            var body = new StringBuilder();
            body.Append('[');
            for (int i = 0; i < tokens.Count; i++)
            {
                if (i > 0) body.Append(',');
                body.Append(Json(tokens[i]));
            }

            body.Append(']');
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

        static bool Truth(dynamic result)
        {
            if (result is bool) return (bool)result;
            if (result == null) return false;
            try
            {
                return Convert.ToInt32(result, CultureInfo.InvariantCulture) != 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static void EachRow(object result, Action<dynamic> row)
        {
            if (result == null || row == null) return;
            if (result is string) return;

            IDictionary<string, object> one = result as IDictionary<string, object>;
            if (one != null)
            {
                row(result);
                return;
            }

            IEnumerable rows = result as IEnumerable;
            if (rows == null) return;
            foreach (object item in rows)
            {
                if (item != null)
                    row(item);
            }
        }

        sealed class ProbeRun
        {
            const string NameA = "f016a";
            const string NameB = "f016b";
            static readonly string HashA = new string('x', 60);
            static readonly string HashB = new string('y', 60);

            readonly Action<string> _done;
            bool _finished;
            int _a;
            int _b;
            readonly Dictionary<string, string> _idsA = new Dictionary<string, string>();
            readonly string[] _tokensA = new string[] { "f016-token-a", "f016-token-b" };

            public ProbeRun(Action<string> done)
            {
                _done = done;
                for (int i = 0; i < Identifier.Types.Length; i++)
                    _idsA[Identifier.Types[i]] = "f016-a-" + Identifier.Types[i];
            }

            public void Start()
            {
                MySql.Update(
                    "DELETE FROM accounts WHERE name IN (?, ?)",
                    new object[] { NameA, NameB },
                    new Action<dynamic>(unused => InsertA()));
            }

            void InsertA()
            {
                MySql.Insert(
                    "INSERT INTO accounts (name, password_hash) VALUES (?, ?)",
                    new object[] { NameA, HashA },
                    new Action<dynamic>(id =>
                    {
                        _a = ToId(id);
                        if (_a <= 0)
                        {
                            Fail("account_a");
                            return;
                        }

                        InsertB();
                    }));
            }

            void InsertB()
            {
                MySql.Insert(
                    "INSERT INTO accounts (name, password_hash) VALUES (?, ?)",
                    new object[] { NameB, HashB },
                    new Action<dynamic>(id =>
                    {
                        _b = ToId(id);
                        if (_b <= 0)
                        {
                            Fail("account_b");
                            return;
                        }

                        Record(_a, _idsA, _tokensA, new Action<RecordResult>(First));
                    }));
            }

            void First(RecordResult result)
            {
                if (result == null || !result.Ok || result.IdentifierCount != Identifier.Types.Length || result.TokenCount != 2 || result.TakenCount != 0)
                {
                    Fail("first");
                    return;
                }

                Expect(_a, _idsA, 1, _tokensA, 1, "first_rows", () =>
                    Record(_a, _idsA, _tokensA, new Action<RecordResult>(Second)));
            }

            void Second(RecordResult result)
            {
                if (result == null || !result.Ok || result.IdentifierCount != Identifier.Types.Length || result.TokenCount != 2 || result.TakenCount != 0)
                {
                    Fail("reuse");
                    return;
                }

                Expect(_a, _idsA, 2, _tokensA, 2, "reuse_rows", () =>
                {
                    var idsB = new Dictionary<string, string>();
                    idsB["license"] = _idsA["license"];
                    idsB["discord"] = "f016-b-discord";
                    var tokensB = new string[] { "f016-token-a", "f016-token-c" };
                    Record(_b, idsB, tokensB, new Action<RecordResult>(resultB => Third(resultB, idsB)));
                });
            }

            void Third(RecordResult result, Dictionary<string, string> idsB)
            {
                if (result == null || !result.Ok || result.IdentifierCount != 1 || result.TokenCount != 2 || result.TakenCount != 1)
                {
                    Fail("taken");
                    return;
                }

                Expect(_a, _idsA, 2, _tokensA, 2, "owner_rows", () =>
                {
                    var storedB = new Dictionary<string, string>();
                    storedB["discord"] = idsB["discord"];
                    Expect(_b, storedB, 1, new string[] { "f016-token-a", "f016-token-c" }, 1, "taken_rows", Rollback);
                });
            }

            void Rollback()
            {
                var queries = new object[]
                {
                    new object[]
                    {
                        "INSERT INTO account_identifiers (account_id, type, value) VALUES (?, 'ip', 'f016-rollback')",
                        new object[] { _b }
                    },
                    new object[]
                    {
                        "UPDATE accounts SET no_such_column = 1 WHERE id = ?",
                        new object[] { _b }
                    }
                };

                MySql.Transaction(queries, new object[0], new Action<dynamic>(committed =>
                {
                    object committedValue = committed;
                    if (Truth(committedValue))
                    {
                        Fail("rollback_committed");
                        return;
                    }

                    MySql.Scalar(
                        "SELECT COUNT(*) FROM account_identifiers WHERE value = ?",
                        new object[] { "f016-rollback" },
                        new Action<dynamic>(count =>
                        {
                            if (ToId(count) != 0)
                            {
                                Fail("rollback_left");
                                return;
                            }

                            Cleanup(true);
                        }));
                }));
            }

            void Expect(int accountId, Dictionary<string, string> identifiers, int times, string[] tokens, int tokenTimes, string step, Action next)
            {
                MySql.Query(
                    "SELECT type, value, times_used FROM account_identifiers WHERE account_id = ?",
                    new object[] { accountId },
                    new Action<dynamic>(idRows =>
                    {
                        object idList = idRows;
                        var found = new Dictionary<string, string>();
                        var used = new Dictionary<string, int>();
                        int rows = 0;
                        EachRow(idList, row =>
                        {
                            rows++;
                            string type = MySql.Text(row, "type");
                            if (string.IsNullOrEmpty(type)) return;
                            found[type] = MySql.Text(row, "value");
                            used[type] = ToId(MySql.Field(row, "times_used"));
                        });

                        if (rows != identifiers.Count)
                        {
                            Fail(step);
                            return;
                        }

                        foreach (KeyValuePair<string, string> pair in identifiers)
                        {
                            string value;
                            int usedTimes;
                            if (!found.TryGetValue(pair.Key, out value) || value != pair.Value || !used.TryGetValue(pair.Key, out usedTimes) || usedTimes != times)
                            {
                                Fail(step);
                                return;
                            }
                        }

                        MySql.Query(
                            "SELECT token, times_used FROM account_tokens WHERE account_id = ?",
                            new object[] { accountId },
                            new Action<dynamic>(tokenRows =>
                            {
                                object tokenListRows = tokenRows;
                                var have = new Dictionary<string, int>();
                                int tokenRowsCount = 0;
                                EachRow(tokenListRows, row =>
                                {
                                    tokenRowsCount++;
                                    string token = MySql.Text(row, "token");
                                    if (!string.IsNullOrEmpty(token))
                                        have[token] = ToId(MySql.Field(row, "times_used"));
                                });

                                if (tokenRowsCount != tokens.Length)
                                {
                                    Fail(step);
                                    return;
                                }

                                for (int i = 0; i < tokens.Length; i++)
                                {
                                    int usedTimes;
                                    if (!have.TryGetValue(tokens[i], out usedTimes) || usedTimes != tokenTimes)
                                    {
                                        Fail(step);
                                        return;
                                    }
                                }

                                next();
                            }));
                    }));
            }

            void Fail(string step)
            {
                Cleanup(false, step);
            }

            void Cleanup(bool ok, string step = null)
            {
                MySql.Update(
                    "DELETE FROM accounts WHERE name IN (?, ?)",
                    new object[] { NameA, NameB },
                    new Action<dynamic>(unused =>
                    {
                        MySql.Scalar(
                            "SELECT COUNT(*) FROM account_identifiers WHERE value LIKE 'f016-%' OR value = 'f016-rollback'",
                            new object[0],
                            new Action<dynamic>(ids =>
                            {
                                MySql.Scalar(
                                    "SELECT COUNT(*) FROM account_tokens WHERE token LIKE 'f016-%'",
                                    new object[0],
                                    new Action<dynamic>(tokenCount =>
                                    {
                                        MySql.Scalar(
                                            "SELECT COUNT(*) FROM accounts WHERE name IN (?, ?)",
                                            new object[] { NameA, NameB },
                                            new Action<dynamic>(accounts =>
                                            {
                                                int left = ToId(ids) + ToId(tokenCount) + ToId(accounts);
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

                                                Finish("ok identifiers=8 tokens=2 reused=2 taken=1 shared_token=1 rollback=1 cleaned=1");
                                            }));
                                    }));
                            }));
                    }));
            }

            void Finish(string line)
            {
                if (_finished) return;
                _finished = true;
                _done(line);
            }

            static int ToId(dynamic value)
            {
                if (value == null) return 0;
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
