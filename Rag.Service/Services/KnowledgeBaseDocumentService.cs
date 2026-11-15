using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Rag.Data.Entity;
using Rag.Data.Repositories.Interfaces;
using Rag.Service.Dtos;
using Rag.Service.Services.Interfaces;
using RagLib.Dtos;
using RagLib.Storage;
using Serilog;

namespace Rag.Service.Services
{
    public class KnowledgeBaseDocumentService : IKnowledgeBaseDocumentService
    {
        private readonly IKnowledgeBaseDocumentRepository _knowledgeBaseDocumentRepository;
        private readonly IKnowledgeBaseCategoryService _knowledgeBaseCategoryService;
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly IMapper _mapper;
        VectorStore _vectorStore;
        public KnowledgeBaseDocumentService(IKnowledgeBaseDocumentRepository knowledgeBaseDocumentRepository,
            IMapper mapper, IKnowledgeBaseService knowledgeBaseService,IKnowledgeBaseCategoryService knowledgeBaseCategoryService,VectorStore vectorStore)
        {
            _knowledgeBaseDocumentRepository = knowledgeBaseDocumentRepository;
            _mapper = mapper;
            _knowledgeBaseService = knowledgeBaseService;
            _knowledgeBaseCategoryService = knowledgeBaseCategoryService;
            _vectorStore = vectorStore;
        }

