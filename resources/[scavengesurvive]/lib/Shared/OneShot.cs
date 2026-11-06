using System.Collections.Generic;
using System.Security.Cryptography;

namespace Shared
{
    /// <summary>
    /// One-shot token. Issue returns it once. Consume deletes it, and rejects it after the ttl.
    /// MTA <c>net.issueToken</c> / <c>net.consumeToken</c> used <c>math.random</c>. This uses 16 random bytes.
    /// </summary>
    public sealed class OneShot
    {
        private readonly Dictionary<string, long> _expires = new Dictionary<string, long>();

        public string Issue(string subject, string purpose, int ttlMs, long nowMs)
        {
            var bytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }

            var token = Hex(bytes) + "-" + nowMs.ToString();
            _expires[Key(subject, purpose, token)] = nowMs + (ttlMs <= 0 ? 30000 : ttlMs);
            return token;
        }

        public bool Consume(string subject, string purpose, string token, long nowMs)
        {
            var key = Key(subject, purpose, token);
            long expires;
            if (!_expires.TryGetValue(key, out expires))
            {
                return false;
            }

            _expires.Remove(key);
            return nowMs <= expires;
        }

        private static string Key(string subject, string purpose, string token)
        {
            return (subject ?? "") + "|" + (purpose ?? "") + "|" + (token ?? "");
        }

        private static string Hex(byte[] bytes)
        {
            var chars = new char[bytes.Length * 2];
            const string alphabet = "0123456789abcdef";
            for (var i = 0; i < bytes.Length; i++)
            {
                chars[i * 2] = alphabet[bytes[i] >> 4];
                chars[i * 2 + 1] = alphabet[bytes[i] & 0x0f];
            }

            return new string(chars);
        }
    }
}
