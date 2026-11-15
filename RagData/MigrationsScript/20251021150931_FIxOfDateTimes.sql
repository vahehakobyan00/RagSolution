START TRANSACTION;
ALTER TABLE "KnowledgeBases" ALTER COLUMN "LastModifiedDate" TYPE timestamp without time zone;

ALTER TABLE "KnowledgeBases" ALTER COLUMN "CreationDate" TYPE timestamp without time zone;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251021150931_FIxOfDateTimes', '9.0.10');

COMMIT;

