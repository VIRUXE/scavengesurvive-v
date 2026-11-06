using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Server
{
    public class Main : ServerScript
    {
        const int Ped = 1;
        const int Vehicle = 2;

        public Main()
        {
            API.SetRoutingBucketPopulationEnabled(Shared.Buckets.World, false);
            API.SetRoutingBucketPopulationEnabled(Shared.Buckets.Tutorial, false);
            EventHandlers["entityCreating"] += new Action<int>(OnEntityCreating);
            Debug.WriteLine("world server started. population off on buckets " + Shared.Buckets.World + " and " + Shared.Buckets.Tutorial + ".");
        }

        // Types 1 through 5 are RANDOM_PERMANENT through RANDOM_AMBIENT.
        // PERMANENT (6) and MISSION (7) stay, so a script vehicle is not cancelled.
        void OnEntityCreating(int entity)
        {
            int kind = API.GetEntityType(entity);
            if (kind != Ped && kind != Vehicle) return;

            int population = API.GetEntityPopulationType(entity);
            if (population >= 1 && population <= 5)
                API.CancelEvent();
        }
    }
}
