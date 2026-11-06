using System;
using System.Collections.Generic;

namespace Shared
{
    public static class Catalogue
    {
        static readonly Dictionary<string, ItemType> ByUname = new Dictionary<string, ItemType>();
        static readonly string[] Order;
        static readonly Dictionary<string, LootTable> LootByName = new Dictionary<string, LootTable>();
        static readonly string[] LootNames;
        static readonly Dictionary<string, float> Bleed = new Dictionary<string, float>();
        static readonly Dictionary<string, string> CalibreNames = new Dictionary<string, string>();
        static readonly Dictionary<string, string[]> AmmoByCalibre = new Dictionary<string, string[]>();
        static readonly Dictionary<string, string> ScarcityValues = new Dictionary<string, string>();

        public static int ItemCount { get { return Order.Length; } }
        public static int LootTableCount { get { return LootNames.Length; } }

        static Catalogue()
        {
            ItemType[] types = ItemData.Types();
            Order = new string[types.Length];
            var ammo = new Dictionary<string, List<string>>();
            for (int i = 0; i < types.Length; i++)
            {
                ItemType item = types[i];
                Order[i] = item.Uname;
                ByUname.Add(item.Uname, item);
                if (item.Ammo != null && item.Ammo.Calibre != null)
                {
                    List<string> list;
                    if (!ammo.TryGetValue(item.Ammo.Calibre, out list))
                    {
                        list = new List<string>();
                        ammo.Add(item.Ammo.Calibre, list);
                    }
                    list.Add(item.Uname);
                }
            }
            foreach (var pair in ammo)
                AmmoByCalibre.Add(pair.Key, pair.Value.ToArray());

            ItemData.FillCalibres(Bleed, CalibreNames);

            LootTable[] tables = LootData.Tables();
            LootNames = new string[tables.Length];
            for (int i = 0; i < tables.Length; i++)
            {
                LootNames[i] = tables[i].Name;
                LootByName.Add(tables[i].Name, tables[i]);
            }

            for (int i = 0; i < Scarcity.All.Length; i++)
                ScarcityValues.Add(Scarcity.All[i].Key, Scarcity.All[i].Value);
        }

        public static ItemType GetItemType(string uname)
        {
            ItemType item;
            if (uname != null && ByUname.TryGetValue(uname, out item))
                return item;
            return null;
        }

        public static string[] ItemTypes()
        {
            var copy = new string[Order.Length];
            Array.Copy(Order, copy, Order.Length);
            return copy;
        }

        public static LootTable GetLootTable(string name)
        {
            LootTable table;
            if (name != null && LootByName.TryGetValue(name, out table))
                return table;
            return null;
        }

        public static string[] LootTableNames()
        {
            var copy = new string[LootNames.Length];
            Array.Copy(LootNames, copy, LootNames.Length);
            return copy;
        }

        public static object CalibreBleed(string name)
        {
            float value;
            if (name != null && Bleed.TryGetValue(name, out value))
                return value;
            return null;
        }

        public static string CalibreName(string name)
        {
            string value;
            if (name != null && CalibreNames.TryGetValue(name, out value))
                return value;
            return null;
        }

        public static string[] AmmoForCalibre(string name)
        {
            string[] list;
            if (name != null && AmmoByCalibre.TryGetValue(name, out list))
            {
                var copy = new string[list.Length];
                Array.Copy(list, copy, list.Length);
                return copy;
            }
            return new string[0];
        }

        public static string GetScarcity(string key)
        {
            string value;
            if (key != null && ScarcityValues.TryGetValue(key, out value))
                return value;
            return null;
        }

        public static ScarcitySetting[] ScarcitySettings()
        {
            var copy = new ScarcitySetting[Scarcity.All.Length];
            Array.Copy(Scarcity.All, copy, Scarcity.All.Length);
            return copy;
        }

        public static Dictionary<string, object> ItemTypeMap(string uname)
        {
            ItemType item = GetItemType(uname);
            if (item == null)
                return null;
            var map = new Dictionary<string, object>();
            map.Add("uname", item.Uname);
            map.Add("name", item.Name);
            map.Add("size", item.Size);
            map.Add("category", item.Category);
            map.Add("maxHitpoints", item.MaxHitpoints);
            map.Add("price", item.Price);
            map.Add("longPickup", item.LongPickup);
            map.Add("useCarryAnim", item.UseCarryAnim);
            if (item.Weapon != null)
                map.Add("weapon", WeaponMap(item.Weapon));
            if (item.Ammo != null)
                map.Add("ammo", AmmoMap(item.Ammo));
            if (item.Food != null)
                map.Add("food", FoodMap(item.Food));
            if (item.Liquid != null)
                map.Add("liquid", LiquidMap(item.Liquid));
            if (item.Bag != null)
                map.Add("bag", BagMap(item.Bag));
            if (item.Container != null)
                map.Add("container", ContainerMap(item.Container));
            if (item.Explosive != null)
                map.Add("explosive", ExplosiveMap(item.Explosive));
            return map;
        }

