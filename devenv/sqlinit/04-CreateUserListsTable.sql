USE `dalamud`;
CREATE TABLE IF NOT EXISTS `users_lists` (
  `id` char(36) COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '(DC2Type:guid)',
  `user_id` char(36) COLLATE utf8mb4_unicode_ci DEFAULT NULL COMMENT '(DC2Type:guid)',
  `added` int(11) NOT NULL,
  `updated` int(11) NOT NULL,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `custom` tinyint(1) NOT NULL DEFAULT 0,
  `custom_type` int(11) DEFAULT NULL,
  `items` longtext COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '(DC2Type:array)',
  PRIMARY KEY (`id`),
  KEY `IDX_EF513BECA76ED395` (`user_id`),
  CONSTRAINT `FK_EF513BECA76ED395` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;