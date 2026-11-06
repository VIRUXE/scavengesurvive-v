using System;
using System.Collections.Generic;
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
            Exports.Add("getItemType", new Func<string, Dictionary<string, object>>(Shared.Catalogue.ItemTypeMap));
            Exports.Add("getLootTable", new Func<string, Dictionary<string, object>>(Shared.Catalogue.LootTableMap));
            Exports.Add("getItemTypes", new Func<string[]>(Shared.Catalogue.ItemTypes));
            Exports.Add("calibreBleed", new Func<string, object>(Shared.Catalogue.CalibreBleed));
            Exports.Add("calibreName", new Func<string, string>(Shared.Catalogue.CalibreName));
            Exports.Add("ammoForCalibre", new Func<string, string[]>(Shared.Catalogue.AmmoForCalibre));
            Exports.Add("langText", new Func<string, string, string>(Shared.Lang.Text));
            Exports.Add("langTextFor", new Func<string, string, string>(Shared.Lang.TextFor));
            Exports.Add("itemName", new Func<string, string, string>(Shared.Lang.ItemType));
            Exports.Add("scarcity", new Func<string, string>(Shared.Catalogue.GetScarcity));
            Debug.WriteLine("data server started. " + VehicleCatalogue.All.Length + " vehicles, " + WeaponCatalogue.All.Length + " weapons, " + Shared.Catalogue.ItemCount + " item types, " + Shared.Catalogue.LootTableCount + " loot tables.");
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
