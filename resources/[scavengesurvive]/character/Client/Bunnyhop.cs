using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Client
{
    public class Bunnyhop : BaseScript
    {
        const int MaxJumps = 10;
        int jumps;
        int warnUntil;

        public Bunnyhop()
        {
            Tick += Watch;
        }

        async Task Watch()
        {
            int ped = API.PlayerPedId();
            bool jumping = API.IsPedOnFoot(ped) && !API.IsPedSwimming(ped)
                && (API.IsPedRunning(ped) || API.IsPedSprinting(ped))
                && !API.IsPedClimbing(ped) && API.IsPedJumping(ped) && !API.IsPedRagdoll(ped);

            if (jumping)
            {
                jumps++;
                if (jumps >= MaxJumps)
                {
                    API.SetPedToRagdoll(ped, 5000, 1400, 2, false, false, false);
                    jumps = 0;
                    warnUntil = API.GetGameTimer() + 3000;
                }
                await Delay(1);
            }
            else
            {
                jumps = 0;
                await Delay(500);
            }

            if (API.GetGameTimer() < warnUntil)
                Text.Draw("Bunnyhopping is disabled", 0.5f, 0.5f, 255, 0, 0, 255, 1f, true);
        }
    }

    static class Text
    {
        public static void Draw(string text, float x, float y, int r, int g, int b, int a, float scale, bool center)
        {
            API.SetTextFont(0);
            API.SetTextScale(scale, scale);
            API.SetTextColour(r, g, b, a);
            if (center) API.SetTextCentre(true);
            API.BeginTextCommandDisplayText("STRING");
            API.AddTextComponentSubstringPlayerName(text);
            API.EndTextCommandDisplayText(x, y);
        }
    }
}
