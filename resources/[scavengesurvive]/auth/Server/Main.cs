using System;
using System.Text;
using CitizenFX.Core;

namespace Server
{
    public class Main : ServerScript
    {
        public Main()
        {
            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(OnConnecting);
            EventHandlers["playerJoining"] += new Action<Player, string>(OnJoining);
            EventHandlers["playerDropped"] += new Action<Player, string>(OnDropped);
            Debug.WriteLine("auth server started.");
        }

        void OnConnecting([FromSource] Player player, string name, dynamic setKickReason, dynamic deferrals)
        {
            deferrals.defer();
            string source = player.Handle;
            Sessions.Begin(source, name ?? "");
            deferrals.update("Checking your account status...");
            Present(deferrals, null, null, null);
        }

        void OnJoining([FromSource] Player player, string oldSource)
        {
            if (string.IsNullOrEmpty(oldSource)) return;
            Sessions.Remap(oldSource, player.Handle);
        }

        void OnDropped([FromSource] Player player, string reason)
        {
            Sessions.End(player.Handle);
        }

        static void Present(dynamic deferrals, string message, string username, string password)
        {
            deferrals.presentCard(Card(message, username, password), new Action<dynamic, string>((data, raw) =>
            {
                if (data == null)
                {
                    deferrals.done("Connection closed.");
                    return;
                }

                string action = Convert.ToString(data.action);
                if (action != "login" && action != "register")
                {
                    deferrals.done("Connection closed.");
                    return;
                }

                string nextUser = Convert.ToString(data.username);
                string nextPass = Convert.ToString(data.password);
                if (string.IsNullOrEmpty(nextUser) || string.IsNullOrEmpty(nextPass))
                {
                    Present(deferrals, "Username and password cannot be empty.", nextUser, nextPass);
                    return;
                }

                // The account row is a later procedure. Nothing is stored, and the password is not logged.
                deferrals.done("Accounts are not open yet.");
            }));
        }

        static string Card(string message, string username, string password)
        {
            var body = new StringBuilder();
            body.Append("{\"type\":\"AdaptiveCard\",\"version\":\"1.3\",\"body\":[");
            body.Append("{\"type\":\"TextBlock\",\"text\":\"Scavenge and Survive\",\"weight\":\"Bolder\",\"size\":\"Large\"},");
            body.Append("{\"type\":\"TextBlock\",\"wrap\":true,\"text\":\"Welcome to a world where survival is the only thing that matters. Scavenge for resources, build your shelter, and fight for your life in a post-apocalyptic world.\"},");
            body.Append("{\"type\":\"TextBlock\",\"wrap\":true");
            if (!string.IsNullOrEmpty(message)) body.Append(",\"color\":\"Attention\"");
            body.Append(",\"text\":").Append(Json(message ?? "Please login or register to continue.")).Append("},");
            body.Append("{\"type\":\"Input.Text\",\"id\":\"username\",\"label\":\"Username\",\"placeholder\":\"Enter your username\",\"value\":").Append(Json(username ?? "")).Append("},");
            body.Append("{\"type\":\"Input.Text\",\"id\":\"password\",\"label\":\"Password\",\"placeholder\":\"Enter your password\",\"style\":\"password\",\"value\":").Append(Json(password ?? "")).Append("}");
            body.Append("],\"actions\":[");
            body.Append("{\"type\":\"Action.Submit\",\"title\":\"Login\",\"data\":{\"action\":\"login\"}},");
            body.Append("{\"type\":\"Action.Submit\",\"title\":\"Register\",\"data\":{\"action\":\"register\"}}");
            body.Append("]}");
            return body.ToString();
        }

        static string Json(string value)
        {
            if (value == null) return "\"\"";
            var escaped = new StringBuilder(value.Length + 2);
            escaped.Append('"');
            foreach (char c in value)
            {
                if (c == '\\' || c == '"') escaped.Append('\\').Append(c);
                else if (c == '\n') escaped.Append("\\n");
                else if (c == '\r') escaped.Append("\\r");
                else if (c == '\t') escaped.Append("\\t");
                else escaped.Append(c);
            }
            escaped.Append('"');
            return escaped.ToString();
        }
    }
}
