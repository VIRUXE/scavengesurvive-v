using CitizenFX.Core.Native;

namespace Client
{
    public class Presence : CitizenFX.Core.BaseScript
    {
        public Presence()
        {
            string app = API.GetConvar("discord_app_id", "");
            if (app.Length > 0) API.SetDiscordAppId(app);
            API.SetDiscordRichPresenceAction(0, "Discord", API.GetConvar("discord_invite_url", "https://discord.gg/9FwKJMykdK"));
        }
    }
}
