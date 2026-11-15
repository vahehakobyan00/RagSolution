using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Rag.Data.Entity;
using Rag.Data.Repositories.Interfaces;
using Rag.Service.Dtos;
using Rag.Service.Services.Interfaces;
using Serilog;

namespace Rag.Service.Services
{
    public class ServiceAgentService : IServiceAgentService
    {
        private readonly IServiceAgentRepository _serviceAgentRepository;
        private readonly IMapper _mapper;
        private readonly IDeleteValidationService _deleteValidationService;
        public ServiceAgentService(IServiceAgentRepository serviceAgentRepository, IMapper mapper,IDeleteValidationService deleteValidationService)
        {
            _serviceAgentRepository = serviceAgentRepository;   
            _mapper = mapper;
            _deleteValidationService = deleteValidationService;
        }
        public async Task<ServiceAgentDto?> CreateEditServiceAgentAsync(CreateEditServiceAgentDto createEditServiceAgentDto)
        {
            try
            {
                var serviceAgent = _mapper.Map<ServiceAgent>(createEditServiceAgentDto.ServiceAgent);
                if (serviceAgent.IsEnabled)
                {
                    Log.Debug("serviceAgent is marked as Enabled. Checking for existing Enabled");
                    var existingEnabled = await _serviceAgentRepository
                        .SelectAll(x => x.IsEnabled && !x.IsDeleted)
                        .ToListAsync();

                    if (existingEnabled != null)
                    {
                        Log.Information("Existing in use serviceAgent found (Count: {CredentialId}), setting IsEnabled = false", existingEnabled.Count);
                        foreach (var item in existingEnabled)
                        {
                            item.IsEnabled = false;
                            _serviceAgentRepository.Update(item);
                        }                      
                    }
                }
                if (!createEditServiceAgentDto.IsEdit)
                {
                    Log.Information("Creating a new ServiceAgent with data: {@ServiceAgent}", serviceAgent);
                    serviceAgent.CreationDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                    _serviceAgentRepository.Insert(serviceAgent);
                }
                else
                {
                    Log.Information("Editing ServiceAgent with ID: {Id}", serviceAgent.Id);
                    var dbEntity = await _serviceAgentRepository
                        .SelectAll(x => x.Id == serviceAgent.Id)
                        .FirstOrDefaultAsync();
                    if (dbEntity == null)
                    {
                        Log.Warning("ServiceAgent with ID: {Id} not found for update", serviceAgent.Id);
                        return null;
                    }

                    serviceAgent = serviceAgent.ToUpdated(dbEntity);
                    serviceAgent.LastModifiedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                    _serviceAgentRepository.Update(serviceAgent);
                }
               
                await _serviceAgentRepository.SaveChangesAsync();
                Log.Information("ServiceAgent operation successful. ID: {Id}", serviceAgent.Id);
                var serviceAgentDto = _mapper.Map<ServiceAgentDto>(serviceAgent);
                return serviceAgentDto;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while creating/editing ServiceAgent.");
                throw;
            }
        }

        public async Task<Response> DeleteServiceAgentByIdAsync(long id)
        {
            try
            {
                var serviceAgent = await _serviceAgentRepository.FindById(id);
                if (serviceAgent == null)
                {
                    return new Response { HasError = true, Message = "Ther is no such service agent with Id:" + id };
                }
                var isExsit = await _deleteValidationService.IsExsitServiceAgentUnderKnowledgeBase(id);
                if (isExsit)
                {
                    return new Response { HasError = true, Message = "Ther is KnowledgeBase with service agent: " + serviceAgent.Name };
                }
                serviceAgent.IsDeleted = true;
                await _serviceAgentRepository.SaveChangesAsync();
                var dbEntity = await _serviceAgentRepository.FindById(id);
                if (dbEntity.IsDeleted == false)
                {
                    return new Response { HasError = true, Message = "Couldnt delete" };
                }
                Log.Information("Service Agent with Id: {Id} is set deleted", id);
                return new Response { HasError = false, Message = "Succsesfully  deleteed" };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while deleting ServiceAgent.");
                throw;
            }
        }

