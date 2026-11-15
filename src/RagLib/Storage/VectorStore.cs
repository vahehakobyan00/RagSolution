using Npgsql;
using NpgsqlTypes;
using Pgvector;
using RagLib.Dtos;
using System.Text.Json;

namespace RagLib.Storage
{
    public sealed class VectorStore
    {
        private readonly NpgsqlDataSource _ds;
        private readonly int _dim;

        public VectorStore(string cs, int dim)
        {
            var b = new NpgsqlDataSourceBuilder(cs);
            b.UseVector();
            _ds = b.Build();
            _dim = dim;
        }

        public async Task EnsureSchemaAsync(CancellationToken ct)
        {
            await using var conn = await _ds.OpenConnectionAsync(ct);
            var sql = $@"
CREATE TABLE IF NOT EXISTS rag_chunks (
    doc_id      text        NOT NULL,
    chunk_id    bigserial   PRIMARY KEY,
    chunk_index int         NOT NULL,
    content     text        NOT NULL,
    metadata    jsonb       NULL,
    embedding   vector({_dim}) NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_rag_chunks_hnsw
  ON rag_chunks USING hnsw (embedding vector_cosine_ops);

CREATE INDEX IF NOT EXISTS idx_rag_chunks_docid
  ON rag_chunks(doc_id, chunk_index);
";
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync(ct);
            await conn.ReloadTypesAsync();
        }

        public async Task<List<long>> UpsertAsync(string docId,IReadOnlyList<(int index, string content, object metadata, float[] embedding)> rows,CancellationToken ct)
        {
            await using var conn = await _ds.OpenConnectionAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            var deleteSql = "DELETE FROM rag_chunks WHERE doc_id = @doc;";
            await using (var deleteCmd = new NpgsqlCommand(deleteSql, conn, tx))
            {
                deleteCmd.Parameters.AddWithValue("doc", docId);
                await deleteCmd.ExecuteNonQueryAsync(ct);
            }

            var sql = @"
INSERT INTO rag_chunks (doc_id, chunk_index, content, metadata, embedding)
VALUES (@doc, @idx, @content, @meta, @emb)
RETURNING chunk_id;
";

            await using var cmd = new NpgsqlCommand(sql, conn, tx);
            cmd.Parameters.Add(new NpgsqlParameter<string>("doc", docId));
            cmd.Parameters.Add(new NpgsqlParameter<int>("idx", 0));
            cmd.Parameters.Add(new NpgsqlParameter<string>("content", ""));
            var pMeta = new NpgsqlParameter("meta", NpgsqlDbType.Jsonb) { Value = DBNull.Value };
            cmd.Parameters.Add(pMeta);
            var pEmb = new NpgsqlParameter<Vector>("emb", new Vector(new float[] { 0f }));
            cmd.Parameters.Add(pEmb);

            var insertedIds = new List<long>();

            foreach (var r in rows)
            {
                cmd.Parameters["doc"].Value = docId;
                cmd.Parameters["idx"].Value = r.index;
                cmd.Parameters["content"].Value = r.content;
                pMeta.Value = JsonSerializer.Serialize(r.metadata);
                pEmb.Value = new Vector(r.embedding);

                var id = (long)await cmd.ExecuteScalarAsync(ct);
                insertedIds.Add(id);
            }

            await tx.CommitAsync(ct);
            return insertedIds;
        }



public sealed record SearchHit(long Id, string Content, double CosSim, string[] Images);

    public async Task<List<SearchHit>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken ct)
    {
        await using var conn = await _ds.OpenConnectionAsync(ct);
        var sql = @"
SELECT  chunk_id,
        content,
        1 - (embedding <=> @q) AS cosine_similarity,
        (metadata->'images')::text AS images_json
FROM    rag_chunks
ORDER BY embedding <=> @q
LIMIT   @k;";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("q", new Vector(queryEmbedding));
        cmd.Parameters.AddWithValue("k", topK);

        var results = new List<SearchHit>(topK);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct))
        {
            var id = rd.GetInt64(0);
            var content = rd.GetString(1);
            var cosSim = rd.GetDouble(2);

            // images_json is a JSON array (or null); parse to string[]
            string[] images = Array.Empty<string>();
            if (!rd.IsDBNull(3))
            {
                var json = rd.GetString(3);            // e.g. ["url1","url2"]
                if (!string.IsNullOrWhiteSpace(json))
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        var list = new List<string>();
                        foreach (var el in doc.RootElement.EnumerateArray())
                            if (el.ValueKind == JsonValueKind.String)
                                list.Add(el.GetString()!);
                        images = list.ToArray();
                    }
                }
            }

            results.Add(new SearchHit(id, content, cosSim, images));
        }
        return results;
    }



    public async Task<Dictionary<long, string>> GetDocIdsByChunkIdsAsync(IEnumerable<long> chunkIds, CancellationToken ct)
        {
            if (chunkIds == null || !chunkIds.Any())
                return new Dictionary<long, string>();

            await using var conn = await _ds.OpenConnectionAsync(ct);

            var sql = @"SELECT chunk_id, doc_id
                FROM rag_chunks
                WHERE chunk_id = ANY(@ids);";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("ids", chunkIds.ToArray());

            var result = new Dictionary<long, string>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var chunkId = reader.GetInt64(0);
                var docId = reader.GetString(1);
                if (!result.ContainsKey(chunkId))
                    result[chunkId] = docId;
            }

            return result;
        }

        public async Task<(List<ChunkDto> Chunks, int Count)> GetChunksByIdsPagedAsync(
    IEnumerable<long>? chunkIds,
    int pageIndex,
    int pageSize,
    CancellationToken ct)
        {
            await using var conn = await _ds.OpenConnectionAsync(ct);

            var skip = pageSize * pageIndex;

            string sql;
            var cmd = new NpgsqlCommand();

            if (chunkIds != null && chunkIds.Any())
            {
                sql = @"
            SELECT doc_id, chunk_id, content,
                   (SELECT COUNT(*) FROM rag_chunks WHERE chunk_id = ANY(@ids)) AS total_count
            FROM rag_chunks
            WHERE chunk_id = ANY(@ids)
            ORDER BY chunk_id DESC
            OFFSET @skip LIMIT @take;
        ";

                cmd.Parameters.AddWithValue("ids", chunkIds.ToArray());
            }
            else
            {
                sql = @"
            SELECT doc_id, chunk_id, content,
                   (SELECT COUNT(*) FROM rag_chunks) AS total_count
            FROM rag_chunks
            ORDER BY chunk_id DESC
            OFFSET @skip LIMIT @take;
        ";
            }

            cmd.Connection = conn;
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("skip", skip);
            cmd.Parameters.AddWithValue("take", pageSize);

            var chunks = new List<ChunkDto>();
            int count = 0;

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                chunks.Add(new ChunkDto
                {
                    DocId = reader.GetString(0),
                    ChunkId = reader.GetInt64(1),
                    Content = reader.GetString(2)
                });

                if (count == 0)
                    count = reader.GetInt32(3);
            }

            return (chunks, count);
        }


        public async Task<int> DeleteByDocIdAsync(string docId, CancellationToken ct)
        {
            await using var conn = await _ds.OpenConnectionAsync(ct);
            var sql = "DELETE FROM rag_chunks WHERE doc_id = @doc;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("doc", docId);

            var affected = await cmd.ExecuteNonQueryAsync(ct);

            return affected;
        }
        public async Task<Dictionary<string, List<string>>> GetChunkIdsByDocIdsAsync(IEnumerable<string> docIds, CancellationToken ct)
        {
            if (docIds == null || !docIds.Any())
                return new Dictionary<string, List<string>>();

            await using var conn = await _ds.OpenConnectionAsync(ct);

            var sql = @"
        SELECT doc_id, chunk_id
        FROM rag_chunks
        WHERE doc_id = ANY(@docs);
    ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("docs", docIds.ToArray());

            var result = new Dictionary<string, List<string>>();

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var docId = reader.GetString(0);
                var chunkId = reader.GetInt64(1).ToString();

                if (!result.TryGetValue(docId, out var list))
                {
                    list = new List<string>();
                    result[docId] = list;
                }

                list.Add(chunkId);
            }

            return result;
        }

    }
}
