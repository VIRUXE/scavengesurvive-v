using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Client
{
    public class Main : BaseScript
    {
        bool showing;
        int endsAt;

        public Main()
        {
            EventHandlers["whitelistCountdownChanged"] += new Action<int>(OnCountdown);
            Tick += Draw;
            Debug.WriteLine("auth client started.");
        }

        void OnCountdown(int seconds)
        {
            if (seconds < 0)
            {
                showing = false;
                return;
            }

            endsAt = API.GetGameTimer() + seconds * 1000;
            showing = true;
        }

        async Task Draw()
        {
            if (!showing)
            {
                await Delay(200);
                return;
            }

            int left = (int)Math.Ceiling((endsAt - API.GetGameTimer()) / 1000.0);
            if (left < 0)
                left = 0;
            int minutes = left / 60;
            int seconds = left % 60;
            Text("Not whitelisted", 0.67f, 0.055f);
            Text(string.Format("Time remaining: {0:00}:{1:00}", minutes, seconds), 0.67f, 0.082f);
            await Task.FromResult(0);
        }

        static void Text(string text, float x, float y)
        {
            API.SetTextFont(0);
            API.SetTextScale(0.32f, 0.32f);
            API.SetTextColour(255, 255, 255, 255);
            API.SetTextCentre(true);
            API.SetTextOutline();
            API.BeginTextCommandDisplayText("STRING");
            API.AddTextComponentSubstringPlayerName(text);
            API.EndTextCommandDisplayText(x, y);
        }
    }
}
