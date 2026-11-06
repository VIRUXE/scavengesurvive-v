-- This Source Code Form is subject to the terms of the Mozilla Public
-- License, v. 2.0. If a copy of the MPL was not distributed with this
-- file, You can obtain one at https://mozilla.org/MPL/2.0/.
--
-- Derived from Southclaws/ScavengeSurvive (MPL-2.0), via
-- VIRUXE/scavengesurvive-mta sql/seed/0003_settings.sql. See NOTICE.md. F014.
-- Hand-edited. No San Andreas coordinates.
-- loot.count_carried is the ADR 0001 default. The MTA seed omitted it.
-- auth.license_autologin is the FiveM license auto-resume switch (F018). Default on.
-- player.auto_ip_login stays 0. That switch is not read.
-- A second load keeps a value that was changed on the live table.

INSERT INTO settings (skey, svalue) VALUES
('items.pickup_radius','2.0'),
('items.sync_radius','150'),
('loot.cache_interval_s','1500'),
('loot.cache_jitter_s','600'),
('loot.cache_min_players','4'),
('loot.cap_multiplier','1.0'),
('loot.cooldown_jitter','0.5'),
('loot.cooldown_minutes','60'),
('loot.count_carried','1'),
('loot.despawn_minutes','120'),
('loot.drop_point_reuse_hours','24'),
('loot.items_per_point','2.0'),
('loot.refill_floor','0.02'),
('loot.refill_rate','0.10'),
('loot.roll_batch','300'),
('loot.spawn_multiplier','1.0'),
('loot.supply_cooldown_s','600'),
('loot.supply_tick_s','60'),
('auth.license_autologin','1'),
('player.auto_ip_login','0'),
('player.combat_log_window','30'),
('player.idle_kick','1'),
('player.login_freeze_time','8'),
('player.max_tab_out_time','60'),
('player.ping_limit','400'),
('server.motd',''),
('server.rules',''),
('server.staff',''),
('server.whitelist','0'),
('server.whitelist_auto','0'),
('server.whitelist_kick_s','300'),
('spawn.bagtype','Satchel'),
('spawn.new_bleed','0.0001'),
('spawn.new_blood','90'),
('spawn.new_food','80'),
('spawn.new_items','Knife'),
('spawn.res_bleed','0'),
('spawn.res_blood','100'),
('spawn.res_food','40'),
('spawn.res_items','AntiSepBandage'),
('staff.max_warnings','5'),
('staff.warn_ban_hours','24'),
('vehicles.abandon_days','3'),
('vehicles.cap_multiplier','1.0'),
('vehicles.cooldown_hours','6'),
('vehicles.cooldown_jitter','0.33'),
('vehicles.damage_bleed_mult','1.0'),
('vehicles.damage_knock_mult','1.0'),
('vehicles.leave_radius','100'),
('vehicles.refill_floor','0.02'),
('vehicles.refill_rate','0.10'),
('vehicles.spawn_chance','4.0'),
('world.boxes_player_distance','60'),
('world.boxes_respawn_s','900'),
('world.min_player_distance','150'),
('world.refill_interval_s','300')
ON DUPLICATE KEY UPDATE svalue = svalue;
