using System;
using System.Collections;
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

        const string AutoLoginKey = "auth.license_autologin";

        const string AutoLoginSql =
            "SELECT svalue FROM settings WHERE skey = 'auth.license_autologin'";

        const string LicenseSql =
            "SELECT a.id, a.name, a.admin_level, a.active " +
            "FROM account_identifiers i " +
            "INNER JOIN accounts a ON a.id = i.account_id " +
            "WHERE i.type = 'license' AND i.value = ?";

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

        // One license row resumes that account. Zero or many show the card.
        // The IP column is not a key. A missing setting stays on.
        public static void Resume(string license, string ip, Func<bool> present, Action<AccountResult> done)
        {
            if (string.IsNullOrEmpty(license))
            {
                done(Result("NONE"));
                return;
            }

            MySql.Single(AutoLoginSql, new object[0], new Action<dynamic>(setting =>
            {
                if (!AutoLoginOn(setting))
                {
                    done(Result("OFF"));
                    return;
                }

                MySql.Query(LicenseSql, new object[] { license }, new Action<dynamic>(rows =>
                {
                    object list = rows;
                    if (list == null || list is string)
                    {
                        done(Result("DB_ERROR"));
                        return;
                    }

                    int count = 0;
                    dynamic match = null;
                    EachRow(list, row =>
                    {
                        count++;
                        match = row;
                    });

                    if (count != 1 || match == null)
                    {
                        done(Result(count == 0 ? "NONE" : "MANY"));
                        return;
                    }

                    int accountId = MySql.Int(match, "id");
                    string stored = MySql.Text(match, "name");
                    int admin = MySql.Int(match, "admin_level");
                    if (accountId <= 0 || string.IsNullOrEmpty(stored))
                    {
                        done(Result("DB_ERROR"));
                        return;
                    }

                    if (MySql.Int(match, "active") == 0)
                    {
                        var inactive = Result("INACTIVE");
                        inactive.AccountId = accountId;
                        inactive.Name = stored;
                        done(inactive);
                        return;
                    }

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
                }));
            }));
        }

        public static void Probe(Action<string> done)
        {
            new ProbeRun(done).Start();
        }

        public static void ResumeProbe(Action<string> done)
        {
            new ResumeProbeRun(done).Start();
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

        static bool AutoLoginOn(dynamic row)
        {
            if (row == null)
                return true;
            string value = MySql.Text(row, "svalue");
            if (string.IsNullOrEmpty(value))
                return true;
            value = value.Trim();
            return !value.Equals("0", StringComparison.Ordinal)
                && !value.Equals("false", StringComparison.OrdinalIgnoreCase)
                && !value.Equals("off", StringComparison.OrdinalIgnoreCase);
        }

        static void EachRow(object result, Action<dynamic> row)
        {
            if (result == null || row == null)
                return;
            if (result is string)
                return;

            IDictionary<string, object> one = result as IDictionary<string, object>;
            if (one != null)
            {
                row(result);
                return;
            }

            IEnumerable rows = result as IEnumerable;
            if (rows == null)
                return;
            foreach (object item in rows)
            {
                if (item != null)
                    row(item);
            }
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

        sealed class ResumeProbeRun
        {
            const string NameA = "f018a";
            const string NameB = "f018b";
            const string License = "f018-lic";
            const string ResumeIp = "198.51.100.18";
            const string OtherIp = "203.0.113.44";
            static readonly string Hash = new string('r', 60);

            readonly Action<string> _done;
            bool _finished;
            int _account;
            string _auto;
            string _ipLogin;
            bool _autoMissing;

            public ResumeProbeRun(Action<string> done)
            {
                _done = done;
            }

            public void Start()
            {
                MySql.Single(AutoLoginSql, new object[0], new Action<dynamic>(auto =>
                {
                    _autoMissing = auto == null;
                    _auto = auto == null ? null : MySql.Text(auto, "svalue");
                    MySql.Single(
                        "SELECT svalue FROM settings WHERE skey = 'player.auto_ip_login'",
                        new object[0],
                        new Action<dynamic>(ip =>
                        {
                            _ipLogin = ip == null ? null : MySql.Text(ip, "svalue");
                            SetAuto("1", () => Wipe(Insert));
                        }));
                }));
            }

            void Insert()
            {
                MySql.Insert(
                    "INSERT INTO accounts (name, password_hash, admin_level, last_ip) VALUES (?, ?, 2, ?)",
                    new object[] { NameA, Hash, OtherIp },
                    new Action<dynamic>(id =>
                    {
                        object idValue = id;
                        _account = ToId(idValue);
                        if (_account <= 0)
                        {
                            Fail("insert_a");
                            return;
                        }

                        MySql.Insert(
                            "INSERT INTO accounts (name, password_hash, last_ip) VALUES (?, ?, ?)",
                            new object[] { NameB, Hash, ResumeIp },
                            new Action<dynamic>(idB =>
                            {
                                object idBValue = idB;
                                int other = ToId(idBValue);
                                if (other <= 0)
                                {
                                    Fail("insert_b");
                                    return;
                                }

                                MySql.Update(
                                    "INSERT INTO account_identifiers (account_id, type, value) VALUES (?, 'license', ?)",
                                    new object[] { _account, License },
                                    new Action<dynamic>(unused =>
                                    {
                                        MySql.Update(
                                            "INSERT INTO account_identifiers (account_id, type, value) VALUES (?, 'license2', ?)",
                                            new object[] { other, License },
                                            new Action<dynamic>(unused2 => One()));
                                    }));
                            }));
                    }));
            }

            void One()
            {
                Resume(License, ResumeIp, () => true, new Action<AccountResult>(result =>
                {
                    if (result == null || !result.Ok || result.Code != "OK" || result.AccountId != _account
                        || result.Name != NameA || result.AdminLevel != 2)
                    {
                        Fail("one");
                        return;
                    }

                    MySql.Single(
                        "SELECT last_ip FROM accounts WHERE id = ?",
                        new object[] { _account },
                        new Action<dynamic>(row =>
                        {
                            string ip = row == null ? null : MySql.Text(row, "last_ip");
                            if (ip != ResumeIp)
                            {
                                Fail("last_ip");
                                return;
                            }

                            MySql.Update(
                                "DELETE FROM settings WHERE skey = ?",
                                new object[] { AutoLoginKey },
                                new Action<dynamic>(unused =>
                                {
                                    Resume(License, ResumeIp, () => true, new Action<AccountResult>(missing =>
                                    {
                                        if (missing == null || !missing.Ok || missing.AccountId != _account)
                                        {
                                            Fail("default_on");
                                            return;
                                        }

                                        Inactive();
                                    }));
                                }));
                        }));
                }));
            }

            void Inactive()
            {
                MySql.Update(
                    "UPDATE accounts SET active = 0 WHERE id = ?",
                    new object[] { _account },
                    new Action<dynamic>(unused =>
                    {
                        Resume(License, ResumeIp, () => true, new Action<AccountResult>(result =>
                        {
                            if (result == null || result.Ok || result.Code != "INACTIVE" || result.AccountId != _account)
                            {
                                Fail("inactive");
                                return;
                            }

                            MySql.Update(
                                "UPDATE accounts SET active = 1 WHERE id = ?",
                                new object[] { _account },
                                new Action<dynamic>(unused2 => Off()));
                        }));
                    }));
            }

            void Off()
            {
                SetAuto("0", () =>
                {
                    Resume(License, ResumeIp, () => true, new Action<AccountResult>(result =>
                    {
                        if (result == null || result.Ok || result.Code != "OFF")
                        {
                            Fail("off");
                            return;
                        }

                        SetAuto("1", () => None());
                    }));
                });
            }

            void None()
            {
                MySql.Update(
                    "DELETE FROM account_identifiers WHERE account_id = ? AND type = 'license'",
                    new object[] { _account },
                    new Action<dynamic>(unused =>
                    {
                        Resume(License, ResumeIp, () => true, new Action<AccountResult>(fresh =>
                        {
                            if (fresh == null || fresh.Ok || fresh.Code != "NONE")
                            {
                                Fail("none");
                                return;
                            }

                            MySql.Update(
                                "UPDATE settings SET svalue = '1' WHERE skey = 'player.auto_ip_login'",
                                new object[0],
                                new Action<dynamic>(unused2 =>
                                {
                                    Resume(License, ResumeIp, () => true, new Action<AccountResult>(ignored =>
                                    {
                                        if (ignored == null || ignored.Ok || ignored.Code != "NONE")
                                        {
                                            Fail("ip");
                                            return;
                                        }

                                        Cleanup(true);
                                    }));
                                }));
                        }));
                    }));
            }

            void SetAuto(string value, Action next)
            {
                MySql.Update(
                    "INSERT INTO settings (skey, svalue) VALUES (?, ?) ON DUPLICATE KEY UPDATE svalue = VALUES(svalue)",
                    new object[] { AutoLoginKey, value },
                    new Action<dynamic>(unused => next()));
            }

            void Wipe(Action next)
            {
                MySql.Update(
                    "DELETE FROM accounts WHERE name IN (?, ?)",
                    new object[] { NameA, NameB },
                    new Action<dynamic>(unused => next()));
            }

            void Fail(string step)
            {
                Cleanup(false, step);
            }

            void Cleanup(bool ok, string step = null)
            {
                Action finish = () =>
                {
                    MySql.Scalar(
                        "SELECT COUNT(*) FROM accounts WHERE name IN (?, ?)",
                        new object[] { NameA, NameB },
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

                            Finish("ok one=1 admin=2 last_ip=1 default_on=1 inactive=1 off=1 none=1 ip_ignored=1 cleaned=1");
                        }));
                };

                Wipe(() => RestoreAuto(() => RestoreIp(finish)));
            }

            void RestoreAuto(Action next)
            {
                if (_autoMissing)
                {
                    MySql.Update(
                        "DELETE FROM settings WHERE skey = ?",
                        new object[] { AutoLoginKey },
                        new Action<dynamic>(unused => next()));
                    return;
                }

                MySql.Update(
                    "UPDATE settings SET svalue = ? WHERE skey = ?",
                    new object[] { _auto ?? "1", AutoLoginKey },
                    new Action<dynamic>(unused => next()));
            }

            void RestoreIp(Action next)
            {
                if (_ipLogin == null)
                {
                    next();
                    return;
                }

                MySql.Update(
                    "UPDATE settings SET svalue = ? WHERE skey = 'player.auto_ip_login'",
                    new object[] { _ipLogin },
                    new Action<dynamic>(unused => next()));
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
