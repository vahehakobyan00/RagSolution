using Rag.Service.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Services.Interfaces
{
    public interface IServiceAgentService
    {
        Task<ServiceAgentDto?> CreateEditServiceAgentAsync(CreateEditServiceAgentDto createEditServiceAgentDto);
        Task<ServiceAgentDto?> GetServiceAgentByIdAsync(long id);
        Task<ServiceAgentsList> GetAllServiceAgentsAsync(GetAllServiceAgentsFilter getAllServiceAgentsFilter);
        Task<Response> DeleteServiceAgentByIdAsync(long id);
        Task<List<DropdownItem>> GetServiceAgentItemsForDropdownAsync();
        Task<ServiceAgentConfigDto?> GetServiceAgentConfigByIdAsync(long id);
        Task<ServiceAgentConfigDto?> GetEnabledServiceAgentAsync();
    }
}
