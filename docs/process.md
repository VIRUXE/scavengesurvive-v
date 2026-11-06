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

## What stays out of git

`env.cfg` holds `mysql_connection_string`, `sv_licenseKey`, and `discord_bot_token`. It is gitignored. Do not commit those values, and do not paste them into issues, docs, or commit messages.

Wiki and GitHub Pages stay off. Docs live in this repo. Secret scanning and push protection stay on.

## Order of the port

The PR plan in the design doc is the order. PR 1 and PR 2 are done (design doc, labels, milestones, epics, one issue per row). The C# resource skeleton is on `main`. Database access is `MySql.*` in `lib/Server/MySql.cs` (the C# oxmysql import). There is no `Db.Call`. F004 is in `sql/schema.sql`: the unchanged tables, triggers, and events, plus the MTA-shaped parents those objects require. The next row in that block is F005, the schema delta. `F002` and `F012` are still open in PR 4. `F003` is oxmysql, which is already ensured, and stays open until a gameplay write goes through the import on the finished schema.
