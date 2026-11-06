# Notices

New code in this repository is under the Unlicense. See `LICENSE`.

## Ported catalogues (MPL-2.0)

These files are derived from [Southclaws/ScavengeSurvive](https://github.com/Southclaws/ScavengeSurvive) commit `399ba8e8fd164243b32fb694f4bb6ce37fe40c35`, Copyright (C) Barnaby "Southclaws" Keene, Mozilla Public License 2.0. The MTA port compiled that data into `data/generated/`. This tree compiled those files into C#. The license text is `LICENSES/MPL-2.0.txt`.

- `resources/[scavengesurvive]/data/Shared/ItemData.Generated.cs`
- `resources/[scavengesurvive]/data/Shared/LootData.Generated.cs`
- `resources/[scavengesurvive]/data/Shared/LangData.Generated.cs`
- `sql/seed/0003_settings.sql`

Each file carries the MPL notice and the derivation line. SA model ids, attach offsets, wear offsets, and defence geometry were not copied. Upstream asks that credits, including in-game credit messages, are kept. Those credit strings stay in the language catalogue.

A later file compiled from the same MTA catalogues, including the recipes on F108, uses this same notice.

`sql/seed/0003_settings.sql` is the MTA settings list (F014). It has no coordinates. `loot.count_carried` is the ADR 0001 default, which that MTA seed omitted. `auth.license_autologin` is the FiveM switch added in F018. Default `1`. `player.auto_ip_login` stays `0` and is not read.

`Scarcity.cs` holds the ADR 0001 defaults. That file is new code under the Unlicense.

## Maps

San Andreas map placements stay in the MTA repository. This tree ships no CC BY-SA 4.0 license file.

The ymap resources under `resources/[maps]/` are third-party Los Santos interiors. They keep the license they shipped with. They are outside this Unlicense grant, and they are outside the upstream map grant.

## Other third-party resources

`oxmysql`, `ox_lib`, and vMenu keep their own licenses. They are outside this Unlicense grant.

Secrets live in gitignored `env.cfg`.
