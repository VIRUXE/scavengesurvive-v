# Scavenge & Survive → FiveM — tracker

Tracks the port on private `VIRUXE/scavengesurvive-v`. Gameplay resources are C#. Behaviour comes from the MTA port.

The gamemode resources are C# and live in `resources/[scavengesurvive]/`. There is no side folder for them. The Lua `world` resource was moved to `spike-lua/world` so the resource name `world` is the C# one. `resources/world/client/scenarios.lua` from that tree is the scenario list brought over from the 2023 repo. The empty `popcycle.dat` from that repo was not brought over.

Design: `docs/superpowers/specs/2026-11-06-scavengesurvive-fivem-port-design.md`

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

A GitHub Project board is not required. Milestones, labels, and the epics are the tracker. If a Project is created later, group the board by milestone. The API cannot set a view's group-by.