        public async Task<(KnowledgeBaseDocumentDto? knowledgeBaseDocumentDto, string? errorMessage)> CreateKnowledgeBaseDocumentAsync(CreateKnowledgeBaseDocumentDto dto)
        {
            Log.Information("Starting {MethodName} with DTO: {@Dto}", nameof(CreateKnowledgeBaseDocumentAsync), dto);
            try
            {
                var entity = _mapper.Map<KnowledgeBaseDocument>(dto);
                entity.CreationDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                if (!(await _knowledgeBaseCategoryService.IsExsist(dto.KnowledgeBaseCategoryId)))
                {
                    Log.Warning("KnowledgeBaseCategoryId {CategoryId} does not exist.", dto.KnowledgeBaseCategoryId);
                    return (null, "There is no Knowledge Base Category");
                }
                if (!(await _knowledgeBaseService.IsExsist(dto.KnowledgeBaseId)))
                {
                    Log.Warning("KnowledgeBaseId {KnowledgeBaseId} does not exist.", dto.KnowledgeBaseId);
                    return (null, "There is no Knowledge Base");
                }
                _knowledgeBaseDocumentRepository.Insert(entity);
                await _knowledgeBaseDocumentRepository.SaveChangesAsync();
                Log.Information("Successfully created KnowledgeBaseDocument with ID {Id}", entity.Id);
                var knowledgeBaseDocumentDto = _mapper.Map<KnowledgeBaseDocumentDto>(entity);
                return (knowledgeBaseDocumentDto, null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while creating KnowledgeBaseDocument for DTO {@Dto}", dto);
                throw;
            }
        }

        public async Task<Response> DeleteKnowledgeBaseDocumentByIdAsync(long id)
        {
            try
            {
                var knowledgeBaseDocument = await _knowledgeBaseDocumentRepository.FindById(id);
                if (knowledgeBaseDocument == null)
                {
                    return new Response { HasError = true, Message = "Ther is no such knowledge Base document with Id:" + id };
                }
                knowledgeBaseDocument.IsDeleted = true;
                await _knowledgeBaseDocumentRepository.SaveChangesAsync();
                var dbEntity = await _knowledgeBaseDocumentRepository.FindById(id);
                if (dbEntity.IsDeleted == false)
                {
                    return new Response { HasError = true, Message = "Couldnt delete" };
                }
                Log.Information("Knowledge Base document with Id: {Id} is set deleted", id);
                if (dbEntity.VectorStoreSyncDate.HasValue)
                {
                    var vectorDeleteResult = await _vectorStore.DeleteByDocIdAsync(dbEntity.DocId, CancellationToken.None);
                    Log.Information("Deleted {Count} vector chunks for DocId {DocId}", vectorDeleteResult, dbEntity.DocId);
                }                               
                return new Response { HasError = false, Message = "Succsesfully  deleteed" };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while deleting KnowledgeBase document.");
                throw;
            }
        }

        public async Task<KnowledgeBaseDocumentDto?> EditKnowledgeBaseDocumentAsync(EditKnowledgeBaseDocumentDto dto)
        {
            try
            {
                var knowledgeBaseDocument = _mapper.Map<KnowledgeBaseDocument>(dto);
                Log.Information("Editing knowledgeKnowledgeBaseDocument with ID: {Id}", dto.Id);
                var dbEntity = await _knowledgeBaseDocumentRepository
                    .SelectAll(x => x.Id == dto.Id)
                    .FirstOrDefaultAsync();
                if (dbEntity == null)
                {
                    Log.Warning("knowledgeBaseDocument with ID: {Id} not found for update", dto.Id);
                    return null;
                }
                knowledgeBaseDocument = knowledgeBaseDocument.ToUpdated(dbEntity);
                knowledgeBaseDocument.LastModifiedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                _knowledgeBaseDocumentRepository.Update(dbEntity);
                await _knowledgeBaseDocumentRepository.SaveChangesAsync();
                Log.Information("knowledgeKnowledgeBaseDocument operation successful. ID: {Id}", dto.Id);
                var knowledgeBaseDocumentDto = _mapper.Map<KnowledgeBaseDocumentDto>(knowledgeBaseDocument);
                return knowledgeBaseDocumentDto;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while editing KnowledgeBaseDocument for DTO {@Dto}", dto);
                throw;
            }
        }

        public async Task<KnowledgeBaseDocumentsList> GetAllKnowledgeBaseDocumentListAsync(GetAllKnowledgeBaseDocumentsFilter getAllKnowledgeBaseDocumentsFilter)
        {
            try
            {
                if (getAllKnowledgeBaseDocumentsFilter.PageSize == 0)
                {
                    getAllKnowledgeBaseDocumentsFilter.PageSize = int.MaxValue;
                }
                var skip = getAllKnowledgeBaseDocumentsFilter.PageSize * getAllKnowledgeBaseDocumentsFilter.PageIndex;
                List<long> idsList = new List<long>();                

                idsList = getAllKnowledgeBaseDocumentsFilter.FileItemIdsList ?? new List<long>();

                var result = _knowledgeBaseDocumentRepository.SelectAll(x => (getAllKnowledgeBaseDocumentsFilter.ShowAll || x.CreatedByUserId == getAllKnowledgeBaseDocumentsFilter.UserId)
                                                               && (!getAllKnowledgeBaseDocumentsFilter.IsExplore || idsList.Contains(x.FileItemId))
                                                               && !x.IsDeleted)
                                                              .Include(x=>x.FileItem)
                                                              .Include(x=>x.KnowledgeBase)
                                                              .Include(x=>x.KnowledgeBaseCategory);
                var count = result.Count();
                var filtered = await result.OrderByDescending(x => x.Id).Skip(skip).Take(getAllKnowledgeBaseDocumentsFilter.PageSize).Select(x=>new KnowledgeBaseDocumentGridItem()
                {
                    Id = x.Id,
                    FileName = x.FileItem.Name,
                    FileDescription = x.FileItem.Description,
                    FileSize = x.FileItem.FileSize,
                    KbName = x.KnowledgeBase.Name,
                    KbCategoryName = x.KnowledgeBaseCategory.Name,
                    UploadDate = x.FileItem.CreationDate,
                    VectorStoreSyncDate = x.VectorStoreSyncDate
                }).ToListAsync();
                if (filtered != null)
                {
                    Log.Information("Showing:{filtredCount} records from all {Count} records ", filtered.Count, count);
                    return new KnowledgeBaseDocumentsList { KnowledgeBaseDocuments = filtered, Count = count };
                }

                Log.Information("Get all Loged in user Knowledge Base Documents succeeded: Count = " + 0);

                return new KnowledgeBaseDocumentsList { KnowledgeBaseDocuments = new List<KnowledgeBaseDocumentGridItem>(), Count = 0 };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while retrieving KnowledgeBases documents for UserId: {UserId}", getAllKnowledgeBaseDocumentsFilter.UserId);
                throw;
            }
        }

        public async Task<DataForIngestDto?> GetDataOfFileForIngest(long id)
        {
            Log.Information("Fetching DataForIngest for KB Document Id {Id}", id);

            try
            {
                var result = await _knowledgeBaseDocumentRepository
                    .SelectAll(x => x.Id == id)
                    .Include(x => x.FileItem)
                    .Select(x => new DataForIngestDto
                    {
                        DocId = x.DocId,
                        FilePath = x.FileItem.FilePath,
                        FileName = x.FileItem.Name
                    })
                    .FirstOrDefaultAsync();

                if (result == null)
                {
                    Log.Warning("No data found for KB Document Id {Id}", id);
                    return null;
                }

                Log.Information("DataForIngest found for KB Document Id {Id}", id);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting DataForIngest by Id {Id}", id);
                throw;
            }
        }

        public async Task<KnowledgeBaseDocumentDto?> GetKnowledgeBaseDocumentByIdAsync(long id)
        {
            try
            {
                Log.Debug("Attempting to retrieve KnowledgeBaseDocument with Id {Id} from repository", id);
                var knowledgeBaseDocument = await _knowledgeBaseDocumentRepository.SelectAll(x => x.Id == id).Include(x => x.FileItem).FirstOrDefaultAsync();
                if (knowledgeBaseDocument == null)
                {
                    Log.Warning("No KnowledgeBaseDocument found with Id {Id}", id);
                    return null;
                }               
                var knowledgeBaseDocumentDto = _mapper.Map<KnowledgeBaseDocumentDto>(knowledgeBaseDocument);
                Log.Information("Successfully retrieved  KnowledgeBaseDocument with Id {Id}", id);
                return knowledgeBaseDocumentDto;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while getting KnowledgeBaseDocument by Id {Id}", id);
                throw;
            }
        }

        public async Task<List<DropdownItem>> GetKnowledgeBaseDocumentsItemsForDropdown()
        {
            try
            {
                var dropDownItems = await _knowledgeBaseDocumentRepository.SelectAll(x => !x.IsDeleted).Include(x=>x.FileItem).Select(x => new DropdownItem()
                {
                    Id = x.Id,
                    Name = x.FileItem.Name,
                }).ToListAsync();
                if (dropDownItems == null)
                {
                    return new List<DropdownItem>();
                }
                return dropDownItems;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while getting Knowledge base document Items For Dropdown");
                throw;
            }
        }

        public async Task SetFileNamesAsync(List<ChunkDto> chunkDtos)
        {
            try
            {
                if (chunkDtos.Count == 0)
                {
                    return;
                }
                foreach (var chunkDto in chunkDtos)
                {
                    var fileName = await _knowledgeBaseDocumentRepository.SelectAll(x => x.DocId == chunkDto.DocId).Select(x=>x.FileItem.Name).FirstOrDefaultAsync();
                    chunkDto.FileName = fileName;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while setting file names to chunks");
                throw;
            }
        }

        public async Task<Response> UpdateSyncInfoById(long id, string docId)
        {
            Log.Information("Updating Sync Info for KB Document Id={Id}, DocId={DocId}", id, docId);

            try
            {
                var dbEntity = await _knowledgeBaseDocumentRepository.FindById(id);

                if (dbEntity == null)
                {
                    Log.Warning("KB Document not found for Id {Id}", id);
                    return new Response { HasError = true, Message = "Document not found." };
                }

                dbEntity.VectorStoreSyncDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                dbEntity.DocId = docId;

                _knowledgeBaseDocumentRepository.Update(dbEntity);
                await _knowledgeBaseDocumentRepository.SaveChangesAsync();

                Log.Information("Sync info updated successfully for KB Document Id {Id}", id);

                return new Response { HasError = false, Message = "Completed successfully." };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating Sync Info for KB Document Id {Id}", id);
                return new Response { HasError = true, Message = "Error updating sync info." };
            }
        }

    }
}
