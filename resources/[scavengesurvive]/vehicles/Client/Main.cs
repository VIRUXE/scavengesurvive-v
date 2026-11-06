using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Client
{
    public class Main : BaseScript
    {
        int ped;

        public Main()
        {
            Tick += Helmet;
            Debug.WriteLine("vehicles client started.");
        }

        async Task Helmet()
        {
            int current = API.PlayerPedId();
            if (current != 0 && current != ped)
            {
                ped = current;
                API.SetPedConfigFlag(current, 35, false);
            }
            await Delay(500);
        }
    }
}
