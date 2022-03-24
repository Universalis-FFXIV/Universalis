USE `dalamud`;
CREATE TABLE `users_sessions` (
  `id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '(DC2Type:guid)',
  `user_id` char(36) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '(DC2Type:guid)',
  `session` varchar(191) COLLATE utf8mb4_unicode_ci NOT NULL,
  `last_active` int(11) NOT NULL,
  `site` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `UNIQ_E121B6C9D044D5D4` (`session`),
  KEY `IDX_E121B6C9A76ED395` (`user_id`),
  KEY `session` (`session`),
  CONSTRAINT `FK_E121B6C9A76ED395` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;