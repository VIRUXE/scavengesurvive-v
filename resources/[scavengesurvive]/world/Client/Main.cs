using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Client
{
    public class Main : BaseScript
    {
        static readonly string[] Scenarios = new string[]
        {
            "WORLD_VEHICLE_ATTRACTOR",
            "WORLD_VEHICLE_AMBULANCE",
            "WORLD_VEHICLE_BICYCLE_BMX",
            "WORLD_VEHICLE_BICYCLE_BMX_BALLAS",
            "WORLD_VEHICLE_BICYCLE_BMX_FAMILY",
            "WORLD_VEHICLE_BICYCLE_BMX_HARMONY",
            "WORLD_VEHICLE_BICYCLE_BMX_VAGOS",
            "WORLD_VEHICLE_BICYCLE_MOUNTAIN",
            "WORLD_VEHICLE_BICYCLE_ROAD",
            "WORLD_VEHICLE_BIKE_OFF_ROAD_RACE",
            "WORLD_VEHICLE_BIKER",
            "WORLD_VEHICLE_BOAT_IDLE",
            "WORLD_VEHICLE_BOAT_IDLE_ALAMO",
            "WORLD_VEHICLE_BOAT_IDLE_MARQUIS",
            "WORLD_VEHICLE_BROKEN_DOWN",
            "WORLD_VEHICLE_BUSINESSMEN",
            "WORLD_VEHICLE_HELI_LIFEGUARD",
            "WORLD_VEHICLE_CLUCKIN_BELL_TRAILER",
            "WORLD_VEHICLE_CONSTRUCTION_SOLO",
            "WORLD_VEHICLE_CONSTRUCTION_PASSENGERS",
            "WORLD_VEHICLE_DRIVE_PASSENGERS",
            "WORLD_VEHICLE_DRIVE_PASSENGERS_LIMITED",
            "WORLD_VEHICLE_DRIVE_SOLO",
            "WORLD_VEHICLE_FIRE_TRUCK",
            "WORLD_VEHICLE_EMPTY",
            "WORLD_VEHICLE_MARIACHI",
            "WORLD_VEHICLE_MECHANIC",
            "WORLD_VEHICLE_MILITARY_PLANES_BIG",
            "WORLD_VEHICLE_MILITARY_PLANES_SMALL",
            "WORLD_VEHICLE_PARK_PARALLEL",
            "WORLD_VEHICLE_PARK_PERPENDICULAR_NOSE_IN",
            "WORLD_VEHICLE_PASSENGER_EXIT",
            "WORLD_VEHICLE_POLICE_BIKE",
            "WORLD_VEHICLE_POLICE_CAR",
            "WORLD_VEHICLE_POLICE",
            "WORLD_VEHICLE_POLICE_NEXT_TO_CAR",
            "WORLD_VEHICLE_QUARRY",
            "WORLD_VEHICLE_SALTON",
            "WORLD_VEHICLE_SALTON_DIRT_BIKE",
            "WORLD_VEHICLE_SECURITY_CAR",
            "WORLD_VEHICLE_STREETRACE",
            "WORLD_VEHICLE_TOURBUS",
            "WORLD_VEHICLE_TOURIST",
            "WORLD_VEHICLE_TANDL",
            "WORLD_VEHICLE_TRACTOR",
            "WORLD_VEHICLE_TRACTOR_BEACH",
            "WORLD_VEHICLE_TRUCK_LOGS",
            "WORLD_VEHICLE_TRUCKS_TRAILERS",
            "WORLD_VEHICLE_DISTANT_EMPTY_GROUND"
        };

        public Main()
        {
            API.SetPedPopulationBudget(0);
            API.SetVehiclePopulationBudget(0);
            API.SetGarbageTrucks(false);
            API.SetRandomBoats(false);
            API.SetRandomBoatsInMp(false);
            API.SetCreateRandomCops(false);
            API.SetCreateRandomCopsNotOnScenarios(false);
            API.SetCreateRandomCopsOnScenarios(false);
            API.SetRandomEventFlag(0);
            API.DisableVehicleDistantlights(true);

            for (int i = 0; i < Scenarios.Length; i++)
                API.SetScenarioTypeEnabled(Scenarios[i], false);

            Tick += Belt;
            Debug.WriteLine("world client started.");
        }

        static async Task Belt()
        {
            API.SetPedDensityMultiplierThisFrame(0f);
            API.SetScenarioPedDensityMultiplierThisFrame(0f, 0f);
            API.SetParkedVehicleDensityMultiplierThisFrame(0f);
            API.SetVehicleDensityMultiplierThisFrame(0f);
            API.SetRandomVehicleDensityMultiplierThisFrame(0f);
            await BaseScript.Delay(0);
        }
    }
}
