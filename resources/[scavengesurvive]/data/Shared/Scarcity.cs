namespace Shared
{
    public sealed class ScarcitySetting
    {
        public readonly string Key;
        public readonly string Value;

        public ScarcitySetting(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }

    /// <summary>
    /// ADR 0001 defaults. Strings match sql/seed/0003_settings.sql, which F014 loads into settings.
    /// </summary>
    static class Scarcity
    {
        internal static readonly ScarcitySetting[] All = new ScarcitySetting[]
        {
            new ScarcitySetting("loot.cap_multiplier", "1.0"),
            new ScarcitySetting("loot.refill_rate", "0.10"),
            new ScarcitySetting("loot.refill_floor", "0.02"),
            new ScarcitySetting("loot.count_carried", "1"),
            new ScarcitySetting("loot.cooldown_minutes", "60"),
            new ScarcitySetting("loot.cooldown_jitter", "0.5"),
            new ScarcitySetting("loot.despawn_minutes", "120"),
            new ScarcitySetting("loot.items_per_point", "2.0"),
            new ScarcitySetting("vehicles.cap_multiplier", "1.0"),
            new ScarcitySetting("vehicles.refill_rate", "0.10"),
            new ScarcitySetting("vehicles.refill_floor", "0.02"),
            new ScarcitySetting("vehicles.cooldown_hours", "6"),
            new ScarcitySetting("vehicles.cooldown_jitter", "0.33"),
            new ScarcitySetting("vehicles.abandon_days", "3"),
            new ScarcitySetting("vehicles.leave_radius", "100"),
            new ScarcitySetting("world.min_player_distance", "150"),
            new ScarcitySetting("world.refill_interval_s", "300")
        };
    }
}
