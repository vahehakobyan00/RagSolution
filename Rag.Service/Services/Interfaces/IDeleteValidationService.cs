using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Services.Interfaces
{
    public interface IDeleteValidationService
    {
        Task<bool> IsExsitServiceAgentUnderKnowledgeBase(long serviceAgentId);
        Task<bool> IsExsistKnowledgeBaseUnderKnowledgeBaseDocument(long knowledgeBaseId);
        Task<bool> IsExsistKnowledgeBaseCategoryUnderKnowledgeBaseDocument(long knowledgeBaseCategoryId);
    }
}
