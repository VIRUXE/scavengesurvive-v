using System.Collections.Generic;
using CitizenFX.Core.Native;

namespace Server
{
    static class Identifier
    {
        public static readonly string[] Types = new string[]
        {
            "license", "license2", "discord", "fivem", "steam", "xbl", "live", "ip"
        };

        static readonly HashSet<string> TypeSet = new HashSet<string>(Types);

        public static Dictionary<string, string> Read(string source, out string[] tokens)
        {
            int count = API.GetNumPlayerIdentifiers(source);
            var raw = new string[count];
            for (int i = 0; i < count; i++)
                raw[i] = API.GetPlayerIdentifier(source, i);

            var found = Take(raw);

            if (!found.ContainsKey("ip"))
            {
                string ip = Host(API.GetPlayerEndpoint(source));
                if (ip != null)
                    found["ip"] = ip;
            }

            int tokenCount = API.GetNumPlayerTokens(source);
            var tokenRaw = new string[tokenCount];
            for (int i = 0; i < tokenCount; i++)
                tokenRaw[i] = API.GetPlayerToken(source, i);
            tokens = TakeTokens(tokenRaw);
            return found;
        }

        public static Dictionary<string, string> Take(string[] raw)
        {
            var found = new Dictionary<string, string>();
            if (raw != null)
            {
                for (int i = 0; i < raw.Length; i++)
                    Keep(found, raw[i]);
            }

            return found;
        }

        public static bool HasLicense(Dictionary<string, string> identifiers)
        {
            string license;
            return identifiers != null
                && identifiers.TryGetValue("license", out license)
                && !string.IsNullOrEmpty(license);
        }

        public static string Summary(Dictionary<string, string> identifiers, string[] tokens)
        {
            var names = new List<string>();
            for (int i = 0; i < Types.Length; i++)
            {
                string value;
                if (identifiers != null && identifiers.TryGetValue(Types[i], out value) && !string.IsNullOrEmpty(value))
                    names.Add(Types[i]);
            }

            int tokenCount = tokens == null ? 0 : tokens.Length;
            return "types=" + string.Join(",", names.ToArray())
                + " token_count=" + tokenCount.ToString()
                + " license=" + (HasLicense(identifiers) ? "yes" : "no");
        }

        static void Keep(Dictionary<string, string> found, string raw)
        {
            if (string.IsNullOrEmpty(raw)) return;

            int sep = raw.IndexOf(':');
            if (sep <= 0 || sep >= raw.Length - 1) return;

            string type = raw.Substring(0, sep);
            if (!TypeSet.Contains(type) || found.ContainsKey(type)) return;

            string value = raw.Substring(sep + 1);
            if (value.Length == 0 || value.Length > 255) return;
            found[type] = value;
        }

        public static string[] TakeTokens(string[] raw)
        {
            var tokens = new List<string>();
            var seen = new HashSet<string>();
            if (raw == null) return new string[0];

            for (int i = 0; i < raw.Length; i++)
            {
                string token = raw[i];
                if (string.IsNullOrEmpty(token) || token.Length > 255 || !seen.Add(token)) continue;
                tokens.Add(token);
            }

            return tokens.ToArray();
        }

        static string Host(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint)) return null;

            string host = endpoint.Trim();
            if (host.Length == 0) return null;

            if (host[0] == '[')
            {
                int end = host.IndexOf(']');
                if (end <= 1) return null;
                host = host.Substring(1, end - 1);
            }
            else
            {
                int colon = host.LastIndexOf(':');
                if (colon > 0 && host.IndexOf(':') == colon)
                    host = host.Substring(0, colon);
            }

            if (host.Length == 0 || host.Length > 255) return null;
            return host;
        }
    }
}
