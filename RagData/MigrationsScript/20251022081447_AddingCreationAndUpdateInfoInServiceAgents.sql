START TRANSACTION;
ALTER TABLE "ServiceAgents" RENAME COLUMN "UserId" TO "CreatedByUserId";

ALTER TABLE "ServiceAgents" ADD "CreationDate" timestamp without time zone NOT NULL DEFAULT TIMESTAMP '-infinity';

ALTER TABLE "ServiceAgents" ADD "LastModifiedDate" timestamp without time zone;

ALTER TABLE "ServiceAgents" ADD "UpdatedByUserId" text;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251022081447_AddingCreationAndUpdateInfoInServiceAgents', '9.0.10');

COMMIT;

