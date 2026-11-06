using System;
using System.Collections.Generic;
using System.Globalization;

namespace Server
{
    sealed class AccountResult
    {
        public bool Ok;
        public string Code;
        public int AccountId;
        public string Name;
        public int AdminLevel;
        public int Attempts;
    }

    static class Accounts
    {
        public const int MaxAttempts = 5;
        const long LockTicks = 10L * 60L * TimeSpan.TicksPerSecond;

        const string LookupSql =
            "SELECT id, name, password_hash, admin_level, active FROM accounts WHERE name = ?";

        const string InsertSql =
            "INSERT INTO accounts (name, password_hash, last_ip, last_login_at) VALUES (?, ?, ?, current_timestamp())";

        const string TouchSql =
            "UPDATE accounts SET last_login_at = current_timestamp(), last_ip = ? WHERE id = ?";

        sealed class Failures
        {
            public int Count;
            public long Since;
        }

        static readonly Dictionary<string, Failures> Attempts = new Dictionary<string, Failures>();

        public static void Register(string name, string password, string ip, Func<bool> present, Action<AccountResult> done)
        {
            if (!Password.NameOk(name))
            {
                done(Result("BAD_NAME"));
                return;
            }

            if (!Password.PasswordOk(password))
            {
                done(Result("WEAK_PASSWORD"));
                return;
            }

            MySql.Single(LookupSql, new object[] { name }, new Action<dynamic>(row =>
            {
                if (row != null)
                {
                    done(Result("EXISTS"));
                    return;
                }

                Password.Hash(password, hash =>
                {
                    if (present != null && !present())
                    {
                        done(Result("GONE"));
                        return;
                    }

                    if (string.IsNullOrEmpty(hash) || hash.Length != 60)
                    {
                        done(Result("DB_ERROR"));
                        return;
                    }

                    MySql.Insert(InsertSql, new object[] { name, hash, Ip(ip) }, new Action<dynamic>(id =>
                    {
                        object idValue = id;
                        int accountId = ToId(idValue);
                        if (accountId <= 0)
                        {
                            done(Result("DB_ERROR"));
                            return;
                        }

                        done(new AccountResult
                        {
                            Ok = true,
                            Code = "OK",
                            AccountId = accountId,
                            Name = name,
                            AdminLevel = 0
                        });
                    }));
                });
            }));
        }

        public static void Login(string attemptKey, string name, string password, string ip, Func<bool> present, Action<AccountResult> done)
        {
            string key = attemptKey ?? "";
            Failures locked = Current(key);
            if (locked != null && locked.Count >= MaxAttempts)
            {
                done(Result("TOO_MANY_ATTEMPTS", locked.Count));
                return;
            }

            if (password == null)
                password = "";

            int bytes = System.Text.Encoding.UTF8.GetByteCount(password);
            if (bytes < 4 || bytes > 72 || password.IndexOf('\0') >= 0)
            {
                Fail(key, 0, done);
                return;
            }

            if (!Password.NameOk(name))
            {
                done(Result("BAD_NAME"));
                return;
            }

            MySql.Single(LookupSql, new object[] { name }, new Action<dynamic>(row =>
            {
                if (row == null)
                {
                    done(Result("NO_ACCOUNT"));
                    return;
                }

                int accountId = MySql.Int(row, "id");
                if (MySql.Int(row, "active") == 0)
                {
                    var inactive = Result("INACTIVE");
                    inactive.AccountId = accountId;
                    inactive.Name = MySql.Text(row, "name") ?? name;
                    done(inactive);
                    return;
                }

                string hash = MySql.Text(row, "password_hash");
                string stored = MySql.Text(row, "name") ?? name;
                int admin = MySql.Int(row, "admin_level");
                Password.Verify(password, hash, match =>
                {
                    if (!match)
                    {
                        Fail(key, accountId, done);
                        return;
                    }

                    Attempts.Remove(key);
                    if (present != null && !present())
                    {
                        done(Result("GONE"));
                        return;
                    }

                    MySql.Update(TouchSql, new object[] { Ip(ip), accountId }, new Action<dynamic>(unused =>
                    {
                        done(new AccountResult
                        {
                            Ok = true,
                            Code = "OK",
                            AccountId = accountId,
                            Name = stored,
                            AdminLevel = admin
                        });
                    }));
                });
            }));
        }

        public static void Forget(string attemptKey)
        {
            Attempts.Remove(attemptKey ?? "");
        }

        public static void Probe(Action<string> done)
        {
            new ProbeRun(done).Start();
        }

        static void Fail(string key, int accountId, Action<AccountResult> done)
        {
            Failures cur = Current(key);
            if (cur == null)
            {
                cur = new Failures { Since = DateTime.UtcNow.Ticks };
                Attempts[key] = cur;
            }

            cur.Count++;
            var result = Result("BAD_PASSWORD", cur.Count);
            result.AccountId = accountId;
            done(result);
        }

        static Failures Current(string key)
        {
            Failures found;
            if (!Attempts.TryGetValue(key, out found))
                return null;
            if (DateTime.UtcNow.Ticks - found.Since >= LockTicks)
            {
                Attempts.Remove(key);
                return null;
            }

            return found;
        }

        static object Ip(string ip)
        {
            if (string.IsNullOrEmpty(ip) || ip.Length > 45)
                return null;
            return ip;
        }

