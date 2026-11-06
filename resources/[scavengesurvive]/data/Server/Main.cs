using System;
using CitizenFX.Core;

namespace Server
{
    public class Main : ServerScript
    {
        public Main()
        {
            Exports.Add("findVehicles", new Func<string, string[]>(FindVehicles));
            Exports.Add("findWeapons", new Func<string, string[]>(FindWeapons));
            Exports.Add("weaponName", new Func<string, string>(hash => WeaponCatalogue.NameOf(hash)));
            Debug.WriteLine("data server started. " + VehicleCatalogue.All.Length + " vehicles, " + WeaponCatalogue.All.Length + " weapons.");
        }

        static string[] FindVehicles(string partial)
        {
            VehicleInfo[] found = VehicleCatalogue.Find(partial);
            var rows = new string[found.Length];
            for (int i = 0; i < found.Length; i++)
            {
                VehicleInfo v = found[i];
                rows[i] = v.Hash + "\t" + v.Type + "\t" + v.ModelName + "\t" + (v.DisplayName ?? "") + "\t" + (v.RealName ?? "");
            }
            return rows;
        }

        static string[] FindWeapons(string partial)
        {
            Weapon[] found = WeaponCatalogue.Find(partial);
            var rows = new string[found.Length];
            for (int i = 0; i < found.Length; i++)
                rows[i] = found[i].Hash + "\t" + found[i].Name;
            return rows;
        }
    }
}
