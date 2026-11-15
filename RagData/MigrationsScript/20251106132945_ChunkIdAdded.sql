START TRANSACTION;
ALTER TABLE "FileItems" ADD "ChunkId" bigint;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251106132945_ChunkIdAdded', '9.0.10');

COMMIT;

