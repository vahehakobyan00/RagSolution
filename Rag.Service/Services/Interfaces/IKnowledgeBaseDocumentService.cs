using Rag.Service.Dtos;
using RagLib.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Services.Interfaces
{
    public interface IKnowledgeBaseDocumentService
    {
        Task<(KnowledgeBaseDocumentDto? knowledgeBaseDocumentDto, string? errorMessage)> CreateKnowledgeBaseDocumentAsync(CreateKnowledgeBaseDocumentDto dto);
        Task<KnowledgeBaseDocumentDto?> GetKnowledgeBaseDocumentByIdAsync(long id);
        Task<KnowledgeBaseDocumentDto?> EditKnowledgeBaseDocumentAsync(EditKnowledgeBaseDocumentDto dto);
        Task<KnowledgeBaseDocumentsList> GetAllKnowledgeBaseDocumentListAsync(GetAllKnowledgeBaseDocumentsFilter getAllKnowledgeBaseDocumentsFilter);
        Task<Response> DeleteKnowledgeBaseDocumentByIdAsync(long id);
        Task<DataForIngestDto?> GetDataOfFileForIngest(long id);
        Task<Response> UpdateSyncInfoById(long id, string DocId);
        Task<List<DropdownItem>> GetKnowledgeBaseDocumentsItemsForDropdown();
        Task SetFileNamesAsync(List<ChunkDto> chunkDtos);
    }
}
