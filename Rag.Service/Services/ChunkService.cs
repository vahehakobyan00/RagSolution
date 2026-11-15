using AutoMapper;
using Rag.Service.Dtos;
using Rag.Service.Services.Interfaces;
using RagLib.Storage;
using Serilog;

namespace Rag.Service.Services
{
    public class ChunkService : IChunkService
    {
        VectorStore _vectorStore;
        private readonly IMapper _mapper;
        private readonly IKnowledgeBaseDocumentService _knowledgeBaseDocumentService;
        public ChunkService(VectorStore vectorStore, IMapper mapper, IKnowledgeBaseDocumentService knowledgeBaseDocumentService)
        {
            _vectorStore = vectorStore;
            _mapper = mapper;
            _knowledgeBaseDocumentService = knowledgeBaseDocumentService;
        }

        public async Task<ChunksList> GetAllChunkAsync(GetAllChunksFilter getAllChunksFilter)
        {
            try
            {
                if (getAllChunksFilter.PageSize == 0)
                    getAllChunksFilter.PageSize = int.MaxValue;
                var chunkGridItems = new List<ChunkGridItem>();
                var result = await _vectorStore.GetChunksByIdsPagedAsync(getAllChunksFilter.ChunkIds, getAllChunksFilter.PageIndex, getAllChunksFilter.PageSize, CancellationToken.None);

                if (result.Chunks != null)
                {
                    await _knowledgeBaseDocumentService.SetFileNamesAsync(result.Chunks);
                    chunkGridItems = _mapper.Map<List<ChunkGridItem>>(result.Chunks);
                    Log.Information("Showing:{filtredCount} records from all {Count} records ", result.Chunks.Count, result.Count);
                    return new ChunksList { Chunks = chunkGridItems, Count = result.Count };
                }

                Log.Information("Get all chunks succeeded: Count = " + 0);

                return new ChunksList { Chunks = new List<ChunkGridItem>(), Count = 0 };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while retrieving Chunks for UserId: {UserId}", getAllChunksFilter.UserId);
                throw;
            }
        }
    }
}
