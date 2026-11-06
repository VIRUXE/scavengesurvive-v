# Working process

This is how every session works on `VIRUXE/scavengesurvive-v`. The owner set it on 2026-11-06.

The repo is public. GitHub is the tracker and the record. The design doc stays the behaviour spec. This file stays the way of working.

## Before changing code

1. Read `docs/superpowers/specs/2026-11-06-scavengesurvive-fivem-port-design.md` for the decision you are about to touch.
2. Read the open issue for the next matrix row. Issue numbers are in `docs/superpowers/tracker-manifest.json`. Milestones M0 through M8 are the order. Do not skip ahead of an open p0 in the current milestone.
3. Comment on that issue that the work has started, and name the row (`F002`, and so on).

## While changing code

1. Commit on `main` as each slice is real. A slice is one row, or one reviewable part of a row. Do not hold a finished slice until the end of the session.
2. Commit messages follow `.cursorrules`: `type(subject): description`. Mention the row id in the body (`F002`).
3. When a decision in the design doc changes, edit the doc in the same commit. Leave the old wording only when it is a dated historical fact.
4. When the row's exit is met, close the GitHub issue in the same turn and say what landed. Leave it open when the exit is not met, and say what is still missing.
5. A `.cs` change ships with the rebuilt `client.net.dll` and `server.net.dll` beside `fxmanifest.lua`.

## What stays out of git

`env.cfg` holds `mysql_connection_string`, `sv_licenseKey`, and `discord_bot_token`. It is gitignored. Do not commit those values, and do not paste them into issues, docs, or commit messages.

Wiki and GitHub Pages stay off. Docs live in this repo. Secret scanning and push protection stay on.

## Order of the port

The PR plan in the design doc is the order. PR 1 and PR 2 are done (design doc, labels, milestones, epics, one issue per row). The C# resource skeleton is on `main`. Database access is oxmysql (`query`, `single`, `scalar`, `update`, `insert`). There is no `Db.Call`. The next implementation block is still PR 4's schema and the `lib` shared types (`F002`, `F004`, `F005`, `F012`). `F003` is oxmysql, which is already ensured.
