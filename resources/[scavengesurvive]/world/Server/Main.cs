using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Server
{
    public class Main : ServerScript
    {
        public Main()
        {
            API.SetRoutingBucketPopulationEnabled(0, false);
            EventHandlers["entityCreating"] += new Action<int>(OnEntityCreating);
            Debug.WriteLine("world server started.");
        }

        void OnEntityCreating(int entity)
        {
            int kind = API.GetEntityType(entity);
            if (kind != 1 && kind != 2) return;

            int population = API.GetEntityPopulationType(entity);
            if (population >= 1 && population <= 5)
                API.CancelEvent();
        }
    }
}
