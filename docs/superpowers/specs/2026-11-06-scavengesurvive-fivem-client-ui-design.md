# Scavenge & Survive → FiveM: client and UI

| | |
|---|---|
| Author | VIRUXE |
| Date | 2026-11-06 |
| Status | Approved (owner decisions 2026-11-06) |
| Parent | `2026-11-06-scavengesurvive-fivem-port-design.md`, decision 25 and rows F159–F180 |
| Behaviour baseline | MTA port `scavengesurvive-mta`: `ui`, `hud`, `chat`, and the client halves of `character`, `items`, `vehicles`, `building`, `staff`, `weapons`, `world`, `loot`. `docs/ui-reference-samp.md` and `docs/mvp-smoke-checklist.md` there. |

This is the client half of the port design. The parent spec is server-first and left the player-visible surface at four rows. This document records every MTA client surface, says what each becomes on FiveM, and fixes the FiveM facts the parent assumed wrongly. File references `path:line` without a prefix are under `scavengesurvive-mta/resources/[scavengesurvive]/`.

## Owner decisions (2026-11-06)

1. **The always-on HUD is C# draws.** Decision 7 of the parent spec stands. Bars, hunger, ammo, watch, key-action list, vehicle panel, dashboard, hold bar, action text, tips, low-HP darkening, countdowns, 3D labels, nametags, tracers, voice icons are `DrawRect` and the text natives from a client assembly. No browser draws the HUD.
2. **NUI pages are Svelte apps.** Two pages: `hud` (inventory, dialogs, text input, keypad, radio panel, tweak pad, staff lists, death screen) and `chat` (the chat box). They are built on the server before the resource restart. `web/dist` is not committed.
3. **A custom `chat` resource replaces the stock one.** It owns chat modes, commands, join lines, MoTD, flood mute, the Discord bridge, and voice.
4. **Voice is ported.** Mumble. Local 40 m always, plus the active mode: global is for everyone, radio is the tuned frequency, staff is the admin channel only while `/a` is active. N stays push-to-talk.
5. **Drop moves to G.** F interact, G drop/give/unload, Y put away, H inventory, R reload, P staff panel. N is voice.
6. **Event names have no prefix**, on every surface. Server events, client events, NUI message `type` values, and NUI callback names are `<subject><PastTenseVerb>`. The spike's colon-named events are removed, not kept.

## FiveM facts the parent spec got wrong or left out

| Parent assumption | FiveM fact | Row |
|---|---|---|
| "The server writes health with `SetEntityHealth` from the wound total, on a 1 s tick." | `SET_ENTITY_HEALTH` has no server RPC. `SetEntityCoords`, `SetPedArmour`, and `SetVehicleDoorsLocked` do. Health is written by the owning client from `characterHealthSynced`. | F166 |
| "`spawnmanager` and `sessionmanager` must not fight `character`." | Neither exists on this host. `system_resources` holds `chat` and `monitor` only. There is no spawn, no respawn, and no loading-screen shutdown unless `character` does them. | F164 |
| Respawn is "a new character row after the death UI". | Without a script the ped lies dead forever and the game shows its wasted shard. The owner client must cancel the shard, run the camera, and `NetworkResurrectLocalPlayer`. | F165 |
| Damage cancel covers `weaponDamageEvent` and modifiers. | GTA V also needs friendly fire on (`NetworkSetFriendlyFireOption`, `SetCanAttackFriendly`), no auto-reload, no auto-swap, no weapon drop on death, no weapon wheel, no health regen, no crits. | F162 |
| Five keys. | N is Mumble push-to-talk. Y is team text chat in GTA Online. H is headlights in a vehicle. F is vehicle entry. GTA V has no crouch. | F064, F163 |
| One `ui_page`. | Two: `hud` and `chat`. Neither holds the HUD. | F159 |
| The stock `chat` box. | Replaced. Its colon-named message event is not called. | F169 |
| vMenu dropped (F146). | Still started from `env.cfg`. | F170 |
| Nametags "from server-pushed names". | MTA never hid its default tags; the UI reference says players see none. FiveM shows none by default. Staff tags are the 3D label pass. | F172 |

## 1. Surfaces

