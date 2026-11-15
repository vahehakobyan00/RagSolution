START TRANSACTION;
ALTER TABLE "Conversations" ADD "SessionId" text NOT NULL DEFAULT '';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251027103651_SessionIdAdded', '9.0.10');

COMMIT;

