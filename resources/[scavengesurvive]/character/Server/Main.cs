using System;
using CitizenFX.Core;

namespace Server
{
    public class Main : ServerScript
    {
        public Main()
        {
            EventHandlers["playerAuthenticated"] += new Action<string, string>(OnAuthenticated);
            Debug.WriteLine("character server started.");
        }

        void OnAuthenticated(string source, string lastPosition)
        {
            TriggerClientEvent("player:spawn", source, lastPosition ?? "");
        }
    }
}
