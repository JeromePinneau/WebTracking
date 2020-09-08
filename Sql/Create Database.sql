
CREATE DATABASE emailtrack;

USE emailtrack;

CREATE TABLE `campaign` (
  `_id` varchar(50) NOT NULL,
  `Name` varchar(45) NOT NULL,
  `EmailSent` int DEFAULT NULL,
  `Domain` varchar(255) DEFAULT NULL,
  `DynamicField` varchar(255) DEFAULT NULL,
  `CreationDate` datetime DEFAULT NULL,
  `UpdatedDate` datetime DEFAULT NULL,
  `OriginalBat` longtext,
  `TrackedBat` longtext,
  PRIMARY KEY (`_id`),
  UNIQUE KEY `idx_campaign_Name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `link` (
  `_id` int NOT NULL AUTO_INCREMENT,
  `IdCampaign` varchar(50) NOT NULL,
  `Link` varchar(1000) DEFAULT NULL,
  PRIMARY KEY (`_id`),
  UNIQUE KEY `_id_UNIQUE` (`_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `unsubscriptions` (
  `Receipient` varchar(50) NOT NULL,
  `IdCampaign` varchar(50) NOT NULL,
  `Timestamp` datetime NOT NULL,
  PRIMARY KEY (`Receipient`,`IdCampaign`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


