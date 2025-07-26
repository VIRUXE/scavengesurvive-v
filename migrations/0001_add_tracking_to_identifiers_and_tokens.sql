ALTER TABLE `account_identifiers`
ADD COLUMN `last_used` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
ADD COLUMN `times_used` int(11) NOT NULL DEFAULT 1;

ALTER TABLE `account_tokens`
ADD COLUMN `last_used` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
ADD COLUMN `times_used` int(11) NOT NULL DEFAULT 1; 