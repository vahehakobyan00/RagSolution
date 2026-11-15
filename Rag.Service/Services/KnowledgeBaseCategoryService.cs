using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Rag.Data.Entity;
using Rag.Data.Repositories.Interfaces;
using Rag.Service.Dtos;
using Rag.Service.Services.Interfaces;
using Serilog;

namespace Rag.Service.Services
{
    public class KnowledgeBaseCategoryService : IKnowledgeBaseCategoryService
    {
        private readonly IKnowledgeBaseCategoryRepository _knowledgeBaseCategoryRepository;
        private readonly IMapper _mapper;
        private readonly IDeleteValidationService _deleteValidationService;
        public KnowledgeBaseCategoryService(IKnowledgeBaseCategoryRepository knowledgeBaseCategoryRepository, IMapper mapper, IDeleteValidationService deleteValidationService)
        {
            _knowledgeBaseCategoryRepository = knowledgeBaseCategoryRepository;
            _mapper = mapper;
            _deleteValidationService = deleteValidationService;
        }
        public async Task<KnowledgeBaseCategoryDto?> CreateEditKnowledgeBaseCategoryAsync(CreateEditKnowledgeBaseCategoryDto createEditKnowledgeBaseCategory)
        {
            try
            {
                var knowledgeBaseCategory = _mapper.Map<KnowledgeBaseCategory>(createEditKnowledgeBaseCategory.KnowledgeBaseCategory);
                if (!createEditKnowledgeBaseCategory.IsEdit)
                {
                    Log.Information("Creating a new knowledgeBaseCategory with data: {@KnowledgeBaseCategory}", knowledgeBaseCategory);
                    knowledgeBaseCategory.CreationDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                    _knowledgeBaseCategoryRepository.Insert(knowledgeBaseCategory);
                }
                else
                {
                    Log.Information("Editing knowledgeBaseCategory with ID: {Id}", knowledgeBaseCategory.Id);
                    var dbEntity = await _knowledgeBaseCategoryRepository
                        .SelectAll(x => x.Id == knowledgeBaseCategory.Id)
                        .FirstOrDefaultAsync();
                    if (dbEntity == null)
                    {
                        Log.Warning("knowledgeBaseCategory with ID: {Id} not found for update", knowledgeBaseCategory.Id);
                        return null;
                    }

                    knowledgeBaseCategory = knowledgeBaseCategory.ToUpdated(dbEntity);
                    knowledgeBaseCategory.LastModifiedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                    _knowledgeBaseCategoryRepository.Update(knowledgeBaseCategory);
                }
                await _knowledgeBaseCategoryRepository.SaveChangesAsync();
                Log.Information("KnowledgeBaseCategory operation successful. ID: {Id}", knowledgeBaseCategory.Id);
                var knowledgeBaseCategoryDto = _mapper.Map<KnowledgeBaseCategoryDto>(knowledgeBaseCategory);
                return knowledgeBaseCategoryDto;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while creating/editing knowledgeBaseCategory.");
                throw;
            }
        }

        public async Task<Response> DeleteKnowledgeBaseCategoryByIdAsync(long id)
        {
            try
            {
                var knowledgeBaseCategory = await _knowledgeBaseCategoryRepository.FindById(id);
                if (knowledgeBaseCategory == null)
                {
                    return new Response { HasError = true, Message = "Ther is no such knowledge Base Category with Id:" + id };
                }
                var isExsit = await _deleteValidationService.IsExsistKnowledgeBaseCategoryUnderKnowledgeBaseDocument(id);
                if (isExsit)
                {
                    return new Response { HasError = true, Message = "Ther is Knowledge Base Document with Knowledge Base Category: " + knowledgeBaseCategory.Name };
                }
                knowledgeBaseCategory.IsDeleted = true;
                await _knowledgeBaseCategoryRepository.SaveChangesAsync();
                var dbEntity = await _knowledgeBaseCategoryRepository.FindById(id);
                if (dbEntity.IsDeleted == false)
                {
                    return new Response { HasError = true, Message = "Couldnt delete" };
                }
                Log.Information("Knowledge Base Category with Id: {Id} is set deleted", id);
                return new Response { HasError = false, Message = "Succsesfully  deleteed" };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while deleting knowledge Base Category.");
                throw;
            }
        }

