-- F004. Unchanged tables, triggers, and events from the MTA dump, plus the
-- MTA-shaped parents their foreign keys, triggers, and events require.
-- MariaDB 13.1.1. Database `ssv`. No procedures. No seeds. No player rows.
-- Parents still in the MTA column shape, for F005 to rewrite: bans, item_types,
-- items, loot_spawns, vehicle_spawns, vehicles, world_items_pos.
-- The 2025 login tables accounts, account_identifiers, and account_tokens are
-- still in the live database and are not in this dump.
-- Change the live database, then dump again. There are no migration files.

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS `anticheat_field_logs` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `field_id` int(11) NOT NULL,
  `player_name` varchar(64) NOT NULL,
  `x` float NOT NULL,
  `y` float NOT NULL,
  `z` float NOT NULL,
  `logged_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  KEY `ix_anticheat_field_logs_field` (`field_id`,`id`),
  KEY `ix_anticheat_field_logs_player` (`player_name`,`logged_at`),
  CONSTRAINT `fk_anticheat_field_logs_field` FOREIGN KEY (`field_id`) REFERENCES `anticheat_fields` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `anticheat_fields` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(64) NOT NULL,
  `points` text NOT NULL,
  `min_z` float NOT NULL,
  `max_z` float NOT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_anticheat_fields_name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `anticheat_traps` (
  `spot` smallint(6) NOT NULL,
  `item_id` int(11) DEFAULT NULL,
  PRIMARY KEY (`spot`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `bans` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `account_id` int(11) DEFAULT NULL,
  `name` varchar(24) NOT NULL,
  `ip` varchar(45) DEFAULT NULL,
  `serial` char(32) DEFAULT NULL,
  `reason` varchar(128) NOT NULL DEFAULT '',
  `by_account_id` int(11) DEFAULT NULL,
  `by_name` varchar(24) NOT NULL DEFAULT 'Server',
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  `expires_at` datetime DEFAULT NULL,
  `active` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  KEY `ix_bans_name` (`name`,`active`),
  KEY `ix_bans_ip` (`ip`,`active`),
  KEY `ix_bans_serial` (`serial`,`active`),
  KEY `ix_bans_expiry` (`active`,`expires_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci WITH SYSTEM VERSIONING;