        static AccountResult Result(string code, int attempts = 0)
        {
            return new AccountResult { Code = code, Attempts = attempts };
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

        sealed class ProbeRun
        {
            const string Name = "f017a";
            const string Secret = "f017pass";
            const string Wrong = "f017nope";
            const string Key = "f017-probe";

            readonly Action<string> _done;
            bool _finished;
            int _id;
            int _bad;

            public ProbeRun(Action<string> done)
            {
                _done = done;
            }

            public void Start()
            {
                Forget(Key);
                Password.SelfCheck(ok =>
                {
                    if (!ok)
                    {
                        Fail("bcrypt");
                        return;
                    }

                    if (!Password.NameOk(Name) || Password.NameOk("has space") || Password.NameOk("")
                        || !Password.PasswordOk(Secret) || Password.PasswordOk("no"))
                    {
                        Fail("rules");
                        return;
                    }

                    MySql.Update(
                        "DELETE FROM accounts WHERE name = ?",
                        new object[] { Name },
                        new Action<dynamic>(unused => RegisterFirst()));
                });
            }

            void RegisterFirst()
            {
                Register(Name, Secret, null, () => true, new Action<AccountResult>(result =>
                {
                    if (result == null || !result.Ok || result.AccountId <= 0 || result.Code != "OK")
                    {
                        Fail("register");
                        return;
                    }

                    _id = result.AccountId;
                    Register(Name, Secret, null, () => true, new Action<AccountResult>(again =>
                    {
                        if (again == null || again.Ok || again.Code != "EXISTS")
                        {
                            Fail("exists");
                            return;
                        }

                        CheckHash();
                    }));
                }));
            }

            void CheckHash()
            {
                MySql.Single(
                    "SELECT CHAR_LENGTH(password_hash) AS hash_len, password_hash, admin_level, last_ip FROM accounts WHERE id = ?",
                    new object[] { _id },
                    new Action<dynamic>(row =>
                    {
                        if (row == null)
                        {
                            Fail("hash_row");
                            return;
                        }

                        object lenValue = MySql.Field((object)row, "hash_len");
                        int len = ToId(lenValue);
                        string hash = MySql.Text(row, "password_hash");
                        int admin = MySql.Int(row, "admin_level");
                        string ip = MySql.Text(row, "last_ip");
                        if (len != 60 || hash == null || hash.Length != 60 || !hash.StartsWith("$2b$10$", StringComparison.Ordinal)
                            || admin != 0 || ip != null)
                        {
                            Fail("hash_shape");
                            return;
                        }

                        Password.Verify(Secret, hash, match =>
                        {
                            if (!match)
                            {
                                Fail("hash_verify");
                                return;
                            }

                            Password.Verify(Wrong, hash, miss =>
                            {
                                if (miss)
                                {
                                    Fail("hash_reject");
                                    return;
                                }

                                LoginOk();
                            });
                        });
                    }));
            }

            void LoginOk()
            {
                Login(Key, Name, Secret, "203.0.113.17", () => true, new Action<AccountResult>(result =>
                {
                    if (result == null || !result.Ok || result.AccountId != _id || result.Name != Name || result.AdminLevel != 0)
                    {
                        Fail("login");
                        return;
                    }

                    MySql.Update(
                        "UPDATE accounts SET active = 0 WHERE id = ?",
                        new object[] { _id },
                        new Action<dynamic>(unused =>
                        {
                            Login(Key, Name, Secret, null, () => true, new Action<AccountResult>(inactive =>
                            {
                                if (inactive == null || inactive.Ok || inactive.Code != "INACTIVE" || inactive.AccountId != _id)
                                {
                                    Fail("inactive");
                                    return;
                                }

                                MySql.Update(
                                    "UPDATE accounts SET active = 1 WHERE id = ?",
                                    new object[] { _id },
                                    new Action<dynamic>(unused2 => Bad(1)));
                            }));
                        }));
                }));
            }

            void Bad(int n)
            {
                Login(Key, Name, Wrong, null, () => true, new Action<AccountResult>(result =>
                {
                    if (result == null || result.Ok || result.Code != "BAD_PASSWORD" || result.Attempts != n)
                    {
                        Fail("attempt");
                        return;
                    }

                    _bad = n;
                    if (n < MaxAttempts)
                    {
                        Bad(n + 1);
                        return;
                    }

                    Login(Key, Name, Wrong, null, () => true, new Action<AccountResult>(locked =>
                    {
                        if (locked == null || locked.Ok || locked.Code != "TOO_MANY_ATTEMPTS" || locked.Attempts != MaxAttempts)
                        {
                            Fail("locked");
                            return;
                        }

                        MySql.Single(
                            "SELECT last_ip FROM accounts WHERE id = ?",
                            new object[] { _id },
                            new Action<dynamic>(row =>
                            {
                                string ip = row == null ? null : MySql.Text(row, "last_ip");
                                if (ip != "203.0.113.17")
                                {
                                    Fail("last_ip");
                                    return;
                                }

                                Cleanup(true);
                            }));
                    }));
                }));
            }

            void Fail(string step)
            {
                Cleanup(false, step);
            }

            void Cleanup(bool ok, string step = null)
            {
                Forget(Key);
                MySql.Update(
                    "DELETE FROM accounts WHERE name = ?",
                    new object[] { Name },
                    new Action<dynamic>(unused =>
                    {
                        MySql.Scalar(
                            "SELECT COUNT(*) FROM accounts WHERE name = ?",
                            new object[] { Name },
                            new Action<dynamic>(count =>
                            {
                                object countValue = count;
                                int left = ToId(countValue);
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

                                Finish("ok bcrypt=1 cost10=1 length=60 prefix=2b register=1 exists=1 login=1 inactive=1 attempts="
                                    + _bad.ToString(CultureInfo.InvariantCulture)
                                    + " locked=1 last_ip=1 cleaned=1");
                            }));
                    }));
            }

            void Finish(string line)
            {
                if (_finished)
                    return;
                _finished = true;
                _done(line);
            }
        }
    }
}
