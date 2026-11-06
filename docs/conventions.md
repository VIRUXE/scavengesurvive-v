# Conventions

Working rules for the FiveM port of Scavenge & Survive. F013. The behaviour spec is `docs/superpowers/specs/2026-11-06-scavengesurvive-fivem-port-design.md`. The session steps are `docs/process.md`. This file is the FiveM stand-in for MTA `CLAUDE.md`.

Gameplay resources are C# under `resources/[scavengesurvive]/<name>/`. There is no Lua gameplay and no `shared/events.lua`. `dev` is not ensured.

## Events

Names are `<subject><PastTenseVerb>` camelCase. Examples already in the spec: `itemCreated`, `itemPickupRequested`, `inventoryOpened`, `playerAuthenticated`, `loginRequested`. The name does not carry `on`, `onClient`, or a `Request` prefix. Direction is which API fires the event.

- Server-local: `TriggerEvent("itemCreated", itemId)`.
- Client to server: `TriggerServerEvent`. The handler takes `[FromSource] Player source`. That player is the only trusted identity. A character id in the payload is checked against the session `auth` stored for that player's server id.
- Server to one client: `player.TriggerEvent`. Server to everyone: `TriggerClientEvent`. C# does not use a target of `-1`. A payload of more than a few kilobytes uses `TriggerLatentClientEvent` with an explicit bps budget. State bags are not a bulk channel.
- Handlers: `[EventHandler("itemPickupRequested")]` or `EventHandlers["itemPickupRequested"] += ...`.

The C# method is the declaration. Each resource does not keep a shared event list file.

## Ids, not handles

A call or event that crosses a resource passes numeric ids and plain values. Account id, character id, item id, and vehicle row id are the ids. Do not pass a `Ped`, a `Vehicle`, or any entity handle to another resource.

Exports are the cross-resource call. A defining server script registers with `Exports.Add`. A caller uses `Exports["myresource"].getWidget()` and checks `GetResourceState(name) == "started"` first. A stopped resource's export is not invoked.

`auth` will export `getAccountId` and `getAdminLevel`. `character` will export `getCharacterId`. Those exports are not present yet. Until they are, another resource does not reach into `auth`'s session table.

Inside one resource, a file-local table is fine. `auth` keeps `Sessions` in `auth/Server/Session.cs`, keyed by the player's server id, and does not publish that dictionary. New code does the same: state stays in the resource that owns it, and other resources ask for an id. Do not add a public static session class, a process-wide `Character.byPlayer`, or any other global that a second resource reads.

## Schema

MariaDB 13.1.1+, database `ssv`, utf8mb4. The connection string stays in gitignored `env.cfg`.

SQL in gameplay code is `MySql.Query`, `Single`, `Scalar`, `Update`, `Insert`, `Prepare`, `Raw`, or `Transaction` in `resources/[scavengesurvive]/lib/Server/MySql.cs`. Each server project compiles that file. There is no `Db.Call`, no C# MySQL driver, and no `CALL` gate. Placeholders only.

Change the live database, then dump to `sql/schema.sql` and commit that dump. Do not hand-edit `sql/schema.sql`. There are no migration files. One-off data fixes run on the live database and do not get a file.

`SET SESSION system_versioning_alter_history = KEEP` in the same session before any `ALTER` on `accounts`, `items`, or `bans`. MariaDB raises 4119 otherwise.

Procedures are not in the current dump. Add a procedure in the row that first needs it, on the live database, then dump. Do not load an MTA procedure whose body still mentions `interior`, `dimension`, `serial`, or a signed `vehicles.model`. `bucket` replaces `dimension`. Zone columns are `varchar(16)`.

The settings seed is `sql/seed/0003_settings.sql`. It is edited by hand and loaded into `settings`. Item types are the C# catalogue, not a `0001` file. Loot tables are C# data in `data`. San Andreas coordinate seeds are not imported. The structure dump does not contain seed rows.

On the oxmysql path that F011 measured: a SQL `NULL` is CLR null, a `DECIMAL` comes back as a string, and a `FLOAT` comes back as a double. Read a `POINT` with `ST_X` and `ST_Y`. Do not `SELECT` the raw point.

## Auth capture