        public async Task<KnowledgeBaseCategoriesList> GetAllKnowledgeBaseCategoryAsync(GetAllKnowledgeBaseCategoriesFilter getAllKnowledgeBaseCategoryFilter)
        {
            try
            {
                if (getAllKnowledgeBaseCategoryFilter.PageSize == 0)
                {
                    getAllKnowledgeBaseCategoryFilter.PageSize = int.MaxValue;
                }
                var skip = getAllKnowledgeBaseCategoryFilter.PageSize * getAllKnowledgeBaseCategoryFilter.PageIndex;
                var knowledgeBaseCategoryGridItems = new List<KnowledgeBaseCategoryGridItem>();
                var idsList = getAllKnowledgeBaseCategoryFilter.KnowledgeBaseCategoriesIdsList ?? new List<long>();
                var result = _knowledgeBaseCategoryRepository.SelectAll(x => (getAllKnowledgeBaseCategoryFilter.ShowAll || x.CreatedByUserId.Equals(getAllKnowledgeBaseCategoryFilter.UserId))
                                                                   &&(!getAllKnowledgeBaseCategoryFilter.IsExplore || idsList.Contains(x.Id) )
                                                                   && !x.IsDeleted).AsQueryable();

                var count = result.Count();
                var filtered = await result.OrderByDescending(x => x.Id).Skip(skip).Take(getAllKnowledgeBaseCategoryFilter.PageSize).ToListAsync();
                if (filtered != null)
                {
                    knowledgeBaseCategoryGridItems = _mapper.Map<List<KnowledgeBaseCategoryGridItem>>(filtered);
                    Log.Information("Showing:{filtredCount} records from all {Count} records ", filtered.Count, count);
                    return new KnowledgeBaseCategoriesList { KnowledgeBaseCategories = knowledgeBaseCategoryGridItems, Count = count };
                }

                Log.Information("Get all Loged in user Knowledge Base Categories succeeded: Count = " + 0);

                return new KnowledgeBaseCategoriesList { KnowledgeBaseCategories = new List<KnowledgeBaseCategoryGridItem>(), Count = 0 };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while retrieving Knowledge Base Categories for UserId: {UserId}", getAllKnowledgeBaseCategoryFilter.UserId);
                throw;
            }
        }

        public async Task<KnowledgeBaseCategoryDto?> GetKnowledgeBaseCategoryByIdAsync(long id)
        {
            try
            {
                Log.Information("Fetching knowledgeBaseCategory with ID: {Id}", id);
                var knowledgeBaseCategory = await _knowledgeBaseCategoryRepository.FindById(id);
                if (knowledgeBaseCategory == null)
                {
                    Log.Warning("No knowledgeBaseCategory found with ID: {Id}", id);
                    return null;
                }

                if (knowledgeBaseCategory.IsDeleted)
                {
                    Log.Warning("knowledgeBaseCategory with ID: {Id} is marked as deleted.", id);
                    return null;
                }
                var knowledgeBaseCategoryDto = _mapper.Map<KnowledgeBaseCategoryDto>(knowledgeBaseCategory);
                Log.Information("Successfully fetched Knowledge Base Category with ID: {Id}", id);
                return knowledgeBaseCategoryDto;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while getting knowledge Base Category by id. {id}", id);
                throw;
            }
        }

        public async Task<List<DropdownItem>> GetKnowledgeBaseCategoryItemsForDropdownAsync()
        {
            try
            {
                var dropDownItems = await _knowledgeBaseCategoryRepository.SelectAll(x => !x.IsDeleted).Select(x => new DropdownItem()
                {
                    Id = x.Id,
                    Name = x.Name,
                }).ToListAsync();
                if (dropDownItems == null)
                {
                    return new List<DropdownItem>();
                }
                return dropDownItems;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while getting Knowledge base category Items For Dropdown");
                throw;
            }
        }

        public async Task<bool> IsExsist(long id)
        {
            try
            {
                var itemId = await _knowledgeBaseCategoryRepository.SelectAll(x => x.Id == id && !x.IsDeleted).Select(x => x.Id).FirstOrDefaultAsync();
                if (itemId == 0)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while checking exsist by id. {id}", id);
                throw;
            }
        }
    }
}
