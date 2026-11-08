using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Server
{
    // F023. The ten codes are Lang.Languages in data. English is the key. A missing accounts.lang is offered once.
    static class Language
    {
        public static string Step = "";

        public static readonly string[] Codes =
        {
            "en", "es", "fr", "pt", "ptbr", "ru", "ro", "id", "bhs", "cs"
        };

        static readonly string[] Names =
        {
            "English",
            "Español",
            "Français",
            "Português (Portugal)",
            "Português (Brasil)",
            "Русский",
            "Română",
            "Bahasa Indonesia",
            "Bosanski / Hrvatski / Srpski",
            "Čeština"
        };

        static readonly HashSet<string> KnownCodes = new HashSet<string>(Codes);
        static readonly string CardJson = BuildCard();

        static Func<string, string, string> Translate = (language, english) => english;

        public static string List
        {
            get { return string.Join(", ", Codes); }
        }

        public static void Bind(Func<string, string, string> translate)
        {
            if (translate != null)
                Translate = translate;
        }

        public static bool Known(string code)
        {
            return code != null && KnownCodes.Contains(code);
        }

        public static string Normalize(string code)
        {
            if (string.IsNullOrEmpty(code))
                return "";
            code = code.Trim().ToLowerInvariant();
            return Known(code) ? code : "";
        }

        public static bool Missing(string stored)
        {
            return stored == null || stored.Trim().Length == 0;
        }

        // Offer only when the account has never stored one and this connection has not picked one.
        public static bool NeedsOffer(string stored, string chosen)
        {
            return Missing(stored) && string.IsNullOrEmpty(chosen);
        }

        public static string Card()
        {
            return CardJson;
        }

        public static string Sentence(string language, string english)
        {
            if (string.IsNullOrEmpty(english))
                return "";
            try
            {
                string value = Translate(string.IsNullOrEmpty(language) ? "en" : language, english);
                return string.IsNullOrEmpty(value) ? english : value;
            }
            catch (Exception)
            {
                return english;
            }
        }

        public static string Format(string language, string english, params string[] args)
        {
            string template = Sentence(language, english);
            if (args == null || args.Length == 0)
                return template;

            var sb = new StringBuilder();
            int ai = 0;
            for (int i = 0; i < template.Length; i++)
            {
                if (template[i] != '%' || i + 1 >= template.Length)
                {
                    sb.Append(template[i]);
                    continue;
                }

                char next = template[i + 1];
                if (next == '%')
                {
                    sb.Append('%');
                    i++;
                    continue;
                }

                if (next == 's' && ai < args.Length)
                {
                    sb.Append(args[ai++]);
                    i++;
                    continue;
                }

                sb.Append(template[i]);
            }

            return sb.ToString();
        }

        // overwrite is /lang. The login offer writes only while lang is still empty.
        public static void Save(int accountId, string code, bool overwrite, Action<bool> done)
        {
            if (accountId <= 0 || !Known(code))
            {
                if (done != null)
                    done(false);
                return;
            }

            string sql = overwrite
                ? "UPDATE accounts SET lang = ? WHERE id = ?"
                : "UPDATE accounts SET lang = ? WHERE id = ? AND (lang IS NULL OR lang = '')";
            MySql.Update(sql, new object[] { code, accountId }, new Action<dynamic>(unused =>
            {
                if (done != null)
                    done(true);
            }));
        }

        public static void Probe(Action<string> done)
        {
            new ProbeRun(done).Start();
        }

        static string BuildCard()
        {
            var body = new StringBuilder();
            body.Append("{\"type\":\"AdaptiveCard\",\"version\":\"1.3\",\"body\":[");
            body.Append("{\"type\":\"TextBlock\",\"text\":\"Language\",\"weight\":\"Bolder\",\"size\":\"Large\"},");
            body.Append("{\"type\":\"TextBlock\",\"wrap\":true,\"text\":\"Choose your language / Escolhe o teu idioma\"},");
            body.Append("{\"type\":\"Input.ChoiceSet\",\"id\":\"language\",\"style\":\"expanded\",\"value\":\"en\",\"choices\":[");
            for (int i = 0; i < Codes.Length; i++)
            {
                if (i > 0)
                    body.Append(',');
                body.Append("{\"title\":").Append(Json(Names[i]));
                body.Append(",\"value\":").Append(Json(Codes[i])).Append('}');
            }

            body.Append("]}],\"actions\":[");
            body.Append("{\"type\":\"Action.Submit\",\"title\":\"Continue\",\"data\":{\"action\":\"language\"}}");
            body.Append("]}");
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
                else
                    escaped.Append(c);
            }

            escaped.Append('"');
            return escaped.ToString();
        }

        sealed class ProbeRun
        {
            const string DeleteSql = "DELETE FROM accounts WHERE name IN ('f023a', 'f023b')";
            const string CountSql = "SELECT COUNT(*) FROM accounts WHERE name IN ('f023a', 'f023b')";
            readonly Action<string> _done;
            int _a;
            int _b;
            int _offer = 1;
            int _kept = 1;
            int _chosen = 1;
            int _blank = 1;
            int _codes = 1;
            int _card = 1;
            int _reject;
            int _set;
            int _once;
            int _keep;
            int _command;
            int _text = 1;
            int _clean;

            public ProbeRun(Action<string> done)
            {
                _done = done;
            }

            public void Start()
            {
                Step = "rules";
                if (!NeedsOffer(null, null) || NeedsOffer("fr", null) || NeedsOffer(null, "es") || !NeedsOffer("  ", null))
                    _offer = 0;
                if (NeedsOffer("fr", null))
                    _kept = 0;
                if (NeedsOffer(null, "es"))
                    _chosen = 0;
                if (!Missing(null) || !Missing("") || !Missing("  ") || Missing("en"))
                    _blank = 0;
                if (Codes.Length != 10 || !Known("ptbr") || !Known("bhs") || Known("EN") || Known("en-us") || Normalize(" PTBR ") != "ptbr")
                    _codes = 0;
                string card = Card();
                if (card.IndexOf("Español", StringComparison.Ordinal) < 0 || card.IndexOf("\"ptbr\"", StringComparison.Ordinal) < 0
                    || card.IndexOf("\"action\":\"language\"", StringComparison.Ordinal) < 0)
                    _card = 0;
                if (Format("en", "Language set to %s.", "es") != "Language set to es.")
                    _text = 0;
                if (Format("en", "Usage: /lang <%s> (now %s)", List, "fr").IndexOf("ptbr", StringComparison.Ordinal) < 0)
                    _text = 0;

                Step = "wipe";
                MySql.Update(DeleteSql, new object[0], new Action<dynamic>(unused => InsertA()));
            }

            void InsertA()
            {
                Step = "insert";
                MySql.Insert(
                    "INSERT INTO accounts (name, password_hash) VALUES ('f023a', ?)",
                    new object[] { new string('x', 60) },
                    new Action<dynamic>(id =>
                    {
                        _a = ToId(id);
                        if (_a <= 0)
                        {
                            Fail("account");
                            return;
                        }

                        InsertB();
                    }));
            }

            void InsertB()
            {
                MySql.Insert(
                    "INSERT INTO accounts (name, password_hash, lang) VALUES ('f023b', ?, 'fr')",
                    new object[] { new string('x', 60) },
                    new Action<dynamic>(id =>
                    {
                        _b = ToId(id);
                        if (_b <= 0)
                        {
                            Fail("accountb");
                            return;
                        }

                        Reject();
                    }));
            }

            void Reject()
            {
                Step = "reject";
                Save(_a, "nope", true, ok =>
                {
                    _reject = ok ? 0 : 1;
                    Read(_a, stored =>
                    {
                        if (stored != null)
                            _reject = 0;
                        SetOnce();
                    });
                });
            }

            void SetOnce()
            {
                Step = "set";
                Save(_a, "es", false, ok =>
                {
                    if (!ok)
                    {
                        Fail("set");
                        return;
                    }

                    Read(_a, stored =>
                    {
                        _set = stored == "es" ? 1 : 0;
                        Save(_a, "ru", false, again =>
                        {
                            Read(_a, after =>
                            {
                                _once = after == "es" ? 1 : 0;
                                Keep();
                            });
                        });
                    });
                });
            }

            void Keep()
            {
                Step = "keep";
                Save(_b, "en", false, ok =>
                {
                    Read(_b, stored =>
                    {
                        _keep = stored == "fr" ? 1 : 0;
                        Save(_b, "id", true, overwritten =>
                        {
                            Read(_b, after =>
                            {
                                _command = after == "id" ? 1 : 0;
                                Clean();
                            });
                        });
                    });
                });
            }

            void Clean()
            {
                Step = "clean";
                MySql.Update(DeleteSql, new object[0], new Action<dynamic>(unused =>
                {
                    MySql.Scalar(CountSql, new object[0], new Action<dynamic>(count =>
                    {
                        _clean = ToId(count) == 0 ? 1 : 0;
                        Finish();
                    }));
                }));
            }

            void Read(int accountId, Action<string> done)
            {
                MySql.Scalar("SELECT lang FROM accounts WHERE id = ?", new object[] { accountId }, new Action<dynamic>(value =>
                {
                    object raw = value;
                    if (raw == null || raw is DBNull)
                        done(null);
                    else
                        done(Convert.ToString(raw, CultureInfo.InvariantCulture));
                }));
            }

            void Fail(string step)
            {
                Step = step;
                MySql.Update(DeleteSql, new object[0], new Action<dynamic>(unused =>
                {
                    if (_done != null)
                        _done("fail step=" + step);
                }));
            }

            void Finish()
            {
                Step = "done";
                if (_done == null)
                    return;
                _done("ok offer=" + _offer
                    + " kept=" + _kept
                    + " chosen=" + _chosen
                    + " blank=" + _blank
                    + " codes=" + _codes
                    + " card=" + _card
                    + " text=" + _text
                    + " reject=" + _reject
                    + " set=" + _set
                    + " once=" + _once
                    + " keep=" + _keep
                    + " command=" + _command
                    + " cleaned=" + _clean);
            }

            static int ToId(object value)
            {
                if (value == null || value is DBNull)
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
