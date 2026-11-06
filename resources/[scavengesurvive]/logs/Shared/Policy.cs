using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Shared
{
    /// <summary>
    /// Field rules for one <c>log.event</c> line. No socket.
    /// Secret keys never pass. Identifier keys pass only on
    /// <c>session.connected</c> and <c>session.banned</c>.
    /// </summary>
    public static class LogPolicy
    {
        public static int Rank(string level)
        {
            switch (level)
            {
                case "debug":
                    return 0;
                case "warn":
                    return 2;
                case "error":
                    return 3;
                default:
                    return 1;
            }
        }

        public static string NormalizeLevel(string level)
        {
            switch (level)
            {
                case "debug":
                case "info":
                case "warn":
                case "error":
                    return level;
                default:
                    return "info";
            }
        }

        public static bool AllowsIdentifiers(string kind)
        {
            return kind == "session.connected" || kind == "session.banned";
        }

        public static bool IsPlayerKey(string key)
        {
            if (key == null)
            {
                return false;
            }

            switch (key.ToLowerInvariant())
            {
                case "player":
                case "attacker":
                case "victim":
                case "killer":
                case "to":
                case "actor":
                case "target":
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsSecretKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return true;
            }

            string name = key.ToLowerInvariant();
            switch (name)
            {
                case "password":
                case "password_hash":
                case "pass":
                case "secret":
                case "token":
                case "tokens":
                case "token_hash":
                case "bearer":
                case "keypad":
                case "keypad_code":
                case "pin":
                    return true;
            }

            return name.Contains("password")
                || name.Contains("keypad")
                || name.EndsWith("_token")
                || name.EndsWith("_secret");
        }

        public static bool IsIdentifierKey(string key)
        {
            if (key == null)
            {
                return false;
            }

            switch (key.ToLowerInvariant())
            {
                case "ip":
                case "serial":
                case "license":
                case "license2":
                case "discord":
                case "steam":
                case "fivem":
                case "xbl":
                case "live":
                case "identifier":
                case "identifiers":
                case "endpoint":
                    return true;
                default:
                    return false;
            }
        }

        public static Dictionary<string, object> Fields(string kind, IDictionary<string, object> fields)
        {
            var safe = new Dictionary<string, object>();
            if (fields == null)
            {
                return safe;
            }

            bool identifiers = AllowsIdentifiers(kind);
            foreach (var pair in fields)
            {
                if (pair.Key == null || IsSecretKey(pair.Key))
                {
                    continue;
                }

                if (!identifiers && IsIdentifierKey(pair.Key))
                {
                    continue;
                }

                object value = CopyValue(pair.Value);
                if (value == null)
                {
                    continue;
                }

                safe[pair.Key] = value;
            }

            return safe;
        }

        private static object CopyValue(object value)
        {
            if (value == null || value is string)
            {
                return Scrub(value as string);
            }

            if (IsNumber(value) || value is bool)
            {
                return value;
            }

            var list = value as IList;
            if (list == null || value is string)
            {
                return null;
            }

            var scalars = new List<object>();
            int n = list.Count < 24 ? list.Count : 24;
            for (int i = 0; i < n; i++)
            {
                object item = list[i];
                if (item is string)
                {
                    scalars.Add(Scrub((string)item));
                }
                else if (IsNumber(item) || item is bool)
                {
                    scalars.Add(item);
                }
            }

            return scalars;
        }

        private static bool IsNumber(object value)
        {
            return value is int || value is long || value is short || value is byte || value is uint
                || value is float || value is double || value is decimal;
        }

        // A hash pasted into a free-text field is still a password.
        private static string Scrub(string value)
        {
            if (value == null)
            {
                return null;
            }

            if (value.IndexOf("$2a$", StringComparison.Ordinal) >= 0
                || value.IndexOf("$2b$", StringComparison.Ordinal) >= 0
                || value.IndexOf("$2y$", StringComparison.Ordinal) >= 0)
            {
                return "[redacted]";
            }

            return value;
        }
    }
}
