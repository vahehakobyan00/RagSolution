START TRANSACTION;
CREATE INDEX "IX_ServiceAgents_LlmProviderId" ON "ServiceAgents" ("LlmProviderId");

ALTER TABLE "ServiceAgents" ADD CONSTRAINT "FK_ServiceAgents_LlmProviders_LlmProviderId" FOREIGN KEY ("LlmProviderId") REFERENCES "LlmProviders" ("Id") ON DELETE CASCADE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251024105855_AddinRelationInServiceAgentForLlmProvider', '9.0.10');

COMMIT;