| MTA surface (file) | FiveM surface | Row |
|---|---|---|
| Canvas, fonts, colours (`ui/shared/logic/canvas.lua`) | C# draw kit | F160 |
| Action text, toast (`ui/client/actiontext.lua`) | C# draw | F160 |
| Help tip (`ui/client/tip.lua`) | C# draw | F160 |
| Hold bar, knockout bar (`ui/client/progress.lua`) | C# draw | F160 |
| Overheat bar (`vehicles/client/overheat.lua`) | C# draw | F160 |
| Skill notice (`hud/client/main.lua:170-196`) | C# draw | F160 |
| Low-HP darkening (`hud/client/main.lua:572-574, 620-625`) | C# full-screen rect | F160 |
| Whitelist countdown (`auth/client/whitelist.lua`) | C# draw (exists, `auth/Client/Main.cs`) | F020 |
| Stock HUD show/hide (`hud/client/main.lua:27-41`) | HUD component policy | F161 |
| Hunger bar (`hud/shared/logic/bars.lua:8`) | C# rect above the health bar | F161 |
| Ammo text (`hud/client/main.lua:220-239`) | C# text under the weapon icon | F161 |
| Key-action column (`hud/shared/logic/keyactions.lua`) | C# draw | F160, F064 |
| Watch (`hud/shared/logic/watch.lua`) | C# draw | F128 |
| Vehicle panel (`hud/shared/logic/vehiclepanel.lua`) | C# draw | F177 |
| Driver dashboard (`hud/client/dashboard.lua`) | C# draw | F177 |
| Voice icons and speaker list (`hud/client/voice.lua`) | C# 3D label pass and draw | F175, F172 |
| List dialog (`ui/client/list.lua`) | `hud` Svelte page | F168 |
| Message, input, password dialogs (`ui/client/dialogbox.lua`) | `hud` Svelte page | F168 |
| Info windows, CEGUI (`ui/client/info.lua`) | `hud` Svelte page | F168 |
| Language picker (`auth/client/language.lua`) | `hud` Svelte page, list dialog | F023, F168 |
| Login and register dialogs (`auth/client/login.lua`) | Deferral Adaptive Card (parent, F017) | F017 |
| Welcome box, 10 s countdown (`auth/client/login.lua:20-36`) | `hud` Svelte page, message dialog | F024, F168 |
| Inventory list, body panel, gear tiles (`items/client/inventory.lua`) | `hud` Svelte page | F045 |
| 3D model previews on gear tiles (`items/client/preview.lua`) | Not reproduced. Captions only. | F116 defer |
| Skills list (`character/client/skills.lua`) | `hud` Svelte page | F173 |
| Recipes window (`crafting/client/recipes.lua`) | `hud` Svelte page | F173 |
| Keypad (`building/client/keypad.lua`) | `hud` Svelte page | F173 |
| Item tweak pad (`building/client/tweak.lua`) | `hud` Svelte page, arrow still C# 3D | F173, F061 |
| Radio panel (`chat/client/main.lua:131-164`) | `hud` Svelte page | F173, F076 |
| Note, sign, body, pot, machine dialogs (`items/client/text.lua`) | `hud` Svelte page | F173 |
| Death screen (`character/client/deathscreen.lua`) | `hud` Svelte page plus C# camera | F165 |
| Staff panel, lists, dialogs (`staff/client/panel.lua`, `dialogs.lua`) | `hud` Svelte page | F178 |
| Spectate banner (`staff/client/main.lua:108-119`) | C# draw | F178 |
| Staff nametags (`staff/client/main.lua:82-106`) | C# 3D label pass | F172 |
| Item labels, defence hitpoints, sign text (`items/client/world.lua:601-692`) | C# 3D label pass | F172 |
| Supply crate labels (`loot/client/main.lua:84-101`) | C# 3D label pass | F172 |
| Carmour door label, ghost panel, torch lead (`vehicles/client/carmour.lua`) | C# 3D label pass and marker draws | F177, F101 |
| Chat box (MTA built-in) | `chat` Svelte page | F169 |
| Bone attachments (`items/client/attach.lua`) | Local objects on bones from state bags | F171 |
| Blood, fire, smoke, sparks, fireworks (`character/client/blood.lua`, `items/client/world.lua`, `firework.lua`) | Particle and decal table | F174 |
| Zipline rope (`world/client/features.lua:213-223`) | Deferred with F079 | F079 |
| Tracers (`weapons/client/tracer.lua`) | C# 3D line | F087 |
| Presence (`presence/client/main.lua`) | `SetDiscordAppId` exists in `character/Client/Presence.cs` | F135 |

Nothing on this list is a GTA native feed, help text, or scaleform. The look is SA-MP through the MTA port, not GTA Online.

## 2. C# draw kit (F160)

Port of `ui/shared/logic/canvas.lua`, `ui/client/{actiontext,tip,progress}.lua`, and `hud/shared/logic/*`. It lives in `hud` `Client` and other resources call it through client exports that take plain values.

