# RagSolution (.NET 8) — PDF/DOCX → Local Embeddings (Ollama) → Postgres/pgvector → RAG API

This solution contains:
- **RagLib** — shared library (text extraction, chunking, embeddings client, pgvector storage)
- **RagIngestWorker** — console app to ingest files from a path into Postgres
- **RagApi** — minimal Web API: `/query` and `/ingest-from-file`
- **load/k6-search.js** — k6 load test hitting `/query`

## Quick start

1. Make sure Postgres (with pgvector) and Ollama are running locally, then pull the model:
```bash
ollama pull bge-m3
```
2. Build & run API (inside repo root):
```bash
dotnet build
dotnet run --project src/RagApi/RagApi.csproj
```

3. Ingest a file:
```bash
dotnet run --project src/RagIngestWorker/RagIngestWorker.csproj --   data/sample.pdf my-doc-id "Host=localhost;Port=5432;Database=ragdb;Username=rag;Password=ragpwd"
```

4. Query:
```bash
curl -X POST http://localhost:8088/query -H "Content-Type: application/json"   -d '{"query":"What does the book say about returns?", "topK":5}'
```

## Environment

- **Ollama** at `http://localhost:11434`
- **Postgres** at `localhost:5432`, DB `ragdb`, user/pass `rag/ragpwd`
- **OpenAI** (optional) — set `OPENAI_API_KEY` for nicer final answers

## Notes
- Default embedding model: `bge-m3` (1024 dims). Change to `nomic-embed-text` (768) if you prefer.
- ANN index: HNSW + cosine. Keep dims ≤ ~1500 for better perf.
- For scanned PDFs, bolt on OCR before ingestion (not included).
