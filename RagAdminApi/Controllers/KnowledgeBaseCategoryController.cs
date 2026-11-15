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
    public class KnowledgeBaseCategoryController : BaseController<KnowledgeBaseCategoryController>
    {
        private readonly IKnowledgeBaseCategoryService _knowledgeBaseCategoryService;
        private readonly IMapper _mapper;
        public KnowledgeBaseCategoryController(IKnowledgeBaseCategoryService knowledgeBaseCategoryService, IMapper mapper)
        {
            _knowledgeBaseCategoryService = knowledgeBaseCategoryService;
            _mapper = mapper;
        }

        [HttpPost]
        [Route("CreateKnowledgeBaseCategory")]
        public async Task<IActionResult> CreateKnowledgeBaseCategory(CreateKnowledgeBaseCategoryModel createKnowledgeBaseCategoryModel)
        {
            try
            {
                var knowledgeBaseCategoryDto = _mapper.Map<KnowledgeBaseCategoryDto>(createKnowledgeBaseCategoryModel);
                knowledgeBaseCategoryDto.CreatedByUserId = UserId;
                CreateEditKnowledgeBaseCategoryDto createEditKnowledgeBaseCategoryDto = new CreateEditKnowledgeBaseCategoryDto()
                {
                    IsEdit = false,
                    KnowledgeBaseCategory = knowledgeBaseCategoryDto,
                };
                var creationResult = await _knowledgeBaseCategoryService.CreateEditKnowledgeBaseCategoryAsync(createEditKnowledgeBaseCategoryDto);
                if (creationResult != null)
                {
                    return Ok(creationResult);
                }
                return BadRequest("Failed to create Knowledge Base Category.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in CreateKnowledgeBaseCategory");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetKnowledgeBaseCategoryById")]
        public async Task<IActionResult> GetKnowledgeBaseCategoryById(long id)
        {
            try
            {
                var knowledgeBaseCategory = await _knowledgeBaseCategoryService.GetKnowledgeBaseCategoryByIdAsync(id);
                if (knowledgeBaseCategory == null)
                {
                    return BadRequest("Knowledge Base Category not found");
                }
                return Ok(knowledgeBaseCategory);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in GetKnowledgeBaseCategoryById");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetAllKnowledgeBaseCategories")]
        public async Task<IActionResult> GetAllKnowledgeBaseCategories(GetAllKnowledgeBaseCategoriesRequest getAllKnowledgeBaseCategoriesRequest)
        {
            try
            {
                Log.Information("GetAllKnowledgeBaseCategories  for user {UserId}", UserId);
                var getAllKnowledgeBaseCategoriesFilter = new GetAllKnowledgeBaseCategoriesFilter()
                {
                    PageIndex = getAllKnowledgeBaseCategoriesRequest.PageIndex,
                    PageSize = getAllKnowledgeBaseCategoriesRequest.PageSize,
                    UserId = UserId,
                    ShowAll = getAllKnowledgeBaseCategoriesRequest.ShowAll,
                    IsExplore = getAllKnowledgeBaseCategoriesRequest.IsExplore,
                    KnowledgeBaseCategoriesIdsList = getAllKnowledgeBaseCategoriesRequest.KnowledgeBaseCategoriesIdsList

                };
                var KnowledgeBaseCategoriesList = await _knowledgeBaseCategoryService.GetAllKnowledgeBaseCategoryAsync(getAllKnowledgeBaseCategoriesFilter);
                return Ok(KnowledgeBaseCategoriesList);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in GetAllKnowledgeBaseCategories");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("EditKnowledgeBaseCategory")]
        public async Task<IActionResult> EditKnowledgeBaseCategory(EditKnowledgeBaseCategoryModel editKnowledgeBaseCategoryModel)
        {
            try
            {
                var knowledgeBaseCategoryDto = _mapper.Map<KnowledgeBaseCategoryDto>(editKnowledgeBaseCategoryModel);
                knowledgeBaseCategoryDto.UpdatedByUserId = "";
                CreateEditKnowledgeBaseCategoryDto createEditKnowledgeBaseCategoryDto = new CreateEditKnowledgeBaseCategoryDto()
                {
                    IsEdit = true,
                    KnowledgeBaseCategory = knowledgeBaseCategoryDto,
                };
                var editResult = await _knowledgeBaseCategoryService.CreateEditKnowledgeBaseCategoryAsync(createEditKnowledgeBaseCategoryDto);
                if (editResult != null)
                {
                    return Ok(editResult);
                }
                return BadRequest("Failed to edit Knowledge Base Category.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in EditKnowledgeBaseCategory");
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("DeleteKnowledgeBaseCategoryById")]
        public async Task<IActionResult> DeleteKnowledgeBaseCategoryById(long id)
        {
            try
            {
                var deleteResponse = await _knowledgeBaseCategoryService.DeleteKnowledgeBaseCategoryByIdAsync(id);
                if (deleteResponse.HasError)
                {
                    return BadRequest(deleteResponse.Message);
                }
                return Ok(deleteResponse.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in DeleteKnowledgeBaseCategoryById");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetKnowledgeBaseCategoryItemsForDropdown")]
        public async Task<IActionResult> GetKnowledgeBaseCategoryItemsForDropdown()
        {
            try
            {
                var resp = await _knowledgeBaseCategoryService.GetKnowledgeBaseCategoryItemsForDropdownAsync();
                return Ok(resp);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while getting KnowledgeBaseCategory Items For Dropdown");
                return BadRequest(ex.Message);
            }
        }
    }
}
