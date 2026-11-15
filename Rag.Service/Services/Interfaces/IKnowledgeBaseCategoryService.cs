using Rag.Service.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Services.Interfaces
{
    public interface IKnowledgeBaseCategoryService
    {
        Task<KnowledgeBaseCategoryDto?> CreateEditKnowledgeBaseCategoryAsync(CreateEditKnowledgeBaseCategoryDto createEditKnowledgeBaseCategory);
        Task<KnowledgeBaseCategoryDto?> GetKnowledgeBaseCategoryByIdAsync(long id);
        Task<KnowledgeBaseCategoriesList> GetAllKnowledgeBaseCategoryAsync(GetAllKnowledgeBaseCategoriesFilter getAllKnowledgeBaseCategoryFilter);
        Task<Response> DeleteKnowledgeBaseCategoryByIdAsync(long id);
        Task<List<DropdownItem>> GetKnowledgeBaseCategoryItemsForDropdownAsync();
        Task<bool> IsExsist(long id);
    }
}
