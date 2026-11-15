using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Wrapper.Interfaces
{
    public interface IIdentityWrapper
    {
        Task<Dictionary<string, string>> GetUserNamesByUserIdsAsync(List<string> userIds);
    }
}
