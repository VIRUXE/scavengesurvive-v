# Agents

Read this file at the start of every session on this repo.

## Mandatory

A finished slice updates the commit, the docs, and the GitHub issue together, in the same turn, then push. This is mandatory. A slice left with only code, or only a comment on a stale issue title or body, is not done.

The steps are `docs/process.md`. Decision 23 in `docs/superpowers/specs/2026-11-06-scavengesurvive-fivem-port-design.md` records the rule.

## Also

- Behaviour spec: `docs/superpowers/specs/2026-11-06-scavengesurvive-fivem-port-design.md`. Issue numbers: `docs/superpowers/tracker-manifest.json`.
- Commit messages: `.cursorrules`.
- SQL is `MySql.*` in `resources/[scavengesurvive]/lib/Server/MySql.cs`. There is no `Db.Call`.
- Secrets stay in gitignored `env.cfg`. Do not commit or paste them.
- Do not restart FXServer. Reload the one resource that changed with qrcon: `qrcon 127.0.0.1:30120 "$RCON" -c "restart <resource>"`. `$RCON` is `rcon_password` in `env.cfg`. Do not print it.
- That restart is only that resource. Do not restart a category, a peer, or the process. A resource must come back on its own and must not start or stop another resource. No `fxmanifest` `dependency` that couples them. Start order stays the `ensure` lines.
