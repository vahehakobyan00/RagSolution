START TRANSACTION;
ALTER TABLE "KnowledgeBaseDocuments" ALTER COLUMN "VectorStoreSyncDate" DROP NOT NULL;

ALTER TABLE "KnowledgeBaseDocuments" ALTER COLUMN "DocId" DROP NOT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251028150828_FixingKBDocumentsSyncInfo', '9.0.10');

COMMIT;

