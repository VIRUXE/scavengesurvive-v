using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Client
{
    public class Main : BaseScript
    {
        public Main()
        {
            EventHandlers["admin:teleportToMarker"] += new Action(TeleportToMarker);
            EventHandlers["staff:measure"] += new Action<int, uint>(Measure);
            Debug.WriteLine("staff client started.");
        }

        async void Measure(int id, uint hash)
        {
            API.RequestModel(hash);
            int started = API.GetGameTimer();
            while (!API.HasModelLoaded(hash) && API.GetGameTimer() - started < 3000)
                await Delay(0);

            Vector3 min = new Vector3();
            Vector3 max = new Vector3();
            API.GetModelDimensions(hash, ref min, ref max);
            API.SetModelAsNoLongerNeeded(hash);
            Vector3 size = max - min;
            int ped = API.PlayerPedId();
            Vector3 forward = API.GetEntityForwardVector(ped);
            TriggerServerEvent("staff:measured", id, size.X, size.Y, size.Z, forward.X, forward.Y, forward.Z);
        }

        async void TeleportToMarker()
        {
            if (!API.IsWaypointActive())
            {
                Feed("No waypoint found, please set a waypoint first.");
                return;
            }

            int waypoint = API.GetFirstBlipInfoId(API.GetWaypointBlipEnumId());
            Vector3 coords = API.GetBlipInfoIdCoord(waypoint);
            API.DoScreenFadeOut(500);
            while (!API.IsScreenFadedOut()) await Delay(0);
            await Place(coords.X, coords.Y, 0f);
            API.DoScreenFadeIn(500);
        }

        async Task Place(float x, float y, float z)
        {
            int ped = API.PlayerPedId();
            int veh = API.GetVehiclePedIsIn(ped, false);
            API.SetPedCoordsKeepVehicle(ped, x, y, 100f);
            int frozen = veh > 0 ? veh : ped;
            API.FreezeEntityPosition(frozen, true);

            while (API.IsEntityWaitingForWorldCollision(ped))
                await Delay(100);

            if (z == 0f)
            {
                float ground = 0f;
                for (int i = 0; i < 5 && ground == 0f; i++)
                {
                    float found = Ground(x, y);
                    if (found == 0f) await Delay(500);
                    else ground = found;
                }
                if (ground != 0f) z = ground;
            }

            ped = API.PlayerPedId();
            API.SetPedCoordsKeepVehicle(ped, x, y, z);
            if (veh > 0)
            {
                veh = API.GetVehiclePedIsIn(ped, false);
                API.SetEntityAlpha(veh, 125, 1);
                API.SetEntityCoords(veh, x, y, z + 0.5f, false, false, false, false);
                API.SetPedIntoVehicle(ped, veh, -1);
                API.SetVehicleOnGroundProperly(veh);
                API.SetEntityCollision(veh, true, true);
                API.FreezeEntityPosition(veh, false);
                await Delay(2000);
                API.ResetEntityAlpha(veh);
                API.SetVehicleCanBreak(veh, true);
                API.SetVehicleWheelsCanBreak(veh, true);
            }
            else
            {
                API.FreezeEntityPosition(ped, false);
            }
            API.SetGameplayCamRelativeHeading(0f);
        }

        static float Ground(float x, float y)
        {
            float z = 1500f;
            bool found = true;
            while (found && z > 0f)
            {
                float ground = 0f;
                Vector3 normal = new Vector3();
                found = API.GetGroundZAndNormalFor_3dCoord(x, y, z - 1f, ref ground, ref normal);
                if (found) z = ground;
            }
            if (z == 1500f) return 0f;
            return z;
        }

        static void Feed(string message)
        {
            API.BeginTextCommandThefeedPost("STRING");
            API.AddTextComponentSubstringPlayerName(message);
            API.EndTextCommandThefeedPostTicker(false, true);
        }
    }
}