CREATE TABLE IF NOT EXISTS `bug_reports` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(24) NOT NULL,
  `account_id` int(11) DEFAULT NULL,
  `reason` varchar(128) NOT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  KEY `ix_bug_reports_created` (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `containers` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `kind` enum('inventory','bag','safebox','tent','vehicle','supply') NOT NULL,
  `size` tinyint(4) NOT NULL,
  `owner_char_id` int(11) DEFAULT NULL,
  `owner_item_id` int(11) DEFAULT NULL,
  `owner_vehicle_id` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_containers_owner_item` (`owner_item_id`),
  UNIQUE KEY `uq_containers_owner_vehicle` (`owner_vehicle_id`),
  KEY `ix_containers_owner_char` (`owner_char_id`),
  CONSTRAINT `fk_containers_owner_item` FOREIGN KEY (`owner_item_id`) REFERENCES `items` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_containers_owner_vehicle` FOREIGN KEY (`owner_vehicle_id`) REFERENCES `vehicles` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `felled_trees` (
  `tree_key` varchar(48) NOT NULL,
  `felled_at` datetime NOT NULL DEFAULT current_timestamp(),
  `regrow_at` datetime NOT NULL,
  PRIMARY KEY (`tree_key`),
  KEY `idx_felled_trees_regrow` (`regrow_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `item_types` (
  `uname` varchar(32) NOT NULL,
  `name` varchar(48) NOT NULL,
  `model` int(11) NOT NULL,
  `size` tinyint(4) NOT NULL DEFAULT 1,
  `max_hitpoints` smallint(6) NOT NULL DEFAULT 5,
  `category` enum('generic','weapon','ammo','food','liquid','bag','safebox') NOT NULL DEFAULT 'generic',
  `category_data` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL CHECK (json_valid(`category_data`)),
  `container_size` smallint(6) GENERATED ALWAYS AS (cast(coalesce(json_value(`category_data`,'$.size'),json_value(`category_data`,'$.bag_size')) as signed)) STORED,
  PRIMARY KEY (`uname`),
  CONSTRAINT `chk_item_types_data` CHECK (`category_data` is null or json_valid(`category_data`))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `items` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `type_uname` varchar(32) NOT NULL,
  `hitpoints` smallint(6) NOT NULL DEFAULT 5,
  `data` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL CHECK (json_valid(`data`)),
  `x` float DEFAULT NULL,
  `y` float DEFAULT NULL,
  `z` float DEFAULT NULL,
  `rz` float DEFAULT NULL,
  `ground_nx` float DEFAULT NULL,
  `ground_ny` float DEFAULT NULL,
  `interior` tinyint(4) DEFAULT NULL,
  `dimension` int(11) DEFAULT NULL,
  `container_id` int(11) DEFAULT NULL,
  `slot` tinyint(4) DEFAULT NULL,
  `holder_char_id` int(11) DEFAULT NULL,
  `holder_kind` enum('held','holster','bag','hat','mask','armour','parachute') DEFAULT NULL,
  `loot_spawn_id` int(11) DEFAULT NULL,
  `loot_zone` varchar(8) DEFAULT NULL,
  `despawn_at` datetime DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `row_start` timestamp(6) GENERATED ALWAYS AS ROW START,
  `row_end` timestamp(6) GENERATED ALWAYS AS ROW END,
  PRIMARY KEY (`id`,`row_end`),
  UNIQUE KEY `uq_items_container_slot` (`container_id`,`slot`,`row_end`),
  UNIQUE KEY `uq_items_holder_kind` (`holder_char_id`,`holder_kind`,`row_end`),
  KEY `ix_items_loot_spawn` (`loot_spawn_id`),
  KEY `ix_items_type` (`type_uname`),
  KEY `ix_items_loot_zone` (`loot_zone`,`row_end`),
  KEY `ix_items_despawn` (`row_end`,`despawn_at`),
  PERIOD FOR SYSTEM_TIME (`row_start`, `row_end`),
  CONSTRAINT `fk_items_container` FOREIGN KEY (`container_id`) REFERENCES `containers` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_items_type` FOREIGN KEY (`type_uname`) REFERENCES `item_types` (`uname`),
  CONSTRAINT `chk_items_one_location` CHECK ((`x` is not null) + (`container_id` is not null) + (`holder_char_id` is not null) = 1),
  CONSTRAINT `chk_items_data` CHECK (`data` is null or json_valid(`data`))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci WITH SYSTEM VERSIONING;

CREATE TABLE IF NOT EXISTS `loot_spawn_state` (
  `spawn_id` int(11) NOT NULL,
  `next_roll_at` datetime NOT NULL,
  PRIMARY KEY (`spawn_id`),
  KEY `ix_loot_spawn_state_due` (`next_roll_at`),
  CONSTRAINT `fk_loot_spawn_state_spawn` FOREIGN KEY (`spawn_id`) REFERENCES `loot_spawns` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `loot_spawns` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `x` float NOT NULL,
  `y` float NOT NULL,
  `z` float NOT NULL,
  `table_name` varchar(32) NOT NULL,
  `weight` float NOT NULL,
  `size` tinyint(4) NOT NULL DEFAULT -1,
  `interior` tinyint(4) NOT NULL DEFAULT 0,
  `dimension` int(11) NOT NULL DEFAULT 0,
  `zone` varchar(8) NOT NULL,
  `enabled` tinyint(1) NOT NULL DEFAULT 1,
  `pos` point NOT NULL,
  PRIMARY KEY (`id`),
  SPATIAL KEY `sp_loot_spawns_pos` (`pos`),
  KEY `ix_loot_spawns_zone` (`zone`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `settings` (
  `skey` varchar(48) NOT NULL,
  `svalue` varchar(2048) NOT NULL,
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`skey`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `staff_actions` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `actor_account_id` int(11) DEFAULT NULL,
  `actor_name` varchar(24) NOT NULL,
  `action` varchar(32) NOT NULL,
  `target_name` varchar(24) DEFAULT NULL,
  `target_account_id` int(11) DEFAULT NULL,
  `details` varchar(255) DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  KEY `ix_staff_actions_actor` (`actor_account_id`,`created_at`),
  KEY `ix_staff_actions_target` (`target_name`,`created_at`),
  KEY `ix_staff_actions_action` (`action`,`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `vehicle_events` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `vehicle_id` int(11) NOT NULL,
  `character_id` int(11) DEFAULT NULL,
  `name` varchar(24) NOT NULL,
  `seat` tinyint(4) NOT NULL,
  `action` enum('enter','exit') NOT NULL,
  `x` float NOT NULL,
  `y` float NOT NULL,
  `z` float NOT NULL,
  `at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  KEY `ix_vehicle_events_vehicle` (`vehicle_id`,`at`),
  KEY `ix_vehicle_events_character` (`character_id`,`at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `vehicle_spawn_state` (
  `spawn_id` int(11) NOT NULL,
  `next_roll_at` datetime NOT NULL,
  PRIMARY KEY (`spawn_id`),
  KEY `ix_vehicle_spawn_state_due` (`next_roll_at`),
  CONSTRAINT `fk_vehicle_spawn_state_spawn` FOREIGN KEY (`spawn_id`) REFERENCES `vehicle_spawns` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `vehicle_spawns` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `x` float NOT NULL,
  `y` float NOT NULL,
  `z` float NOT NULL,
  `rz` float NOT NULL,
  `group_name` varchar(16) DEFAULT NULL,
  `type_name` varchar(32) DEFAULT NULL,
  `categories` varchar(8) NOT NULL DEFAULT '',
  `sizes` varchar(3) NOT NULL DEFAULT '',
  `file` varchar(48) NOT NULL,
  `enabled` tinyint(1) NOT NULL DEFAULT 1,
  `pos` point NOT NULL,
  PRIMARY KEY (`id`),
  SPATIAL KEY `sp_vehicle_spawns_pos` (`pos`),
  KEY `ix_vehicle_spawns_file` (`file`),
  CONSTRAINT `ck_vehicle_spawns_target` CHECK (`group_name` is null <> (`type_name` is null))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `vehicles` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `spawn_id` int(11) DEFAULT NULL,
  `type_uname` varchar(32) NOT NULL,
  `model` smallint(6) NOT NULL,
  `x` float NOT NULL,
  `y` float NOT NULL,
  `z` float NOT NULL,
  `rz` float NOT NULL DEFAULT 0,
  `health` float NOT NULL DEFAULT 1000,
  `fuel` float NOT NULL DEFAULT 0,
  `locked` tinyint(1) NOT NULL DEFAULT 0,
  `key_code` int(11) DEFAULT NULL,
  `keypad_code` char(4) DEFAULT NULL,
  `trailer_id` int(11) DEFAULT NULL,
  `damage` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL CHECK (json_valid(`damage`)),
  `carmour` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL CHECK (json_valid(`carmour`)),
  `gas_bottles` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL CHECK (json_valid(`gas_bottles`)),
  `left_spawn` tinyint(1) NOT NULL DEFAULT 0,
  `last_used_at` datetime DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `battery` float DEFAULT NULL,
  `battery_since` int(10) unsigned DEFAULT NULL,
  `starter` smallint(5) unsigned DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `ix_vehicles_spawn` (`spawn_id`),
  KEY `fk_vehicles_trailer` (`trailer_id`),
  CONSTRAINT `fk_vehicles_spawn` FOREIGN KEY (`spawn_id`) REFERENCES `vehicle_spawns` (`id`) ON DELETE SET NULL,
  CONSTRAINT `fk_vehicles_trailer` FOREIGN KEY (`trailer_id`) REFERENCES `vehicles` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `whitelist` (
  `name` varchar(24) NOT NULL,
  `added_by` varchar(24) NOT NULL DEFAULT 'Server',
  `added_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `world_crates` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `kind` enum('supply','cache') NOT NULL,
  `container_id` int(11) NOT NULL,
  `x` float NOT NULL,
  `y` float NOT NULL,
  `z` float NOT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_world_crates_container` (`container_id`),
  KEY `ix_world_crates_kind` (`kind`),
  CONSTRAINT `fk_world_crates_container` FOREIGN KEY (`container_id`) REFERENCES `containers` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

CREATE TABLE IF NOT EXISTS `world_items_pos` (
  `item_id` int(11) NOT NULL,
  `pos` point NOT NULL,
  PRIMARY KEY (`item_id`),
  SPATIAL KEY `sp_world_items_pos` (`pos`),
  CONSTRAINT `fk_world_items_pos_item` FOREIGN KEY (`item_id`) REFERENCES `items` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

SET FOREIGN_KEY_CHECKS = 1;

DELIMITER //

CREATE OR REPLACE TRIGGER items_ground_reset BEFORE UPDATE ON items FOR EACH ROW
BEGIN
  IF NOT (OLD.x <=> NEW.x AND OLD.y <=> NEW.y AND OLD.z <=> NEW.z)
    AND OLD.ground_nx <=> NEW.ground_nx AND OLD.ground_ny <=> NEW.ground_ny THEN
    SET NEW.ground_nx = NULL, NEW.ground_ny = NULL;
  END IF;
END//

CREATE OR REPLACE TRIGGER items_world_pos_insert AFTER INSERT ON items FOR EACH ROW
BEGIN
  IF NEW.x IS NOT NULL THEN
    INSERT INTO world_items_pos (item_id, pos) VALUES (NEW.id, POINT(NEW.x, NEW.y));
  END IF;
END//

CREATE OR REPLACE TRIGGER items_world_pos_update AFTER UPDATE ON items FOR EACH ROW
BEGIN
  IF NEW.x IS NULL THEN
    IF OLD.x IS NOT NULL THEN
      DELETE FROM world_items_pos WHERE item_id = NEW.id;
    END IF;
  ELSEIF NOT (OLD.x <=> NEW.x AND OLD.y <=> NEW.y) THEN
    INSERT INTO world_items_pos (item_id, pos) VALUES (NEW.id, POINT(NEW.x, NEW.y))
      ON DUPLICATE KEY UPDATE pos = VALUE(pos);
  END IF;
END//

CREATE OR REPLACE EVENT `ev_despawn_items` ON SCHEDULE EVERY 1 MINUTE STARTS '2026-10-25 12:32:28' ON COMPLETION NOT PRESERVE ENABLE DO DELETE FROM items WHERE despawn_at IS NOT NULL AND despawn_at < NOW() AND x IS NOT NULL//

CREATE OR REPLACE EVENT `ev_expire_bans` ON SCHEDULE EVERY 1 MINUTE STARTS '2026-10-25 12:32:28' ON COMPLETION NOT PRESERVE ENABLE DO UPDATE bans SET active = 0 WHERE active = 1 AND expires_at IS NOT NULL AND expires_at < NOW()//

CREATE OR REPLACE EVENT `ev_item_history` ON SCHEDULE EVERY 1 HOUR STARTS '2026-10-25 12:32:28' ON COMPLETION NOT PRESERVE ENABLE DO DELETE HISTORY FROM items BEFORE SYSTEM_TIME NOW() - INTERVAL 7 DAY//

DELIMITER ;
