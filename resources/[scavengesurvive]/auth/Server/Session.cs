using System.Collections.Generic;

namespace Server
{
    sealed class Session
    {
        public string Source;
        public string Name;
        public Dictionary<string, string> Identifiers;
        public string[] Tokens;
        public int AccountId;
        public string Username;
        public string Email;
        public string LastPosition;
        public bool HasAccount;
        public bool Spawned;
    }

    static class Sessions
    {
        static readonly Dictionary<string, Session> BySource = new Dictionary<string, Session>();

        public static Session Begin(string source, string name)
        {
            var session = new Session
            {
                Source = source,
                Name = name,
                Identifiers = Identifier.Read(source),
                Tokens = Identifier.ReadTokens(source)
            };
            BySource[source] = session;
            return session;
        }

        public static void Remap(string oldSource, string newSource)
        {
            Session session;
            if (!BySource.TryGetValue(oldSource, out session)) return;

            BySource.Remove(oldSource);
            session.Source = newSource;
            BySource[newSource] = session;
        }

        public static Session Get(string source)
        {
            Session session;
            BySource.TryGetValue(source, out session);
            return session;
        }

        public static void End(string source)
        {
            BySource.Remove(source);
        }
    }
}
