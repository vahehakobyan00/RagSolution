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
    public class KnowledgeBaseController : BaseController<KnowledgeBaseController>
    {
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly IMapper _mapper;
        public KnowledgeBaseController(IKnowledgeBaseService knowledgeBaseService, IMapper mapper)
        {
            _knowledgeBaseService = knowledgeBaseService;
            _mapper = mapper;
        }


        [HttpPost]
        [Route("CreateKnowledgeBase")]
        public async Task<IActionResult> CreateKnowledgeBase(CreateKnowledgeBaseModel createKnowledgeBaseModel)
        {
            try
            {
                var knowledgeBaseDto = _mapper.Map<KnowledgeBaseDto>(createKnowledgeBaseModel);
                knowledgeBaseDto.CreatedByUserId = UserId;
                CreateEditKnowledgeBaseDto createEditKnowledgeBaseDto = new CreateEditKnowledgeBaseDto()
                {
                    IsEdit = false,
                    KnowledgeBase = knowledgeBaseDto,
                };
                var creationResult = await _knowledgeBaseService.CreateEditKnowledgeBaseAsync(createEditKnowledgeBaseDto);
                if (creationResult.knowledgeBaseDto != null)
                {
                    return Ok(creationResult.knowledgeBaseDto);
                }
                return BadRequest(creationResult.errorMessage);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in CreateKnowledgeBase");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetKnowledgeBaseById")]
        public async Task<IActionResult> GetKnowledgeBaseById(long id)
        {
            try
            {
                var knowledgeBase = await _knowledgeBaseService.GetKnowledgeBaseByIdAsync(id);
                if (knowledgeBase == null)
                {
                    return BadRequest("KnowledgeBase not found");
                }
                return Ok(knowledgeBase);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in GetKnowledgeBaseById");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("EditKnowledgeBase")]
        public async Task<IActionResult> EditKnowledgeBase(EditKnowledgeBaseModel editKnowledgeBaseModel)
        {
            try
            {
                var knowledgeBaseDto = _mapper.Map<KnowledgeBaseDto>(editKnowledgeBaseModel);
                knowledgeBaseDto.UpdatedByUserId = UserId;
                CreateEditKnowledgeBaseDto createEditKnowledgeBaseDto = new CreateEditKnowledgeBaseDto()
                {
                    IsEdit = true,
                    KnowledgeBase = knowledgeBaseDto,
                };
                var editResult = await _knowledgeBaseService.CreateEditKnowledgeBaseAsync(createEditKnowledgeBaseDto);
                if (editResult.knowledgeBaseDto != null)
                {
                    return Ok(editResult.knowledgeBaseDto);
                }
                return BadRequest(editResult.errorMessage);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in EditKnowledgeBase");
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("DeleteKnowledgeBaseById")]
        public async Task<IActionResult> DeleteKnowledgeBaseById(long id)
        {
            try
            {
                var deleteResponse = await _knowledgeBaseService.DeleteKnowledgeBaseByIdAsync(id);
                if (deleteResponse.HasError)
                {
                    return BadRequest(deleteResponse.Message);
                }
                return Ok(deleteResponse.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in DeleteKnowledgeBaseById");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetAllKnowledgeBase")]
        public async Task<IActionResult> GetAllKnowledgeBase(GetAllKnowledgeBasesRequest getAllKnowledgeBaseRequest)
        {
            try
            {
                Log.Information("GetAllKnowledgeBase  for user {UserId}", UserId);
                var getAllKnowledgeBasesFilter = new GetAllKnowledgeBasesFilter()
                {
                    PageIndex = getAllKnowledgeBaseRequest.PageIndex,
                    PageSize = getAllKnowledgeBaseRequest.PageSize,
                    ShowAll = getAllKnowledgeBaseRequest.ShowAll,
                    UserId = UserId,
                    IsExplore = getAllKnowledgeBaseRequest.IsExplore,
                    KnowledgeBasesIdsList = getAllKnowledgeBaseRequest.KnowledgeBasesIdsList
                };
                var knowledgeBasesList = await _knowledgeBaseService.GetAllKnowledgeBaseAsync(getAllKnowledgeBasesFilter);
                return Ok(knowledgeBasesList);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in GetAllKnowledgeBase");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetKnowledgeBaseItemsForDropdown")]
        public async Task<IActionResult> GetKnowledgeBaseItemsForDropdown()
        {
            try
            {
                var resp = await _knowledgeBaseService.GetKnowledgeBaseItemsForDropdownAsync();
                return Ok(resp);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while getting KnowledgeBase Items For Dropdown");
                return BadRequest(ex.Message);
            }
        }
    }
}
