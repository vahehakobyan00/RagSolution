using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rag.Service.Dtos;
using Rag.Service.Services.Interfaces;
using RagAdminApi.Models;
using RagAdminApi.Requests;
using Serilog;

namespace RagAdminApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ServiceAgentController : BaseController<ServiceAgentController>
    {
        private readonly IServiceAgentService _serviceAgentService;
        private readonly IMapper _mapper;
        private readonly IEnumService _enumService;
        public ServiceAgentController(IServiceAgentService serviceAgentService, IMapper mapper, IEnumService enumService)
        {
            _serviceAgentService = serviceAgentService;
            _mapper = mapper;
            _enumService = enumService;
        }

        [HttpPost]
        [Route("CreateServiceAgent")]
        public async Task<IActionResult> CreateServiceAgent(CreateServiceAgentModel createServiceAgentModel)
        {
            try
            {
                var serviceAgentDto = _mapper.Map<ServiceAgentDto>(createServiceAgentModel);
                serviceAgentDto.CreatedByUserId = UserId;
                CreateEditServiceAgentDto createEditServiceAgentDto = new CreateEditServiceAgentDto()
                {
                    IsEdit = false,
                    ServiceAgent = serviceAgentDto,
                };
                var creationResult = await _serviceAgentService.CreateEditServiceAgentAsync(createEditServiceAgentDto);
                if (creationResult != null)
                {
                    return Ok(creationResult);
                }
                return BadRequest("Failed to create Service Agent");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in CreateServiceAgent");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetServiceAgentById")]
        public async Task<IActionResult> GetServiceAgentById(long id)
        {
            try
            {
                var serviceAgent = await _serviceAgentService.GetServiceAgentByIdAsync(id);
                if (serviceAgent == null)
                {
                    return BadRequest("ServiceAgent not found");
                }
                return Ok(serviceAgent);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in GetServiceAgentById");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetAllServiceAgents")]
        public async Task<IActionResult> GetAllServiceAgents(GetAllServiceAgentsRequest getAllServiceAgentsRequest)
        {
            try
            {
                Log.Information("GetAllServiceAgents  for user {UserId}", UserId);
                var getAllServiceAgentsFilter = new GetAllServiceAgentsFilter()
                {
                    PageIndex = getAllServiceAgentsRequest.PageIndex,
                    PageSize = getAllServiceAgentsRequest.PageSize,
                    UserId = UserId,
                    ShowAll = getAllServiceAgentsRequest.ShowAll

                };
                var serviceAgentsList = await _serviceAgentService.GetAllServiceAgentsAsync(getAllServiceAgentsFilter);
                return Ok(serviceAgentsList);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in GetAllServiceAgents");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("EditServiceAgent")]
        public async Task<IActionResult> EditServiceAgent(EditServiceAgentModel editServiceAgentModel)
        {
            try
            {
                var serviceAgentDto = _mapper.Map<ServiceAgentDto>(editServiceAgentModel);
                serviceAgentDto.UpdatedByUserId = "";
                CreateEditServiceAgentDto createEditServiceAgentDto = new CreateEditServiceAgentDto()
                {
                    IsEdit = true,
                    ServiceAgent = serviceAgentDto,
                };
                var editResult = await _serviceAgentService.CreateEditServiceAgentAsync(createEditServiceAgentDto);
                if (editResult != null)
                {
                    return Ok(editResult);
                }
                return BadRequest("Failed to create Service Agent");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in EditServiceAgent");
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("DeleteServiceAgentById")]
        public async Task<IActionResult> DeleteServiceAgentById(long id)
        {
            try
            {
                var deleteResponse = await _serviceAgentService.DeleteServiceAgentByIdAsync(id);
                if (deleteResponse.HasError)
                {
                    return BadRequest(deleteResponse.Message);
                }
                return Ok(deleteResponse.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in DeleteServiceAgentById");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetLlmProviderItemsForDropdown")]
        public IActionResult GetLlmProviderItemsForDropdown()
        {
            try
            {
                var resp = _enumService.CastLlmProviderEnumIntoList();
                Log.Information("Getting Llm Provider items for dropdown succeeded");
                return Ok(resp);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while getting Llm Provider items for dropdown");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetServiceAgentItemsForDropdown")]
        public async Task<IActionResult> GetServiceAgentItemsForDropdown()
        {
            try
            {
                var resp = await _serviceAgentService.GetServiceAgentItemsForDropdownAsync();
                return Ok(resp);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while getting Service Agent Items For Dropdown");
                return BadRequest(ex.Message);
            }
        }
    }
}