- **Canvas.** The MTA canvas is 640×480 units scaled by `screenH / 480` and anchored left, right, or centre. On FiveM the draw natives take normalized 0–1 coordinates. The kit keeps the 640×480 numbers from `docs/ui-reference-samp.md` and converts: `y = unitY / 480`, `x = anchorX + unitX * (480 / 640) * (screenH / screenW)` for the anchor the widget declares, with `GetActiveScreenResolution` read once per second and `GetSafeZoneSize` applied to edge anchors. The upstream numbers stay the source of truth.
- **Fonts.** SA font 1 default-bold → GTA font 0. Bank Gothic → GTA font 4 (condensed, nearest). Pricedown → GTA font 7. Outline through `SetTextOutline`, shadow through `SetTextDropShadow`. Scale is `letterSize × 0.0028` of screen height, tuned once against the MTA screenshots in `docs/reviews/`.
- **Colours.** Key list yellow `#E8E890`. Chat palette YELLOW `FFFF00`, RED `E85454`, GREEN `33AA33`, BLUE `33CCFF`, ORANGE `FFAA00`, GREY `AFAFAF`, WHITE `FFFFFF`. Box alphas 255 / 128 / 100 / 85 / 80.
- **Action text.** One centred white outlined line at unit y 350, wrap 200 units, replace-in-place. `toast(text)` holds it 3 s. Server codes arrive on `toastShown(code, args)` and the client resolves text through `Lang` (parent decision on `data`).
- **Help tip.** Green paragraph at 150,350 with an optional blue footer `#323C7F` ("Type /tooltips…"), 20 s, only for players with tooltips on. `helpTipShown` / `helpTipHidden`.
- **Hold bar.** Grey `AFAFAF` bar at 291,345, 57.5×5.19 units. The server times the action and sends `holdStarted(ms)` / `holdCancelled`; the client only animates the fill. Knockout bar at 291,315 in RED. Overheat bar at 220,380, 200×20, RED, "Overheating!" in font 7 above 80 %.
- **Skill notice.** Green "+Skill" at 320,430 for 5 s on `skillGained`.
- **Low-HP darkening.** Full-screen `DrawRect` black with alpha `(40 − hp) × 4.4` when hp < 40, rising at once and fading 4 alpha per 100 ms, suppressed while Painkill or Adrenaline is active (`hud/shared/logic/bars.lua:21-35`).
- **Countdowns.** Whitelist at 430,40 (exists). Restart countdown top centre, optional.
- **Key-action column.** Right-aligned at x 618 from y 120: yellow key name plus white verb, one line per action. Port the `_UpdateKeyActions` decision table in `hud/shared/logic/keyactions.lua:85-238` verbatim. Hidden while a dialog is open, while knocked out, before spawn, and when the HUD is hidden. Key names refresh every second (spike S7).
- **Watch (F128).** Black ellipse bottom-left at 87,338, 108×89 (`DrawRect` ring approximation or a small sprite later under F180): `%.0f DEG` at y 358, `HH:MM` at y 372 in font 4 scale 0.50, frequency `%.2f` at y 391. All RED while the combat-log window is open (`combatWindowChanged`). Uses the vehicle heading while driving. Updated every second.
- **Vehicle panel and dashboard (F177).** Driver only. Right-aligned at x 620 in font 4: `DOR` / `ENG` / `DMG` coloured by state, fuel `%.2fL/%.2f`, `%dKM/H`, vehicle name. Dashboard strip bottom centre: lamps BAT, CHK, FUEL, TYRE, LGT, a 22-segment tachometer, a voltmeter; crank and stall animations on `engineCranked` / `engineStalled`. Shown only for a vehicle type with an engine and a tank.
- **Performance.** One draw tick per frame in `hud`. Everything else writes a model the tick reads. Text measured once per change, not per frame. Nothing logs per frame.

## 3. Stock GTA HUD policy (F161)

Each frame while spawned: hide radar (`DisplayRadar(false)`), cash, wanted stars, the ammo counter, vehicle name, street name, and the weapon wheel (`HudWeaponWheelIgnoreSelection` plus `DisableControlAction` 37). Keep the health and armour bars, the weapon icon, the reticle, and the area name, as `hud/client/main.lua:27-41` does. The pause map stays (MTA kept F11). Everything is hidden before spawn, on death, and through the `setHidden` export, and comes back on the first `characterStatsSynced`. The hunger bar is a green `1FF81F` rect above the health bar at the health bar's anchor. Ammo text `mag/reserve` sits under the weapon icon for firearms with a calibre, from item data, never from the game's ammo count.

Also here: `SetUserRadioControlEnabled(false)` and `SetRadioToStationNone` on vehicle entry (`chat/client/main.lua:246-264` did `setRadioChannel(0)`), `DisableControlAction` 81, 82, 85 (radio), phone off, `InvalidateIdleCam` each frame.

## 4. GTA defaults belt (F162)

Switches GTA V needs that SA did not. Owned by `character` `Client` (player) and `weapons` `Client` (weapon), applied on spawn and on resource start:

- `NetworkSetFriendlyFireOption(true)` and `SetCanAttackFriendly(ped, true, true)`. Without them players cannot hurt each other.
- `SetWeaponsNoAutoreload(true)`, `SetWeaponsNoAutoswap(true)`, `SetPedCanSwitchWeapon(ped, false)`. Ammo and the held weapon are server-owned.
- `SetPedDropsWeaponsWhenDead(ped, false)`. Death drops are rows, not pickups.
- `SetPlayerHealthRechargeMultiplier(player, 0.0)`, `SetPedSuffersCriticalHits(ped, false)` (parent damage section).
- `SetMaxWantedLevel(0)`, `ClearPlayerWantedLevel`, `EnableDispatchService(i, false)` for every service.
- `SetPedConfigFlag(ped, 35, false)` helmet (exists in `vehicles/Client/Main.cs`).
- `SetPedMaxHealth(ped, 200)` so male and female peds share one band (F166).

## 5. Keys (F064)

`RegisterKeyMapping` commands. Players rebind in Settings → Key Bindings → FiveM. Commands are unrestricted and do no work beyond a local intent and the server event.

