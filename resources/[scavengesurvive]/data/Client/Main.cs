using System;
using System.Collections.Generic;
using CitizenFX.Core;

namespace Client
{
    public class Main : BaseScript
    {
        public Main()
        {
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
            Debug.WriteLine("data client started. " + Shared.Catalogue.ItemCount + " item types, " + Shared.Catalogue.LootTableCount + " loot tables.");
        }
    }
}
