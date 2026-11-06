using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Server
{
    /// <summary>
    /// C# stand-in for <c>@oxmysql/lib/MySQL.lua</c>.
    /// Compiled into each server assembly. Calls oxmysql. Placeholders only. Not a procedure wrapper.
    /// FiveM constructs this script because it extends <see cref="ServerScript"/>.
    /// </summary>
    public class MySql : ServerScript
    {
        static MySql _self;

        public MySql()
        {
            _self = this;
        }

        public static void Query(string sql, Action<dynamic> callback)
        {
            Query(sql, new object[0], callback);
        }

        public static void Query(string sql, object parameters, Action<dynamic> callback)
        {
            Call("query", sql, parameters, callback);
        }

        public static void Single(string sql, Action<dynamic> callback)
        {
            Single(sql, new object[0], callback);
        }

        public static void Single(string sql, object parameters, Action<dynamic> callback)
        {
            Call("single", sql, parameters, callback);
        }

        public static void Scalar(string sql, Action<dynamic> callback)
        {
            Scalar(sql, new object[0], callback);
        }

        public static void Scalar(string sql, object parameters, Action<dynamic> callback)
        {
            Call("scalar", sql, parameters, callback);
        }

        public static void Update(string sql, Action<dynamic> callback)
        {
            Update(sql, new object[0], callback);
        }

        public static void Update(string sql, object parameters, Action<dynamic> callback)
        {
            Call("update", sql, parameters, callback);
        }

        public static void Insert(string sql, Action<dynamic> callback)
        {
            Insert(sql, new object[0], callback);
        }

        public static void Insert(string sql, object parameters, Action<dynamic> callback)
        {
            Call("insert", sql, parameters, callback);
        }

        public static void Prepare(string sql, object parameters, Action<dynamic> callback)
        {
            Call("prepare", sql, parameters, callback);
        }

        public static void Raw(string sql, object parameters, Action<dynamic> callback)
        {
            Call("rawExecute", sql, parameters, callback);
        }

        public static void Transaction(object queries, object parameters, Action<dynamic> callback)
        {
            Invoke("transaction", queries, parameters ?? new object[0], callback);
        }

        public static bool IsReady()
        {
            if (_self == null || API.GetResourceState("oxmysql") != "started")
            {
                return false;
            }

            object ready = _self.Exports["oxmysql"].isReady();
            return ready is bool && (bool)ready;
        }

        public static object Field(dynamic row, string key)
        {
            if (row == null || key == null)
            {
                return null;
            }

            IDictionary<string, object> dict = row as IDictionary<string, object>;
            if (dict != null)
            {
                object value;
                if (dict.TryGetValue(key, out value))
                {
                    return value;
                }
            }

            try
            {
                return row[key];
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static int Int(dynamic row, string key)
        {
            object value = Field(row, key);
            if (value == null || value is DBNull)
            {
                return 0;
            }

            return Convert.ToInt32(value);
        }

        public static string Text(dynamic row, string key)
        {
            object value = Field(row, key);
            return value == null || value is DBNull ? null : Convert.ToString(value);
        }

        static void Call(string method, string sql, object parameters, Action<dynamic> callback)
        {
            Invoke(method, sql, parameters ?? new object[0], callback);
        }

        static void Invoke(string method, object first, object second, Action<dynamic> callback)
        {
            if (_self == null || API.GetResourceState("oxmysql") != "started")
            {
                Debug.WriteLine("MySql." + method + " skipped. oxmysql is not started.");
                if (callback != null)
                {
                    callback(null);
                }

                return;
            }

            dynamic ox = _self.Exports["oxmysql"];
            if (method == "query") ox.query(first, second, callback);
            else if (method == "single") ox.single(first, second, callback);
            else if (method == "scalar") ox.scalar(first, second, callback);
            else if (method == "update") ox.update(first, second, callback);
            else if (method == "insert") ox.insert(first, second, callback);
            else if (method == "prepare") ox.prepare(first, second, callback);
            else if (method == "rawExecute") ox.rawExecute(first, second, callback);
            else if (method == "transaction") ox.transaction(first, second, callback);
            else
            {
                Debug.WriteLine("MySql." + method + " is not a method.");
                if (callback != null)
                {
                    callback(null);
                }
            }
        }
    }
}