| Command | Default | On foot | In a vehicle |
|---|---|---|---|
| `interact` | F | Context priority from `hud/client/keys.lua:128-422`: ground item → carmour attach → ladder, zipline, button → gas mount → crate → torch → container, inspectable, machine → pickup → vehicle (bonnet, wrench, tools, rear/trunk, trailer door lock tools) → petrol can → chainsaw → fishing rod → use held item → open held bag → trailer door → pickup. Hold 500 ms: pick up a bag, welder, sign, body, pot; machine menu; battery at the bonnet; body part with the Wrench. Fishing rod: hold up to 10 s. Treatments: hold; release sends `treatmentCancelRequested`. | Enter and exit stay GTA's, except where the priority above claims F (trunk, bonnet, trailer door). |
| `dropitem` | G | Tap: drop, or give to a player within 1.5 m. Empty hands: take off parachute, else bag. Hold 300 ms on a firearm: unload. Cancels carmour attach. | Lights. |
| `putaway` | Y | Put the held item away. | Engine. |
| `inventory` | H | Toggle the inventory dialog. | Same. `DisableControlAction` 74 so the headlights do not toggle. |
| `reloadweapon` | R | Reload. | Hold 600 ms: radio panel (tap still reloads). |
| `unloadweapon` | none | Unload. | — |
| `vehiclelock` | L | — | Toggle locks (`vehicles/client/main.lua:226-231` used `sub_mission`). |
| `staffpanel` | P | Staff panel (level ≥ 1). | Same. |

- **Hold detection.** The `+cmd` handler records the down time; the `-cmd` handler ends it. A tick polls while down so 300 / 500 / 600 ms thresholds fire without waiting for release. The server still owns completion of any timed action (parent, Keys and UI).
- **Collisions.** F is `INPUT_ENTER` 23: GTA entry is disabled for 1 s after an interact, while a dialog is open, when the nearest car is out of reach or behind a wall (`keys.lua:104-126`), and when the priority picks a trunk or bonnet. Y is `INPUT_MP_TEXT_CHAT_TEAM`: disabled. H in a vehicle: control 74 disabled. N is `INPUT_PUSH_TO_TALK` and stays. G is unbound in the GTA defaults on foot and in vehicles.
- **Guards.** No command fires while the NUI has focus, the chat input is open, or `IsPauseMenuActive`.
- **Poses.** Crouch held 800 ms while still: sit (release before 250 ms stays a crouch). Sprint with crouch: hands up. Jump or sprint: stand. Aim with look-behind: aim shout. These read GTA controls (`INPUT_DUCK` 36, `INPUT_SPRINT` 21, `INPUT_JUMP` 22, `INPUT_AIM` 25, `INPUT_LOOK_BEHIND` 26) and send `sitRequested`, `standRequested`, `handsUpRequested`, `aimShoutRequested` (F151).
- **Spike S7.** Key-name display. A registered command gets an input id; the kit shows it with the `~INPUT_<hash>~` text tag if that resolves on build 3570, else the default key letter. Written into `docs/` before F160 ships.

## 6. Control belts and animations (F163)

Per state, each frame, `DisableControlAction` lists ported from the MTA `toggleControl` calls:

| State | Disabled |
|---|---|
| Cuffed (`character/server/main.lua:35-45`) | attack 24, aim 25, melee 140–143, jump 22, sprint 21, enter 23, weapon select 37, next/prev weapon 16/17 |
| Hands up (`character/server/poses.lua:50-56`) | move 30–35, jump 22, enter 23 |
| Knocked out | all, `DisableAllControlActions(0)` |
| Frozen by staff | all, plus `FreezeEntityPosition` |
| Climbing a ladder or zipline (deferred F079) | move, sprint, jump, attack, aim, enter |
| PC view (`world/client/buttons.lua:160`, deferred F130) | all |

Crouch: GTA V has no crouch. `INPUT_DUCK` toggles `SetPedMovementClipset(ped, "move_ped_crouched", 0.25)` and `ResetPedMovementClipset` so F036 can count a `crouching` kind.