`auth` reads identifiers once, on `playerConnecting`, after `deferrals.defer` and one tick. The types are `license`, `license2`, `discord`, `fivem`, `steam`, `xbl`, `live`, and `ip`. The value is the part after the first `:`. A missing `license` refuses the connection. Tokens are read once into the session. They are not logged and not written to a state bag. `playerJoining` remaps the same session. `session.connected` is the log line that carries the identifier values. `session.refused` with `code` `no_license` is the refusal. Once an account id exists, `auth` writes `account_identifiers` and `account_tokens` in one transaction. A `(type, value)` owned by another account stays there. The same token may sit on two accounts. `session.recorded` carries the counts. Token strings stay out of the log. Register and login are the deferral Adaptive Card: name plus password, no email, no recovery card. The name is 1 to 22 letters, digits, or punctuation. The password is 4 to 32 bytes. The hash is bcrypt cost 10, `$2b$`, 60 characters, in `accounts.password_hash`. A wrong password counts toward 5 tries in 10 minutes, keyed by `license`. The fifth closes the deferral. An inactive account is refused. `auth.getAccountId` and `auth.getAdminLevel` return the session after the identifier write. Passwords and hashes are not logged. When `settings` `auth.license_autologin` is on (a missing row is on), one `license` row resumes that account and logs `session.authenticated` with `method` `license`. `0` shows the card. A new license shows the card. `license2` and `last_ip` are not a login. `player.auto_ip_login` is not read. An inactive account is refused before the card. Before that resume, the deferral matches the captured identifiers and tokens against `ban_identifiers`. A hit logs `session.banned` and closes the deferral. Token strings stay out of the log. `staff_ban` is statements in `auth`: one `bans` row, the `ban_identifiers` set, and one `staff_actions` row. There is no procedure and no `CALL`. An active ban on that name returns `ALREADY_BANNED`.

## Logs

`logs` exports `event(kind, fields, level)` and writes one stdout JSON line (`level`, `resource`, `kind`, then flat snake_case fields). Kind is `<domain>.<pastTenseVerb>`. A change a player can dispute logs one event with the ids. A refusal logs `ok = false` and the `code`. Nothing per frame. A batch logs a count.

Password, keypad, token, bearer, and bcrypt values are dropped. Identifier values stay on `session.connected` and `session.banned`. Player server ids become names. `account_id` and `character_id` are filled when `auth.getAccountId` and `character.getCharacterId` exist.

`logs` is the only resource that performs HTTP requests. Loki runs only when `logs_url` is set.

## Text

Player-facing item text is English in the catalogues and translated through `data`. `Lang.ItemType`, `Lang.Text`, and `Lang.TextFor` are the calls. Both families are compiled C#: ten `lang_*` tables including English, and nine `strings_*` tables. There is no `strings_en`. English is the key. Do not regenerate `strings_*` from `tools/convert`.

## Repo

Commit on `main`. A `.cs` change ships the rebuilt `client.net.dll` and `server.net.dll` beside `fxmanifest.lua` in that same commit. Commit messages are `type(subject): description` (`.cursorrules`).

A resource restarts alone: qrcon `restart <resource>` on `127.0.0.1:30120`. The password is `rcon_password` in `env.cfg`. Do not print it. Do not restart FXServer for a resource change. Start order is the `ensure` lines. No `fxmanifest` `dependency` that stops or restarts a peer.

There is no test suite. Live checks are the record.

New code is the Unlicense (`LICENSE`). The generated catalogues are MPL-2.0. Secrets (`mysql_connection_string`, `sv_licenseKey`, `discord_bot_token`, `rcon_password`) stay in `env.cfg`.

## Editors

`fivem-mcp` 0.2.0 is the project FiveM MCP. Install it with `npm install -g fivem-mcp@0.2.0` so `fivem-mcp` is on `PATH`. `.mcp.json` is the Claude server. `.grok/config.toml` is the Grok server. `.codex/config.toml` is the Codex server. All three run `tools/fivem-mcp.sh`. The published bin is a symlink, and the server starts only when Node is given the real `index.mjs`, so the wrapper does that. The server is docs, natives, a Lua audit, and stack detection. It does not send console commands.

`qbx-lint` and `qbx-lua-ls` 1.1.0 are the FiveM Lua tools. They are not in this repo. Both must be on `PATH`. Claude loads `qbx-lua-ls` for `.lua` from `.claude/marketplace` (`qbx-lua@local-fivem`). Codex on this host has no language-server slot, so it uses the same binaries from the shell.
