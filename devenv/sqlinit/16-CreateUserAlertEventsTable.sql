USE `dalamud`;
CREATE TABLE `users_alerts_events` (
  `id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '(DC2Type:guid)',
  `event_id` char(36) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '(DC2Type:guid)',
  `user_id` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `added` int(11) NOT NULL,
  `data` longtext COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`id`),
  KEY `IDX_93DB681F71F7E88B` (`event_id`),
  CONSTRAINT `FK_93DB681F71F7E88B` FOREIGN KEY (`event_id`) REFERENCES `users_alerts` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;