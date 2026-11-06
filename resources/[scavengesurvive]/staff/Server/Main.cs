using System;
using System.Collections.Generic;
using System.Globalization;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Server
{
    public class Main : ServerScript
    {
        static readonly HashSet<string> Weathers = new HashSet<string>
        {
            "CLEAR", "EXTRASUNNY", "CLOUDS", "OVERCAST", "RAIN", "CLEARING", "THUNDER",
            "SMOG", "FOGGY", "XMAS", "SNOW", "SNOWLIGHT", "BLIZZARD", "HALLOWEEN",
            "NEUTRAL", "RAIN_HALLOWEEN", "SNOW_HALLOWEEN"
        };

        readonly Dictionary<int, Pending> pending = new Dictionary<int, Pending>();
        int nextId;

        public Main()
        {
            API.RegisterCommand("time", new Action<int, List<object>, string>(Time), true);
            API.RegisterCommand("weather", new Action<int, List<object>, string>(Weather), true);
            API.RegisterCommand("tpp", new Action<int, List<object>, string>(TeleportToPlayer), true);
            API.RegisterCommand("tp", new Action<int, List<object>, string>(TeleportToCoords), true);
            API.RegisterCommand("tpm", new Action<int, List<object>, string>(TeleportToMarker), true);
            API.RegisterCommand("tpw", new Action<int, List<object>, string>(TeleportToMarker), true);
            API.RegisterCommand("respawn", new Action<int, List<object>, string>(Respawn), false);
            API.RegisterCommand("veh", new Action<int, List<object>, string>(Vehicle), false);
            API.RegisterCommand("giveweapon", new Action<int, List<object>, string>(GiveWeapon), true);
            EventHandlers["staff:measured"] += new Action<Player, int, float, float, float, float, float, float>(OnMeasured);
            Debug.WriteLine("staff server started.");
        }

        void Time(int source, List<object> args, string raw)
        {
            if (args == null || args.Count < 1)
            {
                Tell(source, "Time", "Usage: /time <hour>");
                return;
            }
            int hour;
            if (!int.TryParse(Convert.ToString(args[0]), out hour))
            {
                Tell(source, "Time", "Usage: /time <hour>");
                return;
            }
            TriggerClientEvent("time:set", -1, hour);
        }

        void Weather(int source, List<object> args, string raw)
        {
            if (args == null || args.Count < 1)
            {
                Tell(source, "Weather", "Usage: /weather <weather>");
                return;
            }
            string weather = Convert.ToString(args[0]).ToUpperInvariant();
            if (!Weathers.Contains(weather))
            {
                Tell(source, "Weather", "Invalid weather");
                return;
            }
            TriggerClientEvent("weather:set", -1, weather);
        }

        void TeleportToPlayer(int source, List<object> args, string raw)
        {
            int target;
            if (args == null || args.Count < 1 || !int.TryParse(Convert.ToString(args[0]), out target))
            {
                Tell(source, "Error", "Invalid player ID");
                return;
            }
            int ped = API.GetPlayerPed(source.ToString());
            int other = API.GetPlayerPed(target.ToString());
            Vector3 coords = API.GetEntityCoords(other);
            API.SetEntityCoords(ped, coords.X, coords.Y, coords.Z, true, false, false, false);
        }

        void TeleportToCoords(int source, List<object> args, string raw)
        {
            string input = raw;
            int space = input.IndexOf(' ');
            if (space < 0)
            {
                Tell(source, "Teleport", "Use: /tp x,y,z[,heading]");
                return;
            }
            input = input.Substring(space + 1).Trim();
            float x, y, z, heading;
            bool hasHeading;
            if (!TryCoords(input, out x, out y, out z, out heading, out hasHeading))
            {
                Tell(source, "Teleport", "Invalid coordinates. Use: /tp x,y,z[,heading]");
                return;
            }
            int ped = API.GetPlayerPed(source.ToString());
            API.SetEntityCoords(ped, x, y, z, true, false, false, false);
            if (hasHeading) API.SetEntityHeading(ped, heading);
        }

        void TeleportToMarker(int source, List<object> args, string raw)
        {
            TriggerClientEvent("admin:teleportToMarker", source);
            Debug.WriteLine("(" + source + ") teleported to marker.");
        }

        void Respawn(int source, List<object> args, string raw)
        {
            int target = source;
            if (args != null && args.Count > 0)
                int.TryParse(Convert.ToString(args[0]), out target);
            TriggerClientEvent("player:respawn", target);
        }

        void Vehicle(int source, List<object> args, string raw)
        {
            if (args == null || args.Count < 1)
            {
                Tell(source, "Vehicle", "Usage: /veh <name>");
                return;
            }
            string[] rows = Exports["data"].findVehicles(Convert.ToString(args[0]));
            if (rows == null || rows.Length == 0)
            {
                Tell(source, "Vehicle Not Found", "The vehicle you are looking for does not exist.");
                return;
            }
            if (rows.Length > 1)
            {
                int shown = Math.Min(rows.Length, 12);
                Tell(source, "Vehicles", rows.Length + " matches. Type a more specific /veh name.");
                for (int i = 0; i < shown; i++)
                {
                    string[] parts = rows[i].Split('\t');
                    Tell(source, parts.Length > 2 ? parts[2] : rows[i], parts.Length > 3 ? parts[3] : "");
                }
                return;
            }
            AskMeasure(source, rows[0]);
        }

        void GiveWeapon(int source, List<object> args, string raw)
        {
            if (args == null || args.Count < 2)
            {
                Tell(source, "Weapon", "Usage: /giveweapon <player> <name> [ammo]");
                return;
            }
            int target;
            if (!int.TryParse(Convert.ToString(args[0]), out target))
            {
                Tell(source, "Weapon", "Invalid player ID");
                return;
            }
            int ammo = 250;
            if (args.Count > 2) int.TryParse(Convert.ToString(args[2]), out ammo);
            string[] found = Exports["data"].findWeapons(Convert.ToString(args[1]));
            if (found == null || found.Length == 0)
            {
                Tell(source, "Weapon", "No weapon matches that name.");
                return;
            }
            if (found.Length > 1)
            {
                int shown = Math.Min(found.Length, 12);
                Tell(source, "Weapons", found.Length + " matches. Type a more specific name.");
                for (int i = 0; i < shown; i++)
                {
                    string[] parts = found[i].Split('\t');
                    Tell(source, parts[0], parts.Length > 1 ? parts[1] : "");
                }
                return;
            }
            string[] one = found[0].Split('\t');
            uint hash = (uint)API.GetHashKey(one[0]);
            API.GiveWeaponToPed(API.GetPlayerPed(target.ToString()), hash, ammo, false, true);
            string targetName = API.GetPlayerName(target.ToString());
            string weaponName = one.Length > 1 ? one[1] : one[0];
            if (target != source)
                Tell(source, "Weapon Given", "You gave " + weaponName + " to " + targetName + ".");
            Tell(target, "Weapon Received", "You have received a " + weaponName + ".");
            Debug.WriteLine("(" + source + ") gave " + weaponName + " to " + targetName);
        }

        void AskMeasure(int source, string row)
        {
            string[] parts = row.Split('\t');
            uint hash = uint.Parse(parts[0], CultureInfo.InvariantCulture);
            int id = ++nextId;
            pending[id] = new Pending { Source = source, Hash = hash, Type = parts[1], Model = parts[2] };
            TriggerClientEvent("staff:measure", source, id, hash);
        }

        void OnMeasured([FromSource] Player player, int id, float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
        {
            Pending job;
            if (!pending.TryGetValue(id, out job) || job.Source.ToString() != player.Handle)
                return;
            pending.Remove(id);

            int ped = API.GetPlayerPed(player.Handle);
            Vector3 coords = API.GetEntityCoords(ped);
            // min is the model size (max-min). max is the ped forward vector.
            float sizeX = Math.Abs(minX);
            float sizeY = Math.Abs(minY);
            float sizeZ = Math.Abs(minZ);
            float distance = sizeY / 2f + 1f;
            Vector3 spawn = coords + new Vector3(maxX, maxY, maxZ) * distance;
            float radius = Math.Max(sizeX, Math.Max(sizeY, sizeZ)) / 2f + 0.5f;
            if (Blocked(spawn, radius, ped))
            {
                Tell(job.Source, "Spawn Area Obstructed", "The area is obstructed by another vehicle or player.");
                return;
            }
            API.CreateVehicleServerSetter(job.Hash, job.Type, spawn.X, spawn.Y, spawn.Z, API.GetEntityHeading(ped));
            Debug.WriteLine("(" + job.Source + ") spawned " + job.Model + " at " + spawn.X.ToString("0.00") + ", " + spawn.Y.ToString("0.00") + ", " + spawn.Z.ToString("0.00"));
        }

        bool Blocked(Vector3 at, float radius, int ignorePed)
        {
            object vehicles = API.GetAllVehicles();
            object[] list = vehicles as object[];
            if (list != null)
            {
                for (int i = 0; i < list.Length; i++)
                {
                    int entity = Convert.ToInt32(list[i]);
                    Vector3 there = API.GetEntityCoords(entity);
                    if (there.DistanceToSquared(at) <= radius * radius) return true;
                }
            }
            foreach (Player other in Players)
            {
                int ped = API.GetPlayerPed(other.Handle);
                if (ped == ignorePed) continue;
                if (API.GetEntityCoords(ped).DistanceToSquared(at) <= radius * radius) return true;
            }
            return false;
        }

        static bool TryCoords(string input, out float x, out float y, out float z, out float heading, out bool hasHeading)
        {
            x = y = z = heading = 0f;
            hasHeading = false;
            string[] parts = input.Split(',');
            if (parts.Length == 3 || parts.Length == 4)
            {
                if (float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                    float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out y) &&
                    float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                {
                    if (parts.Length == 4 && float.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out heading))
                        hasHeading = true;
                    return true;
                }
            }
            return false;
        }

        void Tell(int source, string title, string message)
        {
            if (source <= 0)
            {
                Debug.WriteLine(title + ": " + message);
                return;
            }
            TriggerClientEvent("chat:addMessage", source, new Dictionary<string, object>
            {
                { "color", new[] { 255, 180, 40 } },
                { "args", new[] { title, message } }
            });
        }

        sealed class Pending
        {
            public int Source;
            public uint Hash;
            public string Type;
            public string Model;
        }
    }
}
