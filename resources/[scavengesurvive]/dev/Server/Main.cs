using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Server
{
    /// <summary>
    /// F011. Not ensured in prod. Live checks only. No test harness.
    /// The statement probe is MySql.Single. Decision 22 cancelled CALL and item_give.
    /// The survey command prints the server ped point. The loot insert is F066.
    /// </summary>
    public class Main : ServerScript
    {
        const int MinuteMs = 60000;

        const string ShapeSql = "SELECT 1 AS ok, 'READY' AS code, CAST(NULL AS CHAR) AS payload";

        const string TypeSql =
            "SELECT CAST(NULL AS SIGNED) AS n, CAST(12.50 AS DECIMAL(10,2)) AS fuel, " +
            "CAST(4000.5 AS FLOAT) AS x, " +
            "ST_X(ST_GeomFromText('POINT(4000.5 1200.25)')) AS px, " +
            "ST_Y(ST_GeomFromText('POINT(4000.5 1200.25)')) AS py";

        const string BadSql = "SELECT 1 AS n FROM dev_missing_table";

        const string CountSql = "SELECT COUNT(*) FROM accounts";

        static readonly Regex TableName = new Regex("^[a-z0-9_]{1,32}$", RegexOptions.Compiled);
        static readonly Regex ZoneName = new Regex("^[a-z0-9_]{1,16}$", RegexOptions.Compiled);

        bool _probed;
        bool _busy;
        long _nextCount;

        public Main()
        {
            API.RegisterCommand("devprobe", new Action<int, List<object>, string>(OnProbe), true);
            API.RegisterCommand("devcounts", new Action<int, List<object>, string>(OnCounts), true);
            API.RegisterCommand("survey", new Action<int, List<object>, string>(OnSurvey), true);
            Tick += Loop;
            Debug.WriteLine("dev server started. not ensured. statement probe. no test suite.");
        }

        async Task Loop()
        {
            while (true)
            {
                await Delay(1000);
                try
                {
                    long now = NowMs();
                    if (!_probed && MySql.IsReady())
                    {
                        _probed = true;
                        _nextCount = now + MinuteMs;
                        RunProbe(0);
                        ReportCounts("start");
                    }
                    else if (_probed && now >= _nextCount)
                    {
                        _nextCount = now + MinuteMs;
                        ReportCounts("minute");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[dev] tick failed: " + ex.GetType().Name);
                }
            }
        }

        void OnProbe(int source, List<object> args, string raw)
        {
            if (!MySql.IsReady())
            {
                Tell(source, "dev", "oxmysql is not started.");
                return;
            }

            RunProbe(source);
        }

        void OnCounts(int source, List<object> args, string raw)
        {
            string line = ReportCounts("command");
            Tell(source, "dev", line);
        }

        void OnSurvey(int source, List<object> args, string raw)
        {
            if (source <= 0)
            {
                Tell(source, "survey", "Stand on the spot in game. The console has no ped. Nothing is written.");
                return;
            }

            string table = Arg(args, 0);
            string zone = Arg(args, 1);
            if (table.Length > 0 && !TableName.IsMatch(table))
            {
                Tell(source, "survey", "Table is 1-32 chars of a-z, 0-9, or underscore. Nothing is written.");
                return;
            }

            if (zone.Length > 0 && !ZoneName.IsMatch(zone))
            {
                Tell(source, "survey", "Zone is 1-16 chars of a-z, 0-9, or underscore. Nothing is written.");
                return;
            }

            string id = source.ToString(CultureInfo.InvariantCulture);
            int ped = API.GetPlayerPed(id);
            if (ped == 0)
            {
                Tell(source, "survey", "No ped yet. Nothing is written.");
                return;
            }

            Vector3 coords = API.GetEntityCoords(ped);
            float heading = API.GetEntityHeading(ped);
            int bucket = API.GetPlayerRoutingBucket(id);
            var fields = new Dictionary<string, object>();
            fields["x"] = Num(coords.X);
            fields["y"] = Num(coords.Y);
            fields["z"] = Num(coords.Z);
            fields["heading"] = Num(heading);
            fields["bucket"] = bucket;
            fields["table_name"] = table;
            fields["zone"] = zone;
            fields["written"] = false;
            string line = "x=" + Num(coords.X) + " y=" + Num(coords.Y) + " z=" + Num(coords.Z)
                + " heading=" + Num(heading) + " bucket=" + bucket.ToString(CultureInfo.InvariantCulture)
                + " table=" + (table.Length == 0 ? "-" : table)
                + " zone=" + (zone.Length == 0 ? "-" : zone)
                + " written=no";
            Note("dev.surveyed", fields);
            Tell(source, "survey", line + ". F066 writes the row.");
        }

        void RunProbe(int reply)
        {
            if (_busy)
            {
                Tell(reply, "dev", "Probe is already running.");
                return;
            }

            _busy = true;
            var bag = new Probe();
            bag.Reply = reply;
            MySql.Scalar(CountSql, new Action<dynamic>(before =>
            {
                bag.Before = before;
                MySql.Single(ShapeSql, new Action<dynamic>(shape =>
                {
                    bag.Shape = shape;
                    MySql.Single(TypeSql, new Action<dynamic>(types =>
                    {
                        bag.Types = types;
                        MySql.Single(BadSql, new Action<dynamic>(bad =>
                        {
                            bag.Bad = bad;
                            MySql.Scalar(CountSql, new Action<dynamic>(after =>
                            {
                                bag.After = after;
                                _busy = false;
                                try
                                {
                                    Finish(bag);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("[dev] probe finish failed: " + ex.GetType().Name);
                                }
                            }));
                        }));
                    }));
                }));
            }));
        }

        void Finish(Probe bag)
        {
            var fields = new Dictionary<string, object>();
            bool flat = bag.Shape != null && !(bag.Shape is IList) && !(bag.Shape is string);
            fields["flat"] = flat;
            fields["shape_type"] = TypeName(bag.Shape);
            fields["ok"] = flat ? (object)MySql.Int(bag.Shape, "ok") : null;
            fields["ok_type"] = flat ? TypeName(MySql.Field(bag.Shape, "ok")) : "missing";
            fields["code"] = flat ? MySql.Text(bag.Shape, "code") : null;
            object payload = flat ? MySql.Field(bag.Shape, "payload") : null;
            fields["payload"] = payload == null || payload is DBNull ? "null" : Convert.ToString(payload, CultureInfo.InvariantCulture);
            fields["payload_type"] = TypeName(payload);

            bool types = bag.Types != null && !(bag.Types is IList) && !(bag.Types is string);
            fields["types_flat"] = types;
            object fuel = types ? MySql.Field(bag.Types, "fuel") : null;
            object n = types ? MySql.Field(bag.Types, "n") : null;
            object x = types ? MySql.Field(bag.Types, "x") : null;
            object px = types ? MySql.Field(bag.Types, "px") : null;
            object py = types ? MySql.Field(bag.Types, "py") : null;
            fields["n_type"] = TypeName(n);
            fields["fuel"] = Show(fuel);
            fields["fuel_type"] = TypeName(fuel);
            fields["x"] = Show(x);
            fields["x_type"] = TypeName(x);
            fields["px"] = Show(px);
            fields["py"] = Show(py);
            double dx;
            double dy;
            fields["point_cm"] = PointErrorCm(px, py, out dx, out dy);
            fields["bad_type"] = TypeName(bag.Bad);
            fields["bad_empty"] = bag.Bad == null;
            fields["accounts_before"] = Show(bag.Before);
            fields["accounts_after"] = Show(bag.After);
            fields["accounts_unchanged"] = SameCount(bag.Before, bag.After);

            Note("dev.probed", fields);
            Tell(bag.Reply, "dev",
                "flat=" + (flat ? "yes" : "no")
                + " ok=" + Show(fields["ok"])
                + " code=" + Show(fields["code"])
                + " payload=" + Show(fields["payload"])
                + " fuel=" + Show(fuel)
                + " x=" + Show(x)
                + " px=" + Show(px)
                + " py=" + Show(py)
                + " bad_empty=" + (bag.Bad == null ? "yes" : "no")
                + " accounts_unchanged=" + (SameCount(bag.Before, bag.After) ? "yes" : "no"));
        }

        string ReportCounts(string reason)
        {
            int peds = CountPool(SafePool(API.GetAllPeds));
            int vehicles = CountPool(SafePool(API.GetAllVehicles));
            int objects = CountPool(SafePool(API.GetAllObjects));
            int players = API.GetNumPlayerIndices();
            int entities = peds + vehicles + objects;
            var fields = new Dictionary<string, object>();
            fields["reason"] = reason;
            fields["players"] = players;
            fields["peds"] = peds;
            fields["vehicles"] = vehicles;
            fields["objects"] = objects;
            fields["entities"] = entities;
            Note("dev.counted", fields);
            return "players=" + players.ToString(CultureInfo.InvariantCulture)
                + " peds=" + peds.ToString(CultureInfo.InvariantCulture)
                + " vehicles=" + vehicles.ToString(CultureInfo.InvariantCulture)
                + " objects=" + objects.ToString(CultureInfo.InvariantCulture)
                + " entities=" + entities.ToString(CultureInfo.InvariantCulture);
        }

        void Note(string kind, Dictionary<string, object> fields)
        {
            Debug.WriteLine("[dev] " + kind + " " + Join(fields));
            if (API.GetResourceState("logs") != "started")
            {
                return;
            }

            try
            {
                dynamic logs = Exports["logs"];
                logs.@event(kind, fields, "info");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[dev] logs export failed: " + ex.GetType().Name);
            }
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

        static string Arg(List<object> args, int index)
        {
            if (args == null || index >= args.Count || args[index] == null)
            {
                return "";
            }

            return Convert.ToString(args[index], CultureInfo.InvariantCulture).Trim();
        }

        static object SafePool(Func<object> read)
        {
            try
            {
                return read();
            }
            catch (Exception)
            {
                return null;
            }
        }

        static int CountPool(object pool)
        {
            if (pool == null)
            {
                return 0;
            }

            ICollection collection = pool as ICollection;
            if (collection != null)
            {
                return collection.Count;
            }

            IEnumerable enumerable = pool as IEnumerable;
            if (enumerable == null)
            {
                return 0;
            }

            int count = 0;
            foreach (object unused in enumerable)
            {
                count++;
            }

            return count;
        }

        static bool SameCount(object before, object after)
        {
            if (before == null || after == null || before is DBNull || after is DBNull)
            {
                return false;
            }

            try
            {
                return Convert.ToInt64(before, CultureInfo.InvariantCulture) == Convert.ToInt64(after, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return Convert.ToString(before, CultureInfo.InvariantCulture) == Convert.ToString(after, CultureInfo.InvariantCulture);
            }
        }

        static string PointErrorCm(object px, object py, out double dx, out double dy)
        {
            dx = double.NaN;
            dy = double.NaN;
            double x;
            double y;
            if (!TryDouble(px, out x) || !TryDouble(py, out y))
            {
                return "missing";
            }

            dx = Math.Abs(x - 4000.5) * 100.0;
            dy = Math.Abs(y - 1200.25) * 100.0;
            double worst = Math.Max(dx, dy);
            return worst.ToString("0.###", CultureInfo.InvariantCulture);
        }

        static bool TryDouble(object value, out double number)
        {
            number = 0;
            if (value == null || value is DBNull)
            {
                return false;
            }

            try
            {
                number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        static string Num(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        static string Show(object value)
        {
            if (value == null || value is DBNull)
            {
                return "null";
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        static string TypeName(object value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is DBNull)
            {
                return "DBNull";
            }

            return value.GetType().Name;
        }

        static string Join(Dictionary<string, object> fields)
        {
            var parts = new List<string>();
            foreach (KeyValuePair<string, object> pair in fields)
            {
                parts.Add(pair.Key + "=" + Show(pair.Value));
            }

            return string.Join(" ", parts.ToArray());
        }

        static long NowMs()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }

        sealed class Probe
        {
            public int Reply;
            public object Before;
            public object Shape;
            public object Types;
            public object Bad;
            public object After;
        }
    }
}
