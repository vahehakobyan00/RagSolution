using Microsoft.Extensions.Configuration;
using Rag.Service.Dtos;
using Rag.Service.FileServices.Interfaces;
using Rag.Service.Services.Interfaces;
using Serilog;

namespace Rag.Service.Services
{
    public class SyncVectoreService : ISyncVectoreService
    {
        private readonly IKnowledgeBaseDocumentService _knowledgeBaseDocumentService;
        private readonly IIngestService _ingestService;
        private readonly IConfiguration _configuration;
        public SyncVectoreService(IKnowledgeBaseDocumentService knowledgeBaseDocumentService, IIngestService ingestService,IConfiguration configuration)
        {
            _ingestService = ingestService;
            _knowledgeBaseDocumentService = knowledgeBaseDocumentService;
            _configuration = configuration;
        }
        public async Task<Response> SyncVectore(long kbDocumentId)
        {
            Log.Information("SyncVectore started for KB Document Id {KbDocumentId}", kbDocumentId);

            var dataForIngest = await _knowledgeBaseDocumentService.GetDataOfFileForIngest(kbDocumentId);

            if (dataForIngest == null)
            {
                Log.Warning("No ingest data found for KB Document Id: {KbDocumentId}", kbDocumentId);
                return new Response { HasError = true, Message = "Document not found." };
            }

            string docId = string.IsNullOrEmpty(dataForIngest.DocId)
                ? Guid.NewGuid().ToString()
                : dataForIngest.DocId;

            Log.Information("{Action} DocId: {DocId}",
                string.IsNullOrEmpty(dataForIngest.DocId) ? "Generated new" : "Using existing",
                docId);

            try
            {
                string fileStorage = _configuration.GetSection("FileStorage:SavePath").Value;
                var path = fileStorage + $@"//{dataForIngest.FilePath}";

                Log.Information("Starting vector sync for DocId {DocId} at path {Path}", docId, path);
                string extension = Path.GetExtension(dataForIngest.FileName);
                var syncResponse = await _ingestService.IngestFromPathAsync(path, docId,extension);

                Log.Information("Vector store sync completed. DocId={DocId}, SyncDate={SyncDate}",
                    docId, syncResponse.ExcecutionDate);

                var updateResult = await _knowledgeBaseDocumentService.UpdateSyncInfoById(kbDocumentId, docId);

                if (updateResult.HasError)
                {
                    Log.Warning("Sync completed but sync info update failed for KB Id {KbDocumentId}", kbDocumentId);
                }

                return new Response
                {
                    HasError = false,
                    Message = "Vector synchronization completed successfully."
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error syncing vector for KB Document Id {KbDocumentId}", kbDocumentId);
                throw;
            }
        }

    }
}
