START TRANSACTION;
ALTER TABLE "KnowledgeBaseDocuments" ADD "DocId" text NOT NULL DEFAULT '';

ALTER TABLE "KnowledgeBaseDocuments" ADD "VectorStoreSyncDate" timestamp without time zone NOT NULL DEFAULT TIMESTAMP '-infinity';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251027092904_AddingSyncInfoInKBDocuments', '9.0.10');

COMMIT;

