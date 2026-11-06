using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Loki push body. No socket. Timestamps are decimal strings because the
    /// nanosecond value does not need to ride in a double.
    /// </summary>
    public static class LokiBody
    {
        public const int BatchSize = 500;
        public const int BackoffStartS = 2;
        public const int BackoffMaxS = 60;

        public sealed class Entry
        {
            public string Ns;
            public string Line;
            public string Level;
            public string Resource;
            public string Kind;
            public Dictionary<string, string> Meta;
        }

        public static string Timestamp(long unixMs, int seq)
        {
            if (unixMs < 0)
            {
                unixMs = 0;
            }

            long sec = unixMs / 1000;
            int milli = (int)(unixMs % 1000);
            int tail = seq % 1000000;
            if (tail < 0)
            {
                tail += 1000000;
            }

            return sec.ToString(CultureInfo.InvariantCulture)
                + milli.ToString("D3", CultureInfo.InvariantCulture)
                + tail.ToString("D6", CultureInfo.InvariantCulture);
        }

        public static string Body(IList<Entry> entries, string host, string env)
        {
            var groups = new Dictionary<string, List<Entry>>();
            var order = new List<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                string key = (entry.Level ?? "") + "|" + (entry.Resource ?? "") + "|" + (entry.Kind ?? "");
                List<Entry> group;
                if (!groups.TryGetValue(key, out group))
                {
                    group = new List<Entry>();
                    groups[key] = group;
                    order.Add(key);
                }

                group.Add(entry);
            }

            var streams = new StringBuilder();
            streams.Append("{\"streams\":[");
            for (int g = 0; g < order.Count; g++)
            {
                if (g > 0)
                {
                    streams.Append(',');
                }

                List<Entry> group = groups[order[g]];
                Entry first = group[0];
                streams.Append("{\"stream\":{\"job\":\"ss-game\"");
                AppendLabel(streams, "host", host);
                AppendLabel(streams, "env", env);
                AppendLabel(streams, "level", first.Level);
                AppendLabel(streams, "resource", first.Resource);
                AppendLabel(streams, "kind", first.Kind);
                streams.Append("},\"values\":[");
                for (int i = 0; i < group.Count; i++)
                {
                    if (i > 0)
                    {
                        streams.Append(',');
                    }

                    Entry entry = group[i];
                    streams.Append('[').Append(Quote(entry.Ns)).Append(',').Append(Quote(entry.Line));
                    if (entry.Meta != null && entry.Meta.Count > 0)
                    {
                        streams.Append(",{");
                        bool any = false;
                        foreach (var pair in entry.Meta)
                        {
                            if (pair.Key == null || pair.Value == null)
                            {
                                continue;
                            }

                            if (any)
                            {
                                streams.Append(',');
                            }

                            any = true;
                            streams.Append(Quote(pair.Key)).Append(':').Append(Quote(pair.Value));
                        }

                        streams.Append('}');
                    }

                    streams.Append(']');
                }

                streams.Append("]}");
            }

            streams.Append("]}");
            return streams.ToString();
        }

        public static Dictionary<string, string> Headers(string bearer, string user, string pass, string tenant)
        {
            var headers = new Dictionary<string, string>();
            headers["Content-Type"] = "application/json";
            if (!string.IsNullOrEmpty(tenant))
            {
                headers["X-Scope-OrgID"] = tenant;
            }

            if (!string.IsNullOrEmpty(bearer))
            {
                headers["Authorization"] = "Bearer " + bearer;
            }
            else if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
            {
                string raw = user + ":" + pass;
                headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
            }

            return headers;
        }

        // Never returns the secret. The user name is the same label MTA prints at start.
        public static string DescribeAuth(string bearer, string user, string pass, string tenant)
        {
            string auth = "no auth";
            if (!string.IsNullOrEmpty(bearer))
            {
                auth = "bearer token";
            }
            else if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
            {
                auth = "basic auth as " + user;
            }

            if (!string.IsNullOrEmpty(tenant))
            {
                auth = auth + ", tenant " + tenant;
            }

            return auth;
        }

        // ok, drop (this batch is bad), config (keep and back off), or retry.
        public static string Classify(int status)
        {
            if (status >= 200 && status < 300)
            {
                return "ok";
            }

            if (status == 400 || status == 413)
            {
                return "drop";
            }

            if (status == 401 || status == 403 || status == 404)
            {
                return "config";
            }

            return "retry";
        }

        public static int NextBackoff(int previous)
        {
            if (previous <= 0)
            {
                return BackoffStartS;
            }

            long next = (long)previous * 2;
            if (next > BackoffMaxS)
            {
                return BackoffMaxS;
            }

            return (int)next;
        }

        public static string Quote(string value)
        {
            if (value == null)
            {
                value = "";
            }

            var parts = new StringBuilder(value.Length + 2);
            parts.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"':
                        parts.Append("\\\"");
                        break;
                    case '\\':
                        parts.Append("\\\\");
                        break;
                    case '\n':
                        parts.Append("\\n");
                        break;
                    case '\r':
                        parts.Append("\\r");
                        break;
                    case '\t':
                        parts.Append("\\t");
                        break;
                    default:
                        if (c < 0x20)
                        {
                            parts.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            parts.Append(c);
                        }

                        break;
                }
            }

            parts.Append('"');
            return parts.ToString();
        }

        private static void AppendLabel(StringBuilder parts, string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            parts.Append(",\"").Append(key).Append("\":").Append(Quote(value));
        }
    }
}
