using System;
using System.Globalization;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Shared;

namespace Client
{
    public class Spawner : BaseScript
    {
        bool spawning;
        Shared.Spawn last;

        public Spawner()
        {
            EventHandlers["player:spawn"] += new Action<string>(OnSpawn);
            EventHandlers["player:respawn"] += new Action(SpawnRandom);
            Tick += Ready;
        }

        async Task Ready()
        {
            Tick -= Ready;
            TriggerServerEvent("player:loaded");
            await Delay(0);
        }

        void OnSpawn(string position)
        {
            float x, y, z;
            if (TryRead(position, out x, out y, out z))
            {
                Go(x, y, z + 0f, 0f, false, "Spawned at last known location");
                return;
            }
            SpawnRandom();
        }

        void SpawnRandom()
        {
            Shared.Spawn picked;
            do
            {
                picked = Spawns.All[new Random().Next(Spawns.All.Length)];
            }
            while (last != null && picked == last);
            last = picked;
            Go(picked.X, picked.Y, picked.Z + 0.5f, picked.Heading, picked.GiveParachute, "Spawned at " + picked.Description);
        }

        async void Go(float x, float y, float z, float heading, bool parachute, string note)
        {
            if (spawning) return;
            spawning = true;

            API.DoScreenFadeOut(500);
            while (!API.IsScreenFadedOut()) await Delay(50);

            int ped = API.PlayerPedId();
            API.RequestCollisionAtCoord(x, y, z);
            API.SetEntityCoords(ped, x, y, z, true, false, false, false);
            if (heading != 0f) API.SetEntityHeading(ped, heading);

            int started = API.GetGameTimer();
            while (!API.HasCollisionLoadedAroundEntity(ped) && API.GetGameTimer() - started < 5000)
                await Delay(0);

            API.NetworkResurrectLocalPlayer(x, y, z, heading, false, false);
            ped = API.PlayerPedId();
            API.ClearPedTasksImmediately(ped);
            API.RemoveAllPedWeapons(ped, false);
            API.ClearPlayerWantedLevel(API.PlayerId());

            if (parachute)
                API.GiveWeaponToPed(ped, (uint)API.GetHashKey("gadget_parachute"), 1, false, false);

            API.ShutdownLoadingScreen();
            if (API.IsScreenFadedOut())
            {
                API.DoScreenFadeIn(5000);
                await Delay(5000);
            }

            if (API.IsEntityPositionFrozen(ped)) API.FreezeEntityPosition(ped, false);
            spawning = false;
            Debug.WriteLine(note);
            TriggerServerEvent("player:spawned");
        }

        static bool TryRead(string json, out float x, out float y, out float z)
        {
            x = y = z = 0f;
            if (string.IsNullOrEmpty(json) || json == "null") return false;
            return Read(json, "x", out x) && Read(json, "y", out y) && Read(json, "z", out z);
        }

        static bool Read(string json, string key, out float value)
        {
            value = 0f;
            int at = json.IndexOf("\"" + key + "\"");
            if (at < 0) return false;
            int colon = json.IndexOf(':', at);
            if (colon < 0) return false;
            int end = colon + 1;
            while (end < json.Length && json[end] != ',' && json[end] != '}') end++;
            return float.TryParse(json.Substring(colon + 1, end - colon - 1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
