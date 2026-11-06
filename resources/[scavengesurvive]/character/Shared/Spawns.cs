namespace Shared
{
    sealed class Spawn
    {
        public readonly float X, Y, Z, Heading;
        public readonly string Description;
        public readonly bool GiveParachute;

        public Spawn(float x, float y, float z, float heading, string description, bool giveParachute)
        {
            X = x; Y = y; Z = z; Heading = heading; Description = description; GiveParachute = giveParachute;
        }
    }

    static class Spawns
    {
        public static readonly Spawn[] All = new Spawn[]
        {
            new Spawn(-829.3729f, -1289.8170f, 4.0005f, 41.0737f, "Harbor base jumping location - helicopter launch point near Los Santos docks", false),
            new Spawn(1208.2003f, 174.3914f, 80.1245f, 10.500f, "Race track base jumping location - helicopter launch from Vinewood Race Track", false),
            new Spawn(2463.7935f, 1509.9562f, 35.0349f, 289.2623f, "Windmills base jumping location - helicopter launch from wind farm area", false),
            new Spawn(-274.6549f, 6633.8984f, 7.1166f, 60.1427f, "North cliff base jumping location - foot launch from northern mountain cliffs", false),
            new Spawn(-92.3500f, -854.3000f, 39.5710f, 1.8891f, "Maze Bank base jumping location - motorcycle launch from downtown skyscraper", false),
            new Spawn(-120.9200f, -976.0500f, 295.4900f, 358.9586f, "Crane base jumping location - foot launch from construction crane", true),
            new Spawn(-1237.2000f, 4540.7500f, 184.7500f, 164.6178f, "River cliff base jumping location - foot launch from river cliffs", false),
            new Spawn(-742.5269f, 4493.3149f, 75.1444f, 112.6f, "Runaway train base jumping location - helicopter launch near train tracks", false),
            new Spawn(-801.3582f, 298.8532f, 84.9490f, 104.2070f, "Golf course base jumping location - foot launch from golf course", false),
            new Spawn(-1367.5952f, 4381.9434f, 41.1320f, 329.4791f, "The 1K base jumping location - helicopter launch from mountain area", false),
            new Spawn(2517.9312f, 4971.7524f, 44.7082f, 0.0f, "The 1.5K base jumping location - helicopter launch from high altitude area", false),
            new Spawn(1054.5343f, -179.6562f, 70.3066f, 24.9200f, "Canal base jumping location - helicopter launch from canal area", false),
            new Spawn(-767.415f, 4331.792f, 147.6820f, 359.2885f, "Rock cliff base jumping location - foot launch from rock formations", false),
            new Spawn(-1152.0527f, -1857.8835f, 204.0663f, 0f, "Player position during Harbor base jump course", true),
            new Spawn(885.1140f, -437.3520f, 529.8670f, 0f, "Player position during Race Track base jump course", true),
            new Spawn(2034.9117f, 1971.0510f, 582.7461f, 0f, "Player position during Windmills base jump course", true),
            new Spawn(409.7498f, 5703.5254f, 695.1700f, 0f, "Player position during North Cliff base jump course", false),
            new Spawn(-74.9632f, -827.4467f, 324.9521f, 0f, "Player position during Maze Bank base jump course", true),
            new Spawn(-117.6998f, -975.5710f, 295.0000f, 0f, "Player position during Crane base jump course", false),
            new Spawn(-1243.7838f, 4534.1631f, 184.8471f, 0f, "Player position during River Cliff base jump course", false),
            new Spawn(-359.1000f, 4119.5000f, 304.1000f, 0f, "Player position during Runaway Train base jump course", true),
            new Spawn(-807.0730f, 330.8846f, 232.6766f, 0f, "Player position during Golf Course base jump course", true),
            new Spawn(-1286.9900f, 3668.9216f, 1072.4663f, 0f, "Player position during The 1K base jump course", true),
            new Spawn(1018.4410f, 3956.7056f, 1354.0000f, 0f, "Player position during The 1.5K base jump course", true),
            new Spawn(1627.1957f, -421.7584f, 1321.4835f, 0f, "Player position during Canal base jump course", true),
            new Spawn(-766.5999f, 4334.8052f, 147.1205f, 0f, "Player position during Rock Cliff base jump course", false),
        };
    }
}
