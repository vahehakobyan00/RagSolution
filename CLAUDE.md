# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

A RAG (retrieval-augmented generation) customer-support chatbot. Documents (PDF/DOCX/TXT) are extracted, chunked, embedded with a local Ollama model (`bge-m3`, 1024 dims) and stored in Postgres/pgvector. User questions are embedded, matched by cosine similarity, and answered by an LLM (OpenAI) using the retrieved chunks plus conversation history. Interfaces: a React chat client, a Telegram bot, and an admin API for managing knowledge bases and agents.

`README.md` describes only the original prototype (`src/RagApi`, `src/RagIngestWorker`, .NET 8, port 8088). The main application is now the root-level projects below (.NET 9).

## Commands

```bash
# Backend (from repo root; solution = RagSolution.sln)
dotnet build RagSolution.sln
dotnet run --project RagWebAPI/RagWebAPI.csproj        # chat API, http://localhost:5075 (Swagger in Development)
dotnet run --project RagAdminApi/RagAdminApi.csproj    # admin API, http://localhost:5110
dotnet run --project Rag.TelegramBot/Rag.TelegramBot.csproj

# Frontend (client/, Create React App)
cd client && npm install
npm start                                   # port from client/.env
npm test                                    # Jest watch mode
npm test -- --watchAll=false App.test.js    # single test file, run once
npm run build

# Load test (k6; targets the legacy src/RagApi /query endpoint)
k6 run load/k6-search.js
```

There are no .NET test projects.

### EF Core migrations (Rag.Data)

Per `RagData/MigrationReadMe.txt`, migrations are made in the Visual Studio Package Manager Console, and an incremental SQL script is committed for each one:

1. `Add-Migration <Name> -Context ApplicationDbContext`
2. `Update-Database -Context ApplicationDbContext`
3. `Script-Migration -Output "RagData/MigrationsScript/<Name>.sql" -Context ApplicationDbContext -From <PreviousMigrationName>`

CLI equivalent: `dotnet ef migrations add <Name> --project RagData --startup-project RagAdminApi --context ApplicationDbContext`.

## Architecture

### Project layering

```
RagWebAPI ─┐
RagAdminApi ┼─> Rag.Service ─┬─> Rag.Data (EF Core + Npgsql, repositories)
Rag.TelegramBot ┘            └─> src/RagLib (extraction, chunking, embeddings, VectorStore)
```

- **Rag.Data** (`RagData/`, assembly `Rag.Data`): `ApplicationDbContext`, entities (ServiceAgent, LlmProvider, KnowledgeBase, KnowledgeBaseCategory, KnowledgeBaseDocument, FileItem, Conversation, Turn, TurnFeedback, TelegramConversation), and a repository for each. The connection string is `Rag:ConnectionString` in the host's appsettings. Registered by `AddDataRepositoryServices`.
- **Rag.Service**: business services, DTOs and AutoMapper profiles (`ServiceToDataMappingProfile`). `AddServices(config)` in `Rag.Service/DI/ServiceCollectionExtensions.cs` is the single DI entry point that every host calls. It also registers Rag.Data and RagLib. New services and repositories must be registered there or in `RagData/DI/IServiceCollectionExtension.cs`.
- **RagLib** (`src/RagLib`): `TextExtractorFactory` (chooses an extractor by file extension), `TokenChunker` (cl100k_base, 800 tokens with 120 overlap in `IngestService`), `OllamaEmbeddingClient`, and `VectorStore`. `VectorStore` uses raw Npgsql on the `rag_chunks` table, which has an HNSW cosine index. That table lives outside EF Core. Re-ingesting a `doc_id` deletes its existing chunks first.
- **Hosts**: each API follows Controller → Request/Model → AutoMapper → Service DTO → Repository. RagAdminApi has its own `ApiToServiceMappingProfile`.

### Configuration split

There are two configuration sources:
- **appsettings.json**: `Rag:ConnectionString` (EF database), `Jwt:*`, `FileStorage:SavePath/ViewPath/DownloadPath` (admin uploads), `IdentityApiSettings`, `TelegramBotSettings`, `RagWebApiSettings`.
- **Environment variables**, read in `AddRagLibDI` and again in `RagWebAPI/Program.cs` and `QueryController`: `POSTGRES_CS` (vector store), `OLLAMA_BASE`, `EMBED_MODEL`, `EMBED_DIM`, `OPENAI_API_KEY`, `OPENAI_MODEL`, `MODEL_MAX_TOKENS`, `RESPONSE_RESERVE`. When a variable is unset, a hard-coded LAN default is used.

### Ingestion flow

1. The admin uploads a file through `KnowledgeBaseDocumentController`. `UploaderService`/`FileService` save it under `FileStorage:SavePath` and record a `FileItem`.
2. `SyncVectorController` → `SyncVectoreService.SyncVectore(kbDocumentId)` → `IngestService.IngestFromPathAsync`.
3. The text is extracted (with page spans and images for PDFs), chunked, embedded and upserted into `rag_chunks`.
4. The generated `DocId` and the sync date are written back onto the `KnowledgeBaseDocument`.

### Query flow (RagWebAPI `QueryController`)

- Endpoints: `Query/prompt/stream` and `prompt/streamnew` (NDJSON streaming for the web client), plus `Query/TelegramPrompt` (non-streaming, for the bot). Several older variants remain in the file as commented-out code.
- The LLM credentials and model come from the database. `IServiceAgentService.GetEnabledServiceAgentAsync()` returns the enabled ServiceAgent, and `LlmProviderFactory` turns it into an `ILlmProvider`. OpenAI is the only provider implemented.
- `ContextWindow` (`RagWebAPI/Context`) holds the chat history. It is seeded from the last N DB turns through `HistoryService`, cached per session in a singleton `ConcurrentDictionary<string, ContextWindow>`, and trimmed or summarized to fit `MODEL_MAX_TOKENS - RESPONSE_RESERVE`. Retrieved chunks are added as system context blocks.
- The system prompt (bank customer-support persona, answers grounded in the knowledge base, Markdown output) is a string constant in `QueryController`.
- After the first answer, `TitleGenerator` generates the conversation title.

### Auth

- **RagWebAPI**: `UserIdHeaderMiddleware` requires a GUID `X-User-Id` header on every request and returns 400 if it is missing. Opt an endpoint out with `[SkipUserIdHeader]`. Read the value with `HttpContext.GetUserId()`. `TokenValidationService` validates JWTs issued by the external Identity API. CORS allows only `http://localhost:4000`.
- **RagAdminApi**: JWT bearer (`Jwt:Issuer/Audience/Key`). Calls to the external identity service go through `IdentityWrapper`.
- **Rag.TelegramBot**: a hosted service that long-polls Telegram and forwards each message to RagWebAPI `Query/TelegramPrompt` through `RagWebApiWrapper`, sending `RagWebApiSettings:ApiKey`. It maps Telegram chats to conversations through `TelegramConversationService`.

### Client (`client/`)

CRA + React 19 + antd 5. The chat UI is in `src/pages/Home.js` and `src/components/ConversationsHistory.js`. Backend URLs come from `REACT_APP_API_URL` and `REACT_APP_IDENTITY_URL` in `client/.env`. `client/nginx.conf` and `client/default.conf` are the production nginx configs.

## Other

- `deploy/init.sql` contains database bootstrap SQL. `data/` is where local PDF/DOCX files go for ingestion.
- Logging uses Serilog in every host. RagAdminApi writes rolling files to `Logs/log-.txt` under its base directory.
