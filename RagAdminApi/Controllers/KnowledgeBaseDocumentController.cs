using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rag.Service.Dtos;
using Rag.Service.FileServices.Interfaces;
using Rag.Service.FileServices.Requests;
using Rag.Service.Services.Interfaces;
using RagAdminApi.Models;
using RagAdminApi.Requests;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RagAdminApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class KnowledgeBaseDocumentController : BaseController<KnowledgeBaseDocumentController>
    {
        private readonly IKnowledgeBaseDocumentService _knowledgeBaseDocumentService;
        private readonly IMapper _mapper;
        private readonly IFileService _fileService;
        private readonly IConfiguration _configuration;
        public KnowledgeBaseDocumentController(IKnowledgeBaseDocumentService knowledgeBaseDocumentService,IMapper mapper,
            IFileService fileService, IConfiguration configuration)
        {
           _knowledgeBaseDocumentService = knowledgeBaseDocumentService;
            _mapper = mapper;
            _fileService = fileService;
            _configuration = configuration; 
        }

        [HttpPost]
        [Route("CreateKnowledgeBaseDocument")]
        public async Task<IActionResult> CreateKnowledgeBaseDocument([FromForm] CreateKnowledgeBaseDocumentModel createKnowledgeBaseDocumentModel)
        {
            try
            {
                var options = new JsonSerializerOptions();
                options.PropertyNameCaseInsensitive = true;
                options.Converters.Add(new JsonStringEnumConverter());
                CreateKnowledgeBaseDocumentDto? dataBody = JsonSerializer.Deserialize<CreateKnowledgeBaseDocumentDto>(createKnowledgeBaseDocumentModel.dataContent ?? string.Empty, options);
                if (createKnowledgeBaseDocumentModel.File != null)
                {
                    var fileInfo = await _fileService.UploadFileAsync(new FileUploadRequest
                    {
                        CreatorId = UserId, //"dd5de041-888a-4c19-b230-0f1377ba83b8",
                        file = createKnowledgeBaseDocumentModel.File,
                        Description = dataBody.Description
                    });
                    dataBody!.FileItemId = fileInfo.Id;
                }
                else
                {
                    return BadRequest();
                }                
                dataBody.CreatedByUserId = UserId;
                var resp = await _knowledgeBaseDocumentService.CreateKnowledgeBaseDocumentAsync(dataBody);
                if (resp.errorMessage == null)
                {                   
                    return Ok(resp.knowledgeBaseDocumentDto);
                }
               return BadRequest(resp.errorMessage);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while creating Knowledge Base document");

                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetKnowledgeBaseDocumentById")]
        public async Task<IActionResult> GetKnowledgeBaseDocumentById(long id)
        {
            try
            {
                var resp = await _knowledgeBaseDocumentService.GetKnowledgeBaseDocumentByIdAsync(id);
                Log.Information("Getting KnowledgeBaseDocument by id succeeded: KnowledgeBaseDocumentId = " + id);

                if (resp is not null)
                {
                    resp.FilePath = _configuration.GetSection("FileStorage:ViewPath").Value + $@"//{resp.FileItem.FilePath}";
                }
                return Ok(resp);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while getting group by Id: GroupId = " + id);

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("EditKnowledgeBaseDocument")]
        public async Task<IActionResult> EditKnowledgeBaseDocument([FromForm] EditKnowledgeBaseDocumentModel editKnowledgeBaseDocumentModel)
        {
            try
            {
                string errorMessage = "";

                if (editKnowledgeBaseDocumentModel != null)
                {
                    var options = new JsonSerializerOptions();
                    options.PropertyNameCaseInsensitive = true;
                    options.Converters.Add(new JsonStringEnumConverter());
                    EditKnowledgeBaseDocumentDto? dataBody = JsonSerializer.Deserialize<EditKnowledgeBaseDocumentDto>(editKnowledgeBaseDocumentModel.dataContent ?? string.Empty, options);

                    if (dataBody!.Id > 0)
                    {
                        if (editKnowledgeBaseDocumentModel.File != null)
                        {
                            var fileInfo = await _fileService.UploadFileAsync(new FileUploadRequest
                            {
                                CreatorId = UserId, 
                                file = editKnowledgeBaseDocumentModel.File,
                                Description = dataBody.Description,
                            });
                            dataBody.FileItemId = fileInfo.Id;
                        }
                        else
                        {
                            var fileItemUpdateResult = await _fileService.UpdateFileDescriptionById(dataBody.FileItemId, dataBody.Description);
                            if (fileItemUpdateResult.HasError)
                            {
                                return BadRequest(fileItemUpdateResult.Message);
                            }
                        }
                        var editResponse = await _knowledgeBaseDocumentService.EditKnowledgeBaseDocumentAsync(dataBody);
                        return Ok(editResponse);
                    }
                    else
                    {
                        errorMessage = "Error while editing Knowledge Base Document: Invalid Id";

                        Log.Error(errorMessage);
                    }
                }
                else
                {
                    errorMessage = "Error while editing group: Knowledge Base Document is null";

                    Log.Error(errorMessage);
                }

                return BadRequest(errorMessage);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while editing Knowledge Base Document");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("GetAllKnowledgeBaseDocuments")]
        public async Task<IActionResult> GetAllKnowledgeBaseDocuments(GetAllKnowledgeBaseDocumentsRequest getAllKnowledgeBaseDocumentsRequest)
        {
            try
            {
                Log.Information("GetAllKnowledgeBaseDocuments  for user {UserId}",UserId);
                var getAllKnowledgeBaseDocumentsFilter = new GetAllKnowledgeBaseDocumentsFilter()
                {
                    PageIndex = getAllKnowledgeBaseDocumentsRequest.PageIndex,
                    PageSize = getAllKnowledgeBaseDocumentsRequest.PageSize,
                    ShowAll = getAllKnowledgeBaseDocumentsRequest.ShowAll,
                    UserId = UserId,
                    IsExplore = getAllKnowledgeBaseDocumentsRequest.IsExplore,
                    FileItemIdsList = getAllKnowledgeBaseDocumentsRequest.FileItemIdsList,
                };
                var knowledgeBaseDocumentsList = await _knowledgeBaseDocumentService.GetAllKnowledgeBaseDocumentListAsync(getAllKnowledgeBaseDocumentsFilter);
                return Ok(knowledgeBaseDocumentsList);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in GetAllKnowledgeBaseDocuments");
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete]
        [Route("DeleteKnowledgeBaseDocumentById")]
        public async Task<IActionResult> DeleteKnowledgeBaseDocumentById(long id)
        {
            try
            {
                var deleteResponse = await _knowledgeBaseDocumentService.DeleteKnowledgeBaseDocumentByIdAsync(id);
                if (deleteResponse.HasError)
                {
                    return BadRequest(deleteResponse.Message);
                }
                return Ok(deleteResponse.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in DeleteKnowledgeBaseDocumentById");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetKnowledgeBaseDocumentsItemsForDropdown")]
        public async Task<IActionResult> GetKnowledgeBaseDocumentsItemsForDropdown()
        {
            try
            {
                var resp = await _knowledgeBaseDocumentService.GetKnowledgeBaseDocumentsItemsForDropdown();
                return Ok(resp);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while getting KnowledgeBase documents Items For Dropdown");
                return BadRequest(ex.Message);
            }
        }
    }
}
