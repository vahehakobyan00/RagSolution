START TRANSACTION;
ALTER TABLE "ServiceAgents" ADD "IsEnabled" boolean NOT NULL DEFAULT FALSE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251112152342_AddingIsEnabledBoolToServiceAgent', '9.0.10');

COMMIT;

