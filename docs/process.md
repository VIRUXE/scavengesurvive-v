# Working process

This is how every session works on `VIRUXE/scavengesurvive-v`. The owner set it on 2026-11-06.

The repo is public. GitHub is the tracker and the record. The design doc stays the behaviour spec. This file is the way of working. `AGENTS.md` points every session here.

## Before changing code

1. Read `docs/superpowers/specs/2026-11-06-scavengesurvive-fivem-port-design.md` for the decision you are about to touch.
2. Read the open issue for the next matrix row. Issue numbers are in `docs/superpowers/tracker-manifest.json`. Milestones M0 through M8 are the order. Do not skip ahead of an open p0 in the current milestone.
3. Comment on that issue that the work has started, and name the row (`F002`, and so on).

## Mandatory close of every slice

This is mandatory for this project. A slice is not finished until all three exist, in the same turn:

1. **Commit** on `main`, then push. A slice is one row, or one reviewable part of a row. Do not hold a finished slice until the end of the session. Commit messages follow `.cursorrules`: `type(subject): description`. Mention the row id in the body (`F002`).
2. **Docs.** If the slice changes a behaviour, a decision, or the way of working, edit the design doc or this file in that same commit. Leave the old wording only when it is a dated historical fact. A code change that the docs still describe the old way is not done.
3. **Issues.** Update the GitHub issue in that same turn. Edit the title and body when they still describe the old plan. Comment with the commit hash and what landed. Close the issue only when its exit is met, and say what landed. Leave it open when the exit is not met, and say what is still missing. A comment that leaves a stale title or body is not enough.

Do not commit code and leave the issue or the docs for later. Do not edit an issue or a doc for a slice that is not committed.

A `.cs` change ships with the rebuilt `client.net.dll` and `server.net.dll` beside `fxmanifest.lua`, in that same commit.

## Resource reloads

Do not restart the FXServer process. A change to one resource is one qrcon command, `restart <resource>`, to `127.0.0.1:30120`. The password is `rcon_password` in gitignored `env.cfg`. Do not print it.

Peers stay up. Do not restart a category or a second resource because the first one changed. A resource must start again on its own and must not start or stop another resource. Do not add an `fxmanifest` `dependency` that stops or restarts a peer. Start order stays the `ensure` lines in `server.cfg` and `resources.cfg`.

A convar the process reads only at startup is not applied by a resource restart. Leave the process up unless the owner asks for a process restart.

## What stays out of git

`env.cfg` holds `mysql_connection_string`, `sv_licenseKey`, and `discord_bot_token`. It is gitignored. Do not commit those values, and do not paste them into issues, docs, or commit messages.

Wiki and GitHub Pages stay off. Docs live in this repo. Secret scanning and push protection stay on.

## Order of the port

The PR plan in the design doc is the order. PR 1 and PR 2 are done (design doc, labels, milestones, epics, one issue per row). The C# resource skeleton is on `main`. Database access is `MySql.*` in `lib/Server/MySql.cs` (the C# oxmysql import). There is no `Db.Call`. F005 is in `sql/schema.sql`: the column delta on the live MariaDB database `ssv`, including identifiers, tokens, `ban_identifiers`, and `connection_identifiers`. Procedures are not in that dump. F006 compiled the MTA item types, loot weights, both language families, and the ADR 0001 scarcity keys into `data` as C# types. F007 added `Prop` and `WeaponHash` as uint joaats, both 0. An unmapped prop resolves to `prop_box_wood05a` (joaat `2781083456`), not SA model 331. Weapon hashes stay empty until F081. F008 set the `server.cfg` block: OneSync infinity, population off, `sv_entityLockdown strict`, `setr sv_stateBagStrictMode true`, `sv_filterRequestControl 4`, build 3570, `sv_maxclients 200`, and `sv_endpointPrivacy true`. F009 is the population belt in `world`: that convar, population off on routing buckets 0 and 1, the client density loop, and `entityCreating` cancel for population types 1 through 5. The tutorial bucket is 1. The next matrix row is F010, the `logs` resource. Recipes stay on F108. The settings SQL seed stays on F014. `F002` and `F012` are still open in PR 4. `F003` is oxmysql, which is already ensured, and stays open until a gameplay write goes through the import on the finished schema.