Animation table. The MTA server plays SA animations by name; the FiveM server sends `animationPlayed(dict, name, flags, ms)` to the owner (and to peds in scope through the owner's sync) and the client runs `TaskPlayAnim`:

| MTA | FiveM dict / anim |
|---|---|
| Sit SUNBATHE PARKSIT_M_IN → BEACH PARKSIT_M_LOOP | `anim@amb@business@bgen@bgen_no_work@` `sit_phone_idle_01_nowork` (scenario `PROP_HUMAN_SEAT_BENCH` where a bench exists) |
| Hands up ROB_BANK SHP_HandsUp_Scr | `random@mugging3` `handsup_standing_base`, flag 49 |
| Knockout PED KO_SHOT_STOM / CAR_DEAD_LHS | `SetPedToRagdoll(ped, ms, ms, 0)`; in a vehicle `veh@low@front_ds@idle_duck` |
| Get up PED GETUP_FRONT | `get_up@standard` `front` |
| Pickup / drop BOMBER BOM_Plant_In 450 ms | `pickup_object` `pickup_low` |
| Two-handed carry (bone rotations) | movement clipset `anim@heists@box_carry@` `idle`, `SetPedMovementClipset` |
| Reload COLT45 colt45_reload | GTA's own reload task on `ReloadWeapon` input |
| Give KISSING gift_give / gift_get | `mp_common` `givetake1_a` / `givetake1_b` |
| Holster PED PHONE_IN | `reaction@intimidation@1h` `outro` |
| Wear hat/mask GOGGLES_PUT_ON | `mp_masks@standard_car@ds@` `put_on_mask` |
| Pills BAR dnk_stndM_loop | `mp_player_intdrink` `loop_bottle` |
| Self inject PED IDLE_CSAW | `mp_suicide` `pill` (clip only) |
| Self treatment SWEET Sweet_injuredloop | `amb@world_human_clipboard@male@idle_a` `idle_c` |
| CPR MEDIC CPR | `mini@cpr@cpr_ped` `cpr_pumpchest` |
| Treat another COP_AMBIENT Copbrowse_loop | `amb@medic@standing@kneel@base` `base` |
| Drink BAR dnk_stndM_loop | `mp_player_intdrink` `loop_bottle` |
| Eat (none on MTA) | `mp_player_inteat@burger` `mp_player_int_eat_burger` |
| Clothes change COPBROWSE_LOOP | `clothingshirt` `try_shirt_positive_d` |
| Craft / build / weld BOMBER BOM_Plant_Loop | `amb@world_human_gardener_plant@male@base` `base` |
| Workbench / cashier INT_SHOP SHOP_CASHIER | `mini@repair` `fixing_a_ped` |
| Vehicle repair, battery, key work COPBROWSE_LOOP | `mini@repair` `fixing_a_player` |
| Lock breaker, trunk force POLICE DOOR_KICK | `melee@unarmed@streamed_variations` `kick_short_enemy` |
| Fuel pump PED DRIVE_BOAT | `timetable@gardener@filling_can` `gar_ig_5_filling_can` |
| Chainsaw CHAINSAW CSAW_G | `melee@large_wpn@streamed_core` `ground_attack_on_spot` |
| Explosives BOM_Plant_Loop | `anim@heists@ornate_bank@thermal_charge` `thermal_charge` |
| Fishing landing SWORD sword_block | `amb@world_human_stand_fishing@idle_a` `idle_c` |
| Cuffing CASINO DEALONE | `mp_arresting` `a_uncuff` |

Names are looked up against the build 3570 clip list before each row ships; a missing clip falls back to the gardener loop, never to no animation.

## 7. Spawn, death, health (F164, F165, F166)

**First spawn.** No `spawnmanager`. After `playerAuthenticated` and the character row, the server sends `characterSpawned(x, y, z, heading, appearanceJson, bucket)` to the owner. The client: `DoScreenFadeOut`, `SetPlayerModel` from the JSON, apply components and props, `RequestCollisionAtCoord`, `SetEntityCoords`, wait for collision (5 s cap), `NetworkResurrectLocalPlayer`, `ClearPedTasksImmediately`, `RemoveAllPedWeapons`, `ShutdownLoadingScreenNui`, `DoScreenFadeIn`, then `characterSpawnFinished`. The server starts ticks on that event. The spike's `Spawn.cs`, `Spawns.cs`, base-jump list, parachute, and its colon-named events are deleted (F170).

**Death.** The server reaches hp 0 (parent, Damage). It writes `character_death_drop`, then sends `characterDied(killerName, cause)` to the owner. The owner cancels the game's wasted flow (`AnimpostfxStop("DeathFailOut")`, no `respawnPlayerPedEvent` is accepted by the server), calls `SetEntityHealth(ped, 0)`, and runs the death screen: a scripted camera rising over the body for 30 s (`CreateCam`, `RenderScriptCams`), the `hud` page shows the black overlay (alpha 185), "YOU ARE DEAD!" and the clickable ">Play Again<". Clicking sends `respawnRequested`. The server creates the new row and sends `characterSpawned` again. `respawnPlayerPedEvent` and the game's auto-respawn stay cancelled.

**Health write.** `SetEntityHealth` is client-only. The server sends `characterHealthSynced(hp, armour)` to the owner once a second and on every change. The owner writes `SetEntityHealth(ped, 100 + hp)` on the 100–200 band (`SetPedMaxHealth(ped, 200)` in F162). Armour uses the server RPC `SetPedArmour`. If the ped's health disagrees with the last synced value by more than 1, the client re-applies the synced value; the server value wins. The parent spec's damage section is corrected to this.

## 8. Effects (F167, F174)

- **Drunk and drugs** (`character/client/medical.lua`): `ShakeGameplayCam("DRUNK_SHAKE", level)` and `SetTimecycleModifier("drug_wobbly")` with strength from the drug; `ClearTimecycleModifier` when it ends. Staff on duty exempt.
- **LSD and Heroin sky**: the client overrides `SetWeatherTypeNowPersist` and `NetworkOverrideClockTime` locally every 500 ms while the drug runs; `drugSkyEnded` clears the override and the world cycle (F072, F073) is visible again.
- **Infection shake**: `ShakeGameplayCam("SMALL_EXPLOSION_SHAKE", x)` at the MTA cadence.
- **Knockout**: ragdoll, then the KO anim; the knockout bar from the draw kit.
- **Bleeding**: `UseParticleFxAsset("core")`, `StartParticleFxLoopedOnEntityBone` `blood_stab` on the spine while `bleeding` is set on the player's state bag.
- **Blood pools**: `AddDecal` type 1010 at the server-placed point, same lifetime as the MTA pool object.
- **Particle table** (F174): campfire and barbecue `core` `ent_amb_fire_gen_sm`; machine smoke `core` `ent_amb_smoke_general`; flare trail `core` `exp_grd_flare`; fireworks `scr_indep_fireworks` `scr_indep_firework_starburst`; welding and grinding sparks `core` `ent_amb_sparking_wires`; keypad fault sparks the same. Each is a client-local looped fx keyed by the server row id and stopped when the id leaves the projection.

## 9. Attachments (F171)

MTA replicated item unames through element data and each client attached a bone object. FiveM: the server writes player state bags (`sv_stateBagStrictMode` is on) with unames and flags, never contents:

| Key | Value | Reader |
|---|---|---|
| `held` | item uname or empty | bone `SKEL_R_Hand` 57005 object; carmour attach check; "Draw weapon" line |
| `holster` | uname | `SKEL_Spine3` back object; key list |
| `bag` | uname | freemode component 5 through `apparel`; empty-hand G action |
| `hat`, `mask`, `armour` | uname | freemode props and component 9 through `apparel` |
| `cuffed` | 0/1 | wrist objects; control belt; key list |
| `torch` | 0/1 | welder torch in hand |
| `ammo` | loaded ammo type | tracers (F087) |
| `knockedOut`, `bleeding` | 0/1 | key list hidden; blood fx |
| `duty` | 0/1 | no drug effects; attachments stripped; nametag |
| `lang` | code | client `Lang` |

Every client watches peds in scope and attaches local objects (`CreateObject` with networking off, `AttachEntityToEntity`) for the keys that name a prop. A key change replaces the object. Two-handed items use the carry clipset instead of the MTA per-frame bone rotation. Held weapons are the real GTA weapon given to the ped by the owner on `weaponGiven` from the server (`giveWeaponEvent` from any other source is cancelled, parent).

## 10. 3D labels (F172)

One pass in `hud` at 300 ms for candidates and every frame for the draw, using `GetScreenCoordFromWorldCoord`:

- Item names, yellow, within 4 m with line of sight (`items/client/world.lua:601-692`).
- Built defences, hitpoints within 10 m.
- Sign text, white, within 25 m.
- Supply crate and weapon cache, yellow, within 20 m (`loot/client/main.lua:84-101`).
- Carmour door "hp/max" within 10 m from the door side.
- Staff nametags "name hp% dist" within 300 m, only when the server turned them on for duty or `/nametags` (F126).
- Voice icon over a talking head within 40 m with line of sight (F175).
- Tweak-pad direction arrow (F061).

Players see no nametags by default. FiveM draws none; the gamemode adds none.

## 11. NUI pipeline (F159)

- `hud/web` and `chat/web` are Svelte 5 + Vite apps: `web/src`, `web/package.json`, `web/vite.config.js`, output `web/dist`. `web/dist` and `web/node_modules` are gitignored.
- `fxmanifest.lua`: `ui_page 'web/dist/index.html'` and `files { 'web/dist/**' }`.
- `tools/nui-build.sh <resource>` runs `npm ci --prefix web && npm run build --prefix web` inside that resource, then `qrcon 127.0.0.1:30120 "$RCON" -c "restart <resource>"`. It is the only way a `web/src` change reaches the server. The build is not committed, the same way `bin/` is not; unlike the DLLs, FXServer serves `web/dist` straight from disk, so the server builds it.
- C#: `RegisterNuiCallbackType("dialogResponded")` and a handler on the runtime's NUI callback event for that name; `SendNuiMessage` JSON `{ "type": "dialogOpened", ... }`. Callback names and `type` values are `<subject><PastTenseVerb>`. The Svelte app dispatches on `type`.
- Focus: `SetNuiFocus(true, true)` only while a dialog is open; `SetNuiFocusKeepInput(false)`; Escape closes the top dialog and releases focus. The HUD never takes focus because the HUD is not in the page.
- Every mutating callback carries the `lib` one-shot token (parent, anti-spoof). The NUI never calls MariaDB and never learns another player's identifier.
- `hud` and `chat` do not depend on each other. Each page is its own app.

## 12. Dialog kit (F168, F045, F173)

Port of `ui/client/{list,dialogbox,info}.lua` and `ui/shared/logic/dialog.lua` as Svelte components in `hud/web`:

- **List dialog** (SA-MP `DIALOG_STYLE_LIST` clone): box 280×203 units, title `Inventory (used/size)` or `<Container> (used/size)`, rows `[NN]Name (state)`, `<Empty>` padding, nav rows `My Inventory >`, `<Bag> >`, `<Trunk> >`, `[Pick up container]`, `Radio`, `Skills`, buttons Options / Close, selected row `#81181B`, second level Accept / Back with Equip / Use / Wear / Move to… / Store in… / Combine. Keys: up, down, wheel, Enter, Escape or Backspace, double-click. Colours box `#0D0B07`, list `#020202`, title `#2A2829`, button `#818183`.
- **Message, input, password dialogs**: same chrome, 280–600 units wide, colour-coded body, one-line field (masked for passwords, 128 chars, paste), blinking caret, Enter accepts, Escape refuses. API `dialogOpened(id, style, title, body, left, right)` → callback `dialogResponded(id, accepted, text)`.
- **Info windows**: coloured header, a table or scrolling text, footer, Close; Close, Escape, or Enter shuts them. `/help`, `/chatinfo`, `/rules`, `/admins`, `/credits`, `/mstats`.
- **Inventory (F045)**: the list dialog above plus the body panel at x 100 y 120 w 120 ("Health status and wounds": seven body parts red when > 0, Bleeding, Hungry, Food Infect, Wound Infect, drugs in yellow, "%.1f%% Unconsciousness chance") and six 60×60 gear tiles Head / Face / Hand / Holster / Torso / Back with caps captions and `<EMPTY>`. Tile clicks from `items/client/inventory.lua:738-772`: bag tile toggles the bag view, hand tile puts away, a worn tile takes off, a tile matching the held item wears it. No 3D previews (F116 deferred).
- **Small dialogs (F173)**: keypad 3×4 "SECURITY KEYPAD" / "SET PASSCODE", yellow display, number row and numpad, Backspace, Enter, Escape, hack mode cycling random codes, sounds through `PlaySoundFrontend` (`NAV_UP_DOWN`, `SELECT`, `ERROR` from `HUD_FRONTEND_DEFAULT_SOUNDSET`); radio panel with dial, ±0.5 MHz knobs, global/freq toggle, frequency input 87.5–108.0, wheel ±0.01, power, Close; tweak pad with eight buttons L, R, up, left, right, down, Move, Done and a repeat every 150 ms while held (the cursor is the NUI cursor; no LALT toggle); machine menu Start / Cancel; note write (Done / Cancel) and read (Close / Tear); sign input; body inspection; plant pot status; recipes window with tabs Combination / Construction / Workbench / Help; skills list coloured by level.
- **Staff (F178)**: the panel (760×520 command browser whose form is built from each command's usage line), grid lists for `/reports`, `/acmds`, `/banlist`, `/issues`, the two-step ban dialog, the `/report` chain, the `/bug` input. The views live in `hud/web`; the logic lives in `staff`; `staff` client fires local events with ids and strings only.
- **Death screen (F165)**: overlay and the two boxed lines.

Fonts in the page: a free condensed face for the Bank Gothic look and a bold sans for default-bold, shipped in `web/dist`.

## 13. Chat (F169)

A `chat` resource under `resources/[scavengesurvive]/chat` with `Client`, `Server`, `Shared`, and `web`. The stock `chat` leaves `server.cfg` when this row lands; the live check confirms FXServer takes the gamemode folder for the name (if it refuses the duplicate, the resource is `chatbox`).

- **Modes** (`chat/shared/logic/chat.lua:6-15`): local 0 (40 m, same bucket), global 1 (everyone except `/quiet` players), radio 2 (same frequency within ±0.05), admin 3 (staff only). Every spawn starts in global. The frequency is on the character row.
- **Formats**: `[Local] name: text` in `33CCFF`, `[Global] …` white, `[%.2f] …` in `FFAA00`, `<levelcolour>[Admin] …`, `/me` as `[Local] name text`. Server notices start with ` >  ` in yellow. Lines over 128 characters split at a space. Join: an orange copyright line, ` >  name has joined` in grey, then the MoTD (F024). Discord lines `[Discord] name: text` in `#5865F2` through the node `discord` resource's `sendMessage` export and its incoming event.
- **Commands**: `/g`, `/global`, `/l`, `/local`, `/r` (alone switch the mode, with text send one line), `/me`, `/quiet`, `/freq <87.5–108>`, `/radio`, `/help`, `/chatinfo`, `/rules`, `/admins`, `/credits`, `/pos`, `/motd`.
- **Flood**: 3 messages inside 1 s → 30 s mute with `MUTEDFLOODM`. A muted account's lines are dropped; `staff` tells them once.
- **Events**: client `chatMessageSent(text)`, `chatModeRequested(mode)`; server `chatMessageShown(colour, text)` to one player or to all, `chatCleared`, `chatModeChanged(mode)`. The existing calls to the stock chat's colon-named event in `auth`, `staff`, and `dev` move to `chatMessageShown`.
- **Page**: T opens the input; the input is the only focus the chat page takes; Escape closes it; timestamps client-side; the box hides with the HUD.

## 14. Voice (F175)

FiveM's Mumble voice, configured by `chat`:

- Local always: `MumbleSetTalkerProximity(40.0)` (`NetworkSetTalkerProximity` on older builds).
- Plus the active chat mode, mirroring MTA `setPlayerVoiceBroadcastTo` (`chat/server/main.lua:366-373`): global = a voice target containing every player (`MumbleAddVoiceTargetPlayerByServerId` for all); radio = a channel per frequency (`MumbleSetVoiceChannel(frequency × 100)`, `MumbleAddVoiceTargetChannel`); admin = the staff channel only while `/a` is active for that player.
- N is `INPUT_PUSH_TO_TALK`. Not rebound by the gamemode.
- Icons: `MumbleIsPlayerTalking` per player in scope → the 3D label pass draws the speaker icon within 40 m with line of sight; a centre-left speaker list and "TALKING [LOCAL/GLOBAL/freq]" for the local player, as `hud/client/voice.lua:141-260`.
- Mute: `staff` tells the muted client `voiceMuted(seconds)`; the client sets `NetworkSetVoiceActive(false)` until it ends; the player is told once.

## 15. Vehicles client half (F177)

Vehicle state bags (server-written): `vehicleId`, `type`, `fuel`, `locked`, `engine`, `lights`, `battery`, `bay`, `loose`, `carmour`, `doorHp`, `gasBottles`, `cargo`, `containerId`. The owner client applies `engine` with `SetVehicleEngineOn`, `lights` with `SetVehicleLights`, bonnet, boot, and door states with `SetVehicleDoorOpen` / `SetVehicleDoorShut`. The server sets locks itself through the `SetVehicleDoorsLocked` RPC (state 2 locked, 1 unlocked, 8 locked with boot open for the trunk). The engine stall at fuel 0 is the server flipping `engine` to 0; the owner applies it. Trailer attach is done by the driver's client (`AttachVehicleToTrailer`) on `trailerHitchRequested` accepted by the server. GTA entry is blocked for cars out of reach or behind a wall, and "Put that down before getting in" (F104) is an action text. Carmour attach mode draws the ghost panel with `DrawMarker`, the "+" reticle, and the F / G hints.

## 16. Staff client half (F178)

Spectate: `NetworkSetInSpectatorMode(true, targetPed)`, `SetEntityVisible(self, false)`, arrows cycle targets through `spectateRequested(direction)`, the banner "Spectating X hp N on foot" from the draw kit, bucket follows the target (server `SetPlayerRoutingBucket`). Freeze: `FreezeEntityPosition` on `playerFrozen` / `playerUnfrozen` plus the control belt. Duty: `SetWeatherTypeNowPersist("CLEAR")` locally while on duty, cleared on exit. Staff nametags are the label pass.

## 17. Anticheat client (F179)

MTA reported focus loss from the client. FiveM has no window-focus native; the `hud` page listens to `visibilitychange` and `blur` / `focus` and posts `focusChanged(bool)` as a NUI callback, which the client forwards as `anticheatFocusChanged`. `logs` receives `clientSampleReported(fps, ping, streamedObjects)` every 30 s and `clientErrorReported(text)` on an unhandled client exception. `sv_pureLevel` is recorded as a convar decision in the parent server configuration when M7 starts; it is not set until then.

## 18. Client claims

Extends the parent anti-spoof table. The client may say these; the server checks each:

| Event | Claim | Server check |
|---|---|---|
| `weaponHitReported(attacker, weapon, bodypart, class)` | a hit of that class landed | parent Damage: distance, rate, weapon equals the held item, class bands (F083, F086) |
| `groundSurfaceReported(id, z, nx, ny)` | ground under a drop point | parent F044 clamp |
| `fishCastRequested(heldMs, waterZ)` | cast length and water height | `GetWaterHeight` on the server position (F056) |
| `itemPickupRequested(id, blockedIds)` | nearby items a wall hides | distance from `GetEntityCoords`; blocked ids only skip the label, never move an item |
| `parachuteSpent` | the parachute was used | holder kind `parachute` exists and the ped fell (F060) |
| `playerJumped` | one jump | at most one per 400 ms (F036) |
| `anticheatFocusChanged(bool)` | focus lost or regained | timer only (F179) |
| `carmourAttachRequested(vehicleId, offset, dir)` | placement from the camera ray | within reach of the door (F101) |
| `holdActionCancelRequested` | the player moved more than 0.8 m | server distance (F160) |
| `characterSpawnFinished` | the ped is placed | once per `characterSpawned` |
| `dialogResponded(id, accepted, text)` | a dialog answer | one-shot token, id outstanding for that player |

## 19. Live checks

The on-screen steps of MTA `docs/mvp-smoke-checklist.md`, two clients, for the client rows. Each row's issue names the step it closes.

1. Join: no radar, no cash, no wanted stars; the health bar with the hunger bar above it; the watch bottom-left; the key list on the right; the chat box with the copyright, join, and MoTD lines.
2. H opens `Inventory (1/6)` with `[01]Knife`; Escape closes; the key list hides while it is open.
3. F picks up a Wrench; the label appeared within 2 s; the pickup anim played; the second client saw the Wrench in the first client's hand.
4. G drops it; hold G 300 ms on a loaded pistol unloads; "Transferred N rounds" toast.
5. Y puts away; `Y Draw weapon` appears while holstered; the second client sees the weapon on the back.
6. Starvation: the screen darkens below 40 hp; death shows the rising camera, the overlay, "YOU ARE DEAD!" and ">Play Again<"; no wasted shard; Play Again spawns a new character at a shoreline point.
7. PvP: a pistol shot shows "Wounded: torso" on the victim; the victim's health bar moves only when the server says so (S2); friendly fire is on.
8. Chat: `[Local]` light blue within 40 m, `[Global]` white beyond, `/me`, `/quiet`, `[101.30]` orange; three lines in a second mutes for 30 s.
9. Voice: N talks; the icon shows over the head within 40 m; `/g` voice is heard across the map; `/r` voice is heard on the same frequency only.
10. Restart `hud`: the HUD comes back, the inventory reopens, nothing stays focused. Restart `chat`: history is gone, the input works.
11. Keypad: the 3×4 pad, wrong code sound, hack mode cycling. Radio panel: knobs, wheel, frequency input.
12. Staff: P opens the panel; `/spec` shows the banner and arrows cycle; `/freeze` holds the target; staff nametags show within 300 m.
13. Vehicle: the panel shows DOR / ENG / DMG, fuel, speed, name; Y engine; G lights; L locks; the stall at fuel 0 kills the engine on the driver's screen.
14. Alt-tab for longer than the limit kicks; the `clientSampleReported` line appears in the log every 30 s.
