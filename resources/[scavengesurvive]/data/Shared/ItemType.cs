namespace Shared
{
    public sealed class LiquidPart
    {
        public readonly string Name;
        public readonly float Share;

        public LiquidPart(string name, float share)
        {
            Name = name;
            Share = share;
        }
    }

    public sealed class WeaponStats
    {
        public readonly string Calibre;
        public readonly int? Flags;
        public readonly int? MagSize;
        public readonly int? MaxReserveMags;
        public readonly bool Melee;
        public readonly float? MuzzleVelocity;
        public readonly string Liquid;
        public readonly string AnimSet;
        public readonly float? Bleed;
        public readonly float? Knockout;

        public WeaponStats(string calibre, int? flags, int? magSize, int? maxReserveMags, bool melee, float? muzzleVelocity, string liquid, string animSet, float? bleed, float? knockout)
        {
            Calibre = calibre;
            Flags = flags;
            MagSize = magSize;
            MaxReserveMags = maxReserveMags;
            Melee = melee;
            MuzzleVelocity = muzzleVelocity;
            Liquid = liquid;
            AnimSet = animSet;
            Bleed = bleed;
            Knockout = knockout;
        }
    }

    public sealed class AmmoStats
    {
        public readonly string Calibre;
        public readonly string Name;
        public readonly float BleedMult;
        public readonly float KoMult;
        public readonly float Penetration;
        public readonly int Rounds;
        public readonly bool NoTransfer;

        public AmmoStats(string calibre, string name, float bleedMult, float koMult, float penetration, int rounds, bool noTransfer)
        {
            Calibre = calibre;
            Name = name;
            BleedMult = bleedMult;
            KoMult = koMult;
            Penetration = penetration;
            Rounds = rounds;
            NoTransfer = noTransfer;
        }
    }

    public sealed class FoodStats
    {
        public readonly float BiteValue;
        public readonly int MaxBites;
        public readonly bool CanCook;
        public readonly bool CanRawInfect;
        public readonly bool DestroyOnEnd;

        public FoodStats(float biteValue, int maxBites, bool canCook, bool canRawInfect, bool destroyOnEnd)
        {
            BiteValue = biteValue;
            MaxBites = maxBites;
            CanCook = canCook;
            CanRawInfect = canRawInfect;
            DestroyOnEnd = destroyOnEnd;
        }
    }

    public sealed class LiquidStats
    {
        public readonly float Capacity;
        public readonly bool Reusable;
        public readonly LiquidPart[] Parts;

        public LiquidStats(float capacity, bool reusable, LiquidPart[] parts)
        {
            Capacity = capacity;
            Reusable = reusable;
            Parts = parts;
        }
    }

    public sealed class BagStats
    {
        public readonly string Name;
        public readonly int Size;

        public BagStats(string name, int size)
        {
            Name = name;
            Size = size;
        }
    }

    public sealed class ContainerStats
    {
        public readonly int Size;
        public readonly string Fill;

        public ContainerStats(int size, string fill)
        {
            Size = size;
            Fill = fill;
        }
    }

    public sealed class ExplosiveStats
    {
        public readonly string Preset;
        public readonly string Trigger;

        public ExplosiveStats(string preset, string trigger)
        {
            Preset = preset;
            Trigger = trigger;
        }
    }

    /// <summary>
    /// Gameplay fields of one MTA item type. SA model ids, attach offsets, wear offsets,
    /// and defence geometry are not runtime. Prop and Weapon joaats are F007.
    /// </summary>
    public sealed class ItemType
    {
        public readonly string Uname;
        public readonly string Name;
        public readonly int Size;
        public readonly string Category;
        public readonly int MaxHitpoints;
        public readonly int Price;
        public readonly bool LongPickup;
        public readonly bool UseCarryAnim;
        public readonly WeaponStats Weapon;
        public readonly AmmoStats Ammo;
        public readonly FoodStats Food;
        public readonly LiquidStats Liquid;
        public readonly BagStats Bag;
        public readonly ContainerStats Container;
        public readonly ExplosiveStats Explosive;

        public ItemType(string uname, string name, int size, string category, int maxHitpoints, int price, bool longPickup, bool useCarryAnim, WeaponStats weapon, AmmoStats ammo, FoodStats food, LiquidStats liquid, BagStats bag, ContainerStats container, ExplosiveStats explosive)
        {
            Uname = uname;
            Name = name;
            Size = size;
            Category = category;
            MaxHitpoints = maxHitpoints;
            Price = price;
            LongPickup = longPickup;
            UseCarryAnim = useCarryAnim;
            Weapon = weapon;
            Ammo = ammo;
            Food = food;
            Liquid = liquid;
            Bag = bag;
            Container = container;
            Explosive = explosive;
        }
    }

    public sealed class LootEntry
    {
        public readonly string Uname;
        public readonly float Weight;
        public readonly int PerLimit;
        public readonly int ServerLimit;

        public LootEntry(string uname, float weight, int perLimit, int serverLimit)
        {
            Uname = uname;
            Weight = weight;
            PerLimit = perLimit;
            ServerLimit = serverLimit;
        }
    }

    public sealed class LootTable
    {
        public readonly string Name;
        public readonly float Mult;
        public readonly LootEntry[] Entries;

        public LootTable(string name, float mult, LootEntry[] entries)
        {
            Name = name;
            Mult = mult;
            Entries = entries;
        }
    }
}
