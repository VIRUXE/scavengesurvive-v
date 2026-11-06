# Scavenge & Survive → FiveM — tracker

Tracks the port on public `VIRUXE/scavengesurvive-v`. Gameplay resources are C#. Behaviour comes from the MTA port. Every finished slice updates the commit, the docs, and the issues together. That rule is mandatory. The steps are `docs/process.md`.

The gamemode resources are C# and live in `resources/[scavengesurvive]/`. They are started from `resources.cfg`. The project Lua (`player`, `game`, `vehicle`, `admin`, and the Lua `world`) has been removed after its behaviour was moved into `auth`, `character`, `data`, `staff`, `vehicles`, and `world`. Vendor resources (`oxmysql`, `ox_lib`, `[maps]`, vMenu) stay. The Discord bot stays the node resource.

Design: `docs/superpowers/specs/2026-11-06-scavengesurvive-fivem-port-design.md`

Conventions: `docs/conventions.md`

Issue numbers for the matrix rows are in `docs/superpowers/tracker-manifest.json`.

## Fields

| Field | Values | Source of truth |
| --- | --- | --- |
| Status | Open / Closed | built-in |
| Phase | M0 … M8 | issue milestone |
| Area | auth, character, items, … infra | `area/*` label |
| Port class | port / replace / drop / defer | `port/*` label |
| Priority | p0 … p3 | `priority/*` label |

Size labels exist so a later pass can mark XS–XL. This filing does not guess a size.

## Epics

One epic per milestone, plus a Dropped epic for F134–F150. F151 is under M1. F152–F158 are under M7. Issues #1, #3, #4, and #5 stay open and are labelled onto auth, character, and building.

Milestones, labels, and the epics are the tracker. Wiki and GitHub Pages stay off. The session process is `docs/process.md`.
