using Rag.Service.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Services.Interfaces
{
    public interface IKnowledgeBaseService
    {
        Task<(KnowledgeBaseDto? knowledgeBaseDto, string? errorMessage)> CreateEditKnowledgeBaseAsync(CreateEditKnowledgeBaseDto createEditKnowledgeBaseDto);
        Task<KnowledgeBaseDto?> GetKnowledgeBaseByIdAsync(long id);
        Task<Response> DeleteKnowledgeBaseByIdAsync(long id);
        Task<KnowledgeBasesList> GetAllKnowledgeBaseAsync(GetAllKnowledgeBasesFilter getAllKnowledgeBaseFilter);
        Task<List<DropdownItem>> GetKnowledgeBaseItemsForDropdownAsync();
        Task<bool> IsExsist(long id);
    }
}
