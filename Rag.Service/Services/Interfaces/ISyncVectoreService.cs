using Rag.Service.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Services.Interfaces
{
    public interface ISyncVectoreService
    {
        Task<Response> SyncVectore(long kbDocumentId);
    }
}
