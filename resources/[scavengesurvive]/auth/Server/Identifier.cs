using System.Collections.Generic;

namespace Server
{
    static class Identifier
    {
        static readonly HashSet<string> Types = new HashSet<string>
        {
            "steam", "discord", "xbl", "live", "license", "license2", "fivem", "ip"
        };

        public static Dictionary<string, string> Read(string source)
        {
            var found = new Dictionary<string, string>();
            int count = CitizenFX.Core.Native.API.GetNumPlayerIdentifiers(source);
            for (int i = 0; i < count; i++)
            {
                string raw = CitizenFX.Core.Native.API.GetPlayerIdentifier(source, i);
                if (string.IsNullOrEmpty(raw)) continue;

                int sep = raw.IndexOf(':');
                if (sep <= 0 || sep == raw.Length - 1) continue;

                string type = raw.Substring(0, sep);
                if (!Types.Contains(type) || found.ContainsKey(type)) continue;

                found[type] = raw.Substring(sep + 1);
            }
            return found;
        }

        public static string[] ReadTokens(string source)
        {
            int count = CitizenFX.Core.Native.API.GetNumPlayerTokens(source);
            var tokens = new string[count];
            for (int i = 0; i < count; i++)
                tokens[i] = CitizenFX.Core.Native.API.GetPlayerToken(source, i);
            return tokens;
        }
    }
}
