using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Shared
{
    /// <summary>
    /// One flat JSON object line: level, resource, kind, then scalar fields.
    /// Field names and the 512-character cap match MTA <c>lib/modules/log.lua</c> <c>log.build</c>.
    /// Player elements are resolved by the caller. This type does not open a socket.
    /// </summary>
    public static class LogShape
    {
        public const int MaxString = 512;
        public const int MaxArray = 24;

        public static readonly string[] PlayerKeys =
        {
            "player", "attacker", "victim", "killer", "to", "actor", "target",
        };

        public static bool IsMetaKey(string key)
        {
            switch (key)
            {
                case "player":
                case "account_id":
                case "character_id":
                case "item_id":
                case "vehicle_id":
                case "attacker":
                case "victim":
                case "killer":
                case "to":
                case "actor":
                case "target":
                    return true;
                default:
                    return key != null && (key.EndsWith("_account_id") || key.EndsWith("_character_id"));
            }
        }

        public static string Build(string level, string resource, string kind, IDictionary<string, object> fields)
        {
            var parts = new StringBuilder();
            parts.Append("{\"level\":\"").Append(level ?? "info").Append('"');
            parts.Append(",\"resource\":").Append(EncodeString(resource, 64));
            parts.Append(",\"kind\":").Append(EncodeString(kind, 64));
            if (fields != null)
            {
                foreach (var pair in fields)
                {
                    if (pair.Value == null || pair.Key == null)
                    {
                        continue;
                    }

                    parts.Append(',').Append(EncodeString(pair.Key, 64)).Append(':').Append(EncodeValue(pair.Value));
                }
            }

            parts.Append('}');
            return parts.ToString();
        }

        private static string EncodeValue(object value)
        {
            var list = value as IList;
            if (list != null && !(value is string))
            {
                var parts = new StringBuilder();
                parts.Append('[');
                var n = list.Count < MaxArray ? list.Count : MaxArray;
                for (var i = 0; i < n; i++)
                {
                    if (i > 0)
                    {
                        parts.Append(',');
                    }

                    parts.Append(EncodeScalar(list[i]));
                }

                parts.Append(']');
                return parts.ToString();
            }

            return EncodeScalar(value);
        }

        private static string EncodeScalar(object value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is bool)
            {
                return (bool)value ? "true" : "false";
            }

            if (value is int || value is long || value is short || value is byte)
            {
                return Convert.ToIntegerString(value);
            }

            if (value is float || value is double || value is decimal)
            {
                var number = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (double.IsNaN(number) || double.IsInfinity(number))
                {
                    return "null";
                }

                return number.ToString("0.##", CultureInfo.InvariantCulture);
            }

            return EncodeString(System.Convert.ToString(value, CultureInfo.InvariantCulture), MaxString);
        }

        private static string EncodeString(string value, int max)
        {
            if (value == null)
            {
                value = "";
            }

            if (value.Length > max)
            {
                value = value.Substring(0, max) + "...";
            }

            var parts = new StringBuilder(value.Length + 2);
            parts.Append('"');
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                switch (c)
                {
                    case '"':
                        parts.Append("\\\"");
                        break;
                    case '\\':
                        parts.Append("\\\\");
                        break;
                    case '\n':
                        parts.Append("\\n");
                        break;
                    case '\r':
                        parts.Append("\\r");
                        break;
                    case '\t':
                        parts.Append("\\t");
                        break;
                    default:
                        if (c < 0x20)
                        {
                            parts.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            parts.Append(c);
                        }

                        break;
                }
            }

            parts.Append('"');
            return parts.ToString();
        }

        private static class Convert
        {
            public static string ToIntegerString(object value)
            {
                return System.Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
