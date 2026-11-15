using Microsoft.EntityFrameworkCore;
using Rag.Data.Repositories.Interfaces;
using Rag.Service.Services.Interfaces;
using Serilog;

namespace Rag.Service.Services
{
    public class DeleteValidationService : IDeleteValidationService
    {
        private readonly IKnowledgeBaseRepository _knowledgeBaseRepository;
        private readonly IKnowledgeBaseDocumentRepository _knowledgeBaseDocumentRepository;
        public DeleteValidationService(IKnowledgeBaseRepository knowledgeBaseRepository, IKnowledgeBaseDocumentRepository knowledgeBaseDocumentRepository)
        {
            _knowledgeBaseRepository = knowledgeBaseRepository;
            _knowledgeBaseDocumentRepository = knowledgeBaseDocumentRepository;
        }

        public async Task<bool> IsExsistKnowledgeBaseCategoryUnderKnowledgeBaseDocument(long knowledgeBaseCategoryId)
        {
            try
            {
                var knowledgeBaseDocument = await _knowledgeBaseDocumentRepository.SelectAll(x => x.KnowledgeBaseCategoryId == knowledgeBaseCategoryId && !x.IsDeleted).FirstOrDefaultAsync();
                if (knowledgeBaseDocument == null)
                {
                    Log.Information("No KnowledgeBaseDocument found for KnowledgeBaseCategoryId {KnowledgeBaseCategoryId}.", knowledgeBaseCategoryId);
                    return false;
                }
                Log.Information("Found KnowledgeBaseDocument for KnowledgeBaseCategoryId {KnowledgeBaseCategoryId}.", knowledgeBaseCategoryId);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred detecting knowledge Base Category under KnowledgeBaseDocument");
                throw;
            }
        }

        public async Task<bool> IsExsistKnowledgeBaseUnderKnowledgeBaseDocument(long knowledgeBaseId)
        {
            try
            {
                var knowledgeBaseDocument =await _knowledgeBaseDocumentRepository.SelectAll(x => x.KnowledgeBaseId == knowledgeBaseId && !x.IsDeleted).FirstOrDefaultAsync();
                if(knowledgeBaseDocument == null)
                {
                    Log.Information("No KnowledgeBaseDocument found for KnowledgeBaseId {KnowledgeBaseId}.", knowledgeBaseId);
                    return false;
                }
                Log.Information("Found KnowledgeBaseDocument for KnowledgeBaseId {KnowledgeBaseId}.", knowledgeBaseId);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred detecting knowledge Base  under KnowledgeBaseDocument");
                throw;
            }
        }

        public async Task<bool> IsExsitServiceAgentUnderKnowledgeBase(long serviceAgentId)
        {
            try
            {
                var knowledgeBase = await _knowledgeBaseRepository.SelectAll(x => x.ServiceAgentId == serviceAgentId && !x.IsDeleted).FirstOrDefaultAsync();
                if (knowledgeBase == null)
                {
                    Log.Information("No KnowledgeBase found for ServiceAgentId {ServiceAgentId}.", serviceAgentId);
                    return false;
                }
                Log.Information("Found KnowledgeBase for ServiceAgentId {ServiceAgentId}.", serviceAgentId);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred detecting Service agent under KnowledgeBase");
                throw;
            }
        }
    }
}
