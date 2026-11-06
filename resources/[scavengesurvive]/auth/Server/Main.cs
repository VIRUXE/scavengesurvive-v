using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Server
{
    public class Main : ServerScript
    {
        static readonly Regex Email = new Regex(@"^[\w_.-]+@[\w_.-]+\.[\w]{2,6}$", RegexOptions.Compiled);

        public Main()
        {
            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(OnConnecting);
            EventHandlers["playerJoining"] += new Action<Player, string>(OnJoining);
            EventHandlers["playerDropped"] += new Action<Player, string>(OnDropped);
            EventHandlers["player:spawned"] += new Action<Player>(OnSpawned);
            API.RegisterCommand("authcapture", new Action<int, List<object>, string>(OnCapture), true);
            Debug.WriteLine("auth server started. capture on playerConnecting.");
        }

        // FiveM requires one tick after defer before update, presentCard, or done.
        // Identifiers and tokens are read on that tick, once. playerJoining only remaps.
        async void OnConnecting([FromSource] Player player, string name, dynamic setKickReason, dynamic deferrals)
        {
            string source = player.Handle;
            string playerName = name ?? "";
            deferrals.defer();
            try
            {
                await Delay(0);
                Session session = Sessions.Begin(source, playerName);
                if (!Identifier.HasLicense(session.Identifiers))
                {
                    var refused = new Dictionary<string, object>();
                    refused["player"] = source;
                    refused["name"] = playerName;
                    refused["ok"] = false;
                    refused["code"] = "no_license";
                    refused["identifier_count"] = session.Identifiers.Count;
                    refused["token_count"] = session.Tokens.Length;
                    Note("session.refused", refused);
                    Sessions.End(source);
                    deferrals.done("A Rockstar license is required to join.");
                    return;
                }

                Note("session.connected", ConnectedFields(session));
                deferrals.update("Checking your account status...");
                await Delay(0);
                Present(deferrals, source, null, null, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[auth] connect failed: " + ex.GetType().Name);
                Sessions.End(source);
                try
                {
                    deferrals.done("Connection closed.");
                }
                catch (Exception)
                {
                }
            }
        }

        void OnCapture(int source, List<object> args, string raw)
        {
            if (args != null && args.Count > 0)
            {
                string id = Convert.ToString(args[0]);
                try
                {
                    string[] tokens;
                    Dictionary<string, string> identifiers = Identifier.Read(id, out tokens);
                    Debug.WriteLine("[auth] capture player " + Identifier.Summary(identifiers, tokens));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[auth] capture player failed: " + ex.GetType().Name);
                }
                return;
            }

            Dictionary<string, string> sample = Identifier.Take(new string[]
            {
                "license:first",
                "license:second",
                "license2:l2",
                "discord:d",
                "fivem:f",
                "steam:s",
                "xbl:x",
                "live:v",
                "ip:203.0.113.8",
                "notatype:no",
                "license:",
                ":bad"
            });
            string[] sampleTokens = Identifier.TakeTokens(new string[] { "tok-a", "tok-a", "", "tok-b" });
            string license;
            sample.TryGetValue("license", out license);
            Debug.WriteLine("[auth] capture sample " + Identifier.Summary(sample, sampleTokens)
                + " license_value=" + (license ?? ""));
        }

        static Dictionary<string, object> ConnectedFields(Session session)
        {
            var fields = new Dictionary<string, object>();
            fields["player"] = session.Source;
            fields["name"] = session.Name ?? "";
            fields["ok"] = true;
            for (int i = 0; i < Identifier.Types.Length; i++)
            {
                string type = Identifier.Types[i];
                string value;
                if (session.Identifiers.TryGetValue(type, out value) && !string.IsNullOrEmpty(value))
                    fields[type] = value;
            }

            fields["token_count"] = session.Tokens.Length;
            return fields;
        }

        void Note(string kind, Dictionary<string, object> fields)
        {
            Debug.WriteLine("[auth] " + kind + " token_count=" + Field(fields, "token_count")
                + " code=" + Field(fields, "code"));
            if (API.GetResourceState("logs") != "started")
                return;

            try
            {
                dynamic logs = Exports["logs"];
                logs.@event(kind, fields, "info");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[auth] logs export failed: " + ex.GetType().Name);
            }
        }

        static string Field(Dictionary<string, object> fields, string key)
        {
            object value;
            if (fields == null || !fields.TryGetValue(key, out value) || value == null)
                return "";
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        void OnJoining([FromSource] Player player, string oldSource)
        {
            if (!string.IsNullOrEmpty(oldSource))
                Sessions.Remap(oldSource, player.Handle);

            Session session = Sessions.Get(player.Handle);
            if (session == null || !session.HasAccount) return;

            player.State.Set("username", session.Username, true);
            TriggerEvent("playerAuthenticated", player.Handle);
            Debug.WriteLine("(" + player.Handle + ") " + session.Name + " [" + session.Username + "] has logged in.");
        }

        void OnSpawned([FromSource] Player player)
        {
            Session session = Sessions.Get(player.Handle);
            if (session != null) session.Spawned = true;
        }

        void OnDropped([FromSource] Player player, string reason)
        {
            string source = player.Handle;
            Session session = Sessions.Get(source);
            if (session != null && session.HasAccount && session.Spawned)
            {
                int ped = API.GetPlayerPed(source);
                Vector3 coords = API.GetEntityCoords(ped);
                string json = string.Format(CultureInfo.InvariantCulture, "{{\"x\":{0},\"y\":{1},\"z\":{2}}}", coords.X, coords.Y, coords.Z);
                MySql.Update(
                    "UPDATE accounts SET last_position = ? WHERE id = ?",
                    new object[] { json, session.AccountId },
                    new Action<dynamic>(unused => { }));
            }
            if (session != null)
                Debug.WriteLine("(" + source + ") " + session.Name + " dropped");
            Sessions.End(source);
        }

        void Present(dynamic deferrals, string source, string message, string username, string password)
        {
            deferrals.presentCard(Card(message, username, password), new Action<dynamic, string>((data, raw) =>
            {
                if (data == null)
                {
                    deferrals.done("Connection closed.");
                    return;
                }

                string action = Convert.ToString(data.action);
                string nextUser = Convert.ToString(data.username);
                string nextPass = Convert.ToString(data.password);

                if (action == "recover")
                {
                    PresentRecovery(deferrals, source, null);
                    return;
                }

                if (action != "login" && action != "register")
                {
                    deferrals.done("Connection closed.");
                    return;
                }

                if (string.IsNullOrEmpty(nextUser) || string.IsNullOrEmpty(nextPass))
                {
                    Present(deferrals, source, "Username and password cannot be empty.", nextUser, nextPass);
                    return;
                }

                if (action == "login")
                    Login(deferrals, source, nextUser, nextPass);
                else
                    BeginRegister(deferrals, source, nextUser, nextPass);
            }));
        }

        void Login(dynamic deferrals, string source, string username, string password)
        {
            MySql.Single(
                "SELECT id, username, email, last_position FROM accounts WHERE username = ?",
                new object[] { username },
                new Action<dynamic>(row =>
                {
                    if (row == null)
                    {
                        Present(deferrals, source, "Account not found. Please try again or register.", username, password);
                        return;
                    }

                    int id = MySql.Int(row, "id");
                    MySql.Scalar(
                        "SELECT IF(SHA2(?, 256) = password, 1, 0) FROM accounts WHERE id = ?",
                        new object[] { password, id },
                        new Action<dynamic>(match =>
                        {
                            if (Convert.ToInt32(match) != 1)
                            {
                                Present(deferrals, source, "Incorrect password.", username, password);
                                return;
                            }

                            Accept(deferrals, source, id, MySql.Text(row, "username"), MySql.Text(row, "email"), MySql.Text(row, "last_position"));
                        }));
                }));
        }

        void BeginRegister(dynamic deferrals, string source, string username, string password)
        {
            MySql.Scalar(
                "SELECT id FROM accounts WHERE username = ?",
                new object[] { username },
                new Action<dynamic>(existing =>
                {
                    if (existing != null && Convert.ToString(existing) != "")
                    {
                        Present(deferrals, source, "This username is already registered. Please login or register with a different username.", username, password);
                        return;
                    }

                    deferrals.presentCard(RegisterCard(), new Action<dynamic, string>((data, raw) =>
                    {
                        if (data == null)
                        {
                            deferrals.done("Connection closed.");
                            return;
                        }

                        string email = Convert.ToString(data.email);
                        if (string.IsNullOrEmpty(email))
                        {
                            Present(deferrals, source, "E-mail cannot be empty.", username, password);
                            return;
                        }

                        MySql.Insert(
                            "INSERT INTO accounts (username, password, email) VALUES (?, SHA2(?, 256), ?)",
                            new object[] { username, password, email },
                            new Action<dynamic>(accountId =>
                            {
                                if (accountId == null || Convert.ToString(accountId) == "")
                                {
                                    Present(deferrals, source, "An error occurred during registration. The username might be taken.", username, password);
                                    return;
                                }

                                Accept(deferrals, source, Convert.ToInt32(accountId), username, email, null);
                            }));
                    }));
                }));
        }

        void PresentRecovery(dynamic deferrals, string source, string message)
        {
            deferrals.presentCard(RecoveryCard(message), new Action<dynamic, string>((data, raw) =>
            {
                if (data == null)
                {
                    deferrals.done("Connection closed.");
                    return;
                }

                string action = Convert.ToString(data.action);
                if (action == "back_to_welcome")
                {
                    Present(deferrals, source, null, null, null);
                    return;
                }

                string email = Convert.ToString(data.email);
                if (string.IsNullOrEmpty(email))
                {
                    PresentRecovery(deferrals, source, "E-mail address cannot be blank.");
                    return;
                }

                if (!Email.IsMatch(email))
                {
                    PresentRecovery(deferrals, source, "Please enter a valid e-mail address.");
                    return;
                }

                deferrals.presentCard(
                    "{\"type\":\"AdaptiveCard\",\"version\":\"1.3\",\"body\":[" +
                    "{\"type\":\"TextBlock\",\"text\":\"Password Recovery\",\"weight\":\"Bolder\",\"size\":\"Medium\"}," +
                    "{\"type\":\"TextBlock\",\"wrap\":true,\"text\":\"If an account with that email exists, a password recovery link has been sent.\"}" +
                    "],\"actions\":[{\"type\":\"Action.Submit\",\"title\":\"Close\"}]}",
                    new Action<dynamic, string>((done, rawDone) => deferrals.done("Connection closed.")));
            }));
        }

        void Accept(dynamic deferrals, string source, int id, string username, string email, string lastPosition)
        {
            Session session = Sessions.Get(source);
            if (session == null)
            {
                deferrals.done("Connection closed.");
                return;
            }

            session.HasAccount = true;
            session.AccountId = id;
            session.Username = username;
            session.Email = email;
            session.LastPosition = string.IsNullOrEmpty(lastPosition) ? null : lastPosition;

            foreach (KeyValuePair<string, string> identifier in session.Identifiers)
            {
                MySql.Update(
                    "INSERT INTO account_identifiers (account_id, type, value) VALUES (?, ?, ?) ON DUPLICATE KEY UPDATE last_used = NOW(), times_used = times_used + 1",
                    new object[] { id, identifier.Key, identifier.Value },
                    new Action<dynamic>(unused => { }));
            }

            for (int i = 0; i < session.Tokens.Length; i++)
            {
                MySql.Update(
                    "INSERT INTO account_tokens (account_id, token) VALUES (?, ?) ON DUPLICATE KEY UPDATE last_used = NOW(), times_used = times_used + 1",
                    new object[] { id, session.Tokens[i] },
                    new Action<dynamic>(unused => { }));
            }

            deferrals.done();
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
            body.Append("{\"type\":\"Action.Submit\",\"title\":\"Register\",\"data\":{\"action\":\"register\"}},");
            body.Append("{\"type\":\"Action.Submit\",\"title\":\"Forgot Password?\",\"data\":{\"action\":\"recover\"}}");
            body.Append("]}");
            return body.ToString();
        }

        static string RegisterCard()
        {
            return "{\"type\":\"AdaptiveCard\",\"version\":\"1.3\",\"body\":[" +
                "{\"type\":\"TextBlock\",\"text\":\"Registration\",\"weight\":\"Bolder\",\"size\":\"Medium\"}," +
                "{\"type\":\"Input.Text\",\"id\":\"email\",\"label\":\"E-mail\",\"placeholder\":\"Enter your e-mail\"}" +
                "],\"actions\":[{\"type\":\"Action.Submit\",\"title\":\"Complete Registration\"}]}";
        }

        static string RecoveryCard(string message)
        {
            var body = new StringBuilder();
            body.Append("{\"type\":\"AdaptiveCard\",\"version\":\"1.3\",\"body\":[");
            body.Append("{\"type\":\"TextBlock\",\"text\":\"Password Recovery\",\"weight\":\"Bolder\",\"size\":\"Medium\"},");
            if (!string.IsNullOrEmpty(message))
                body.Append("{\"type\":\"TextBlock\",\"wrap\":true,\"color\":\"Attention\",\"text\":").Append(Json(message)).Append("},");
            body.Append("{\"type\":\"Input.Text\",\"id\":\"email\",\"label\":\"E-mail\",\"placeholder\":\"Enter your e-mail address\"}");
            body.Append("],\"actions\":[");
            body.Append("{\"type\":\"Action.Submit\",\"title\":\"Submit\",\"data\":{\"action\":\"submit_recovery\"}},");
            body.Append("{\"type\":\"Action.Submit\",\"title\":\"Back\",\"data\":{\"action\":\"back_to_welcome\"}}]}");
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
