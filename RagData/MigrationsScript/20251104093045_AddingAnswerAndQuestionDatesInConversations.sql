START TRANSACTION;
ALTER TABLE "Turns" ADD "AnswerDate" timestamp without time zone NOT NULL DEFAULT TIMESTAMP '-infinity';

ALTER TABLE "Turns" ADD "QuestionDate" timestamp without time zone NOT NULL DEFAULT TIMESTAMP '-infinity';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251104093045_AddingAnswerAndQuestionDatesInConversations', '9.0.10');

COMMIT;