        public async Task<ServiceAgentsList> GetAllServiceAgentsAsync(GetAllServiceAgentsFilter getAllServiceAgentsFilter)
        {
            try
            {
                if (getAllServiceAgentsFilter.PageSize == 0)
                {
                    getAllServiceAgentsFilter.PageSize = int.MaxValue;
                }
                var skip = getAllServiceAgentsFilter.PageSize * getAllServiceAgentsFilter.PageIndex;
                var serviceAgentGridItems = new List<ServiceAgentGridItem>();
                var result = _serviceAgentRepository.SelectAll(x => (getAllServiceAgentsFilter.ShowAll || x.CreatedByUserId.Equals(getAllServiceAgentsFilter.UserId))
                                                    && !x.IsDeleted).Include(x => x.LlmProvider).AsQueryable();

                var count = result.Count();
                var filtered = await result.OrderByDescending(x => x.Id).Skip(skip).Take(getAllServiceAgentsFilter.PageSize).ToListAsync();
                if (filtered != null)
                {
                    serviceAgentGridItems = _mapper.Map<List<ServiceAgentGridItem>>(filtered);
                    Log.Information("Showing:{filtredCount} records from all {Count} records ", filtered.Count, count);
                    return new ServiceAgentsList { ServiceAgents = serviceAgentGridItems, Count = count };
                }

                Log.Information("Get all Loged in user Service Agents succeeded: Count = " + 0);

                return new ServiceAgentsList { ServiceAgents = new List<ServiceAgentGridItem>(), Count = 0 };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while retrieving ServiceAgents for UserId: {UserId}", getAllServiceAgentsFilter.UserId);
                throw;
            }
        }

        public async Task<ServiceAgentDto?> GetServiceAgentByIdAsync(long id)
        {
            try
            {
                Log.Information("Fetching ServiceAgent with ID: {Id}", id);
                var serviceAgent = await _serviceAgentRepository.FindById(id);
                if (serviceAgent == null)
                {
                    Log.Warning("No ServiceAgent found with ID: {Id}", id);
                    return null;
                }

                if (serviceAgent.IsDeleted)
                {
                    Log.Warning("ServiceAgent with ID: {Id} is marked as deleted.", id);
                    return null;
                }
                var serviceAgentDto = _mapper.Map<ServiceAgentDto>(serviceAgent);
                Log.Information("Successfully fetched ServiceAgent with ID: {Id}", id);
                return serviceAgentDto;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while getting ServiceAgent by id. {id}",id);
                throw;
            }
        }

        public async Task<ServiceAgentConfigDto?> GetServiceAgentConfigByIdAsync(long id)
        {
            Log.Information("Starting GetServiceAgentConfigByIdAsync with Id: {Id}", id);
            try
            {
                var result = await _serviceAgentRepository.FindById(id);

                if (result == null)
                {
                    Log.Warning("No ServiceAgent found with Id: {Id}", id);
                    return null;
                }
                var serviceAgentConfigDto = _mapper.Map<ServiceAgentConfigDto>(result);
                Log.Information("Successfully retrieved and mapped ServiceAgent config. Id: {Id}", id);
                return serviceAgentConfigDto;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while getting ServiceAgent config by Id: {Id}", id);
                throw;
            }
        }

        public async Task<List<DropdownItem>> GetServiceAgentItemsForDropdownAsync()
        {
            try
            {
                var dropDownItems = await _serviceAgentRepository.SelectAll(x => !x.IsDeleted).Select(x => new DropdownItem()
                {
                    Id = x.Id,
                    Name = x.Name,
                }).ToListAsync();
                if(dropDownItems == null)
                {
                    return new List<DropdownItem>();
                }
                return dropDownItems;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while getting ServiceAgent Items For Dropdown");
                throw;
            }
        }

        public async Task<ServiceAgentConfigDto?> GetEnabledServiceAgentAsync()
        {
            try
            {
                var serviceAgentInUse = await _serviceAgentRepository.SelectAll(x => x.IsEnabled && !x.IsDeleted).FirstOrDefaultAsync();
                if (serviceAgentInUse == null)
                {
                    Log.Warning("No ServiceAgent Enabled");
                    return null;
                }
                var serviceAgentInUseDto = _mapper.Map<ServiceAgentConfigDto>(serviceAgentInUse);
                Log.Information("Successfully retrieved and mapped ServiceAgent Enabled config. Id: {Id}", serviceAgentInUse.Id);
                return serviceAgentInUseDto;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while getting ServiceAgent Enabled");
                throw;
            }
        }
    }
}