        public static Dictionary<string, object> LootTableMap(string name)
        {
            LootTable table = GetLootTable(name);
            if (table == null)
                return null;
            var entries = new Dictionary<string, object>[table.Entries.Length];
            for (int i = 0; i < table.Entries.Length; i++)
            {
                LootEntry entry = table.Entries[i];
                var row = new Dictionary<string, object>();
                row.Add("uname", entry.Uname);
                row.Add("weight", entry.Weight);
                row.Add("perLimit", entry.PerLimit);
                row.Add("serverLimit", entry.ServerLimit);
                entries[i] = row;
            }
            var map = new Dictionary<string, object>();
            map.Add("name", table.Name);
            map.Add("mult", table.Mult);
            map.Add("entries", entries);
            return map;
        }

        static Dictionary<string, object> WeaponMap(WeaponStats weapon)
        {
            var map = new Dictionary<string, object>();
            Add(map, "calibre", weapon.Calibre);
            Add(map, "flags", weapon.Flags);
            Add(map, "magSize", weapon.MagSize);
            Add(map, "maxReserveMags", weapon.MaxReserveMags);
            map.Add("melee", weapon.Melee);
            Add(map, "muzzleVelocity", weapon.MuzzleVelocity);
            Add(map, "liquid", weapon.Liquid);
            Add(map, "animSet", weapon.AnimSet);
            Add(map, "bleed", weapon.Bleed);
            Add(map, "knockout", weapon.Knockout);
            return map;
        }

        static Dictionary<string, object> AmmoMap(AmmoStats ammo)
        {
            var map = new Dictionary<string, object>();
            map.Add("calibre", ammo.Calibre);
            map.Add("name", ammo.Name);
            map.Add("bleedMult", ammo.BleedMult);
            map.Add("koMult", ammo.KoMult);
            map.Add("penetration", ammo.Penetration);
            map.Add("rounds", ammo.Rounds);
            map.Add("noTransfer", ammo.NoTransfer);
            return map;
        }

        static Dictionary<string, object> FoodMap(FoodStats food)
        {
            var map = new Dictionary<string, object>();
            map.Add("biteValue", food.BiteValue);
            map.Add("maxBites", food.MaxBites);
            map.Add("canCook", food.CanCook);
            map.Add("canRawInfect", food.CanRawInfect);
            map.Add("destroyOnEnd", food.DestroyOnEnd);
            return map;
        }

        static Dictionary<string, object> LiquidMap(LiquidStats liquid)
        {
            var parts = new Dictionary<string, object>[liquid.Parts.Length];
            for (int i = 0; i < liquid.Parts.Length; i++)
            {
                var part = new Dictionary<string, object>();
                part.Add("name", liquid.Parts[i].Name);
                part.Add("share", liquid.Parts[i].Share);
                parts[i] = part;
            }
            var map = new Dictionary<string, object>();
            map.Add("capacity", liquid.Capacity);
            map.Add("reusable", liquid.Reusable);
            map.Add("parts", parts);
            return map;
        }

        static Dictionary<string, object> BagMap(BagStats bag)
        {
            var map = new Dictionary<string, object>();
            map.Add("name", bag.Name);
            map.Add("size", bag.Size);
            return map;
        }

        static Dictionary<string, object> ContainerMap(ContainerStats container)
        {
            var map = new Dictionary<string, object>();
            map.Add("size", container.Size);
            Add(map, "fill", container.Fill);
            return map;
        }

        static Dictionary<string, object> ExplosiveMap(ExplosiveStats explosive)
        {
            var map = new Dictionary<string, object>();
            map.Add("preset", explosive.Preset);
            map.Add("trigger", explosive.Trigger);
            return map;
        }

        static void Add(Dictionary<string, object> map, string key, string value)
        {
            if (value != null)
                map.Add(key, value);
        }

        static void Add(Dictionary<string, object> map, string key, int? value)
        {
            if (value.HasValue)
                map.Add(key, value.Value);
        }

        static void Add(Dictionary<string, object> map, string key, float? value)
        {
            if (value.HasValue)
                map.Add(key, value.Value);
        }
    }
}
