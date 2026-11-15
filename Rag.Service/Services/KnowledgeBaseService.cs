using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Rag.Data.Entity;
using Rag.Data.Repositories.Interfaces;
using Rag.Service.Dtos;
using Rag.Service.Services.Interfaces;
using Serilog;

namespace Rag.Service.Services
{
    public class KnowledgeBaseService : IKnowledgeBaseService
    {
        private readonly IKnowledgeBaseRepository _knowledgeBaseRepository;
        private readonly IServiceAgentRepository _serviceAgentRepository;
        private readonly IMapper _mapper;
        private readonly IDeleteValidationService _deleteValidationService;
        public KnowledgeBaseService(IKnowledgeBaseRepository knowledgeBaseRepository, IMapper mapper,
            IServiceAgentRepository serviceAgentRepository,IDeleteValidationService deleteValidationService)
        {
            _knowledgeBaseRepository = knowledgeBaseRepository; 
            _mapper = mapper;
            _serviceAgentRepository = serviceAgentRepository;
            _deleteValidationService = deleteValidationService;
        }

        public async Task<(KnowledgeBaseDto? knowledgeBaseDto, string? errorMessage)> CreateEditKnowledgeBaseAsync(CreateEditKnowledgeBaseDto createEditKnowledgeBaseDto)
        {
            try
            {
                var knowledgeBase = _mapper.Map<KnowledgeBase>(createEditKnowledgeBaseDto.KnowledgeBase);
                var serviceAgent = await _serviceAgentRepository.SelectAll(x=>x.Id == createEditKnowledgeBaseDto.KnowledgeBase.ServiceAgentId && !x.IsDeleted).FirstOrDefaultAsync();
                if (serviceAgent == null)
                {
                    Log.Warning("ServiceAgent not found. ServiceAgentId: {ServiceAgentId}",
                       createEditKnowledgeBaseDto.KnowledgeBase.ServiceAgentId);
                    return (null, "ServiceAgent not found.");//new Response { HasError = true, Message = "ServiceAgent not found." };
                }
                if (!createEditKnowledgeBaseDto.IsEdit)
                {
                    Log.Information("Creating a new knowledgeBase with data: {@KnowledgeBase}", knowledgeBase);
                    knowledgeBase.CreationDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                    _knowledgeBaseRepository.Insert(knowledgeBase);
                }
                else
                {
                    Log.Information("Editing knowledgeBase with ID: {Id} by user id {userId}", knowledgeBase.Id,knowledgeBase.UpdatedByUserId);
                    var dbEntity = await _knowledgeBaseRepository
                        .SelectAll(x => x.Id == knowledgeBase.Id)
                        .FirstOrDefaultAsync();
                    if (dbEntity == null)
                    {
                        Log.Warning("KnowledgeBase with ID: {Id} not found for update", knowledgeBase.Id);
                        return (null, "KnowledgeBase not found.");//return new Response { HasError = true, Message = "KnowledgeBase not found." };
                    }
                    knowledgeBase = knowledgeBase.ToUpdated(dbEntity);
                    knowledgeBase.LastModifiedDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                    _knowledgeBaseRepository.Update(knowledgeBase);
                }
                await _knowledgeBaseRepository.SaveChangesAsync();
                Log.Information("KnowledgeBase operation successful. ID: {Id}", knowledgeBase.Id);
                var knowledgeBaseDto = _mapper.Map<KnowledgeBaseDto>(knowledgeBase);
                return (knowledgeBaseDto,null);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while creating/editing KnowledgeBase.");
                throw;
            }
        }

        public async Task<Response> DeleteKnowledgeBaseByIdAsync(long id)
        {
            try
            {
                var knowledgeBase = await _knowledgeBaseRepository.FindById(id);
                if (knowledgeBase == null)
                {
                    return new Response { HasError = true, Message = "Ther is no such knowledge Base with Id:" + id };
                }
                var isExsit = await _deleteValidationService.IsExsistKnowledgeBaseUnderKnowledgeBaseDocument(id);
                if (isExsit)
                {
                    return new Response { HasError = true, Message = "Ther is Knowledge Base document with Knowledge Base: " + knowledgeBase.Name };
                }
                knowledgeBase.IsDeleted = true;
                await _knowledgeBaseRepository.SaveChangesAsync();
                var dbEntity = await _knowledgeBaseRepository.FindById(id);
                if (dbEntity.IsDeleted == false)
                {
                    return new Response { HasError = true, Message = "Couldnt delete" };
                }
                Log.Information("Knowledge Base with Id: {Id} is set deleted", id);
                return new Response { HasError = false, Message = "Succsesfully  deleteed" };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while deleting KnowledgeBase.");
                throw;
            }
        }

        public async Task<KnowledgeBasesList> GetAllKnowledgeBaseAsync(GetAllKnowledgeBasesFilter getAllKnowledgeBaseFilter)
        {
            try
            {
                if (getAllKnowledgeBaseFilter.PageSize == 0)
                {
                    getAllKnowledgeBaseFilter.PageSize = int.MaxValue;
                }
                var skip = getAllKnowledgeBaseFilter.PageSize * getAllKnowledgeBaseFilter.PageIndex;
                var knowledgeBaseGridItems = new List<KnowledgeBaseGridItem>();
                var idsList = getAllKnowledgeBaseFilter.KnowledgeBasesIdsList ?? new List<long>();
                var query = _knowledgeBaseRepository
                    .SelectAll(x =>(getAllKnowledgeBaseFilter.ShowAll || x.CreatedByUserId == getAllKnowledgeBaseFilter.UserId)
                                    && (!getAllKnowledgeBaseFilter.IsExplore || idsList.Contains(x.Id))
                                    && !x.IsDeleted).AsQueryable();

                var count = query.Count();

                // Join with ServiceAgent to include the Name
                var result = await (from kb in query
                                    join sa in _serviceAgentRepository.SelectAll(x => !x.IsDeleted)
                                        on kb.ServiceAgentId equals sa.Id into saGroup
                                    from sa in saGroup.DefaultIfEmpty()
                                    orderby kb.Id descending
                                    select new KnowledgeBaseGridItem
                                    {
                                        Id = kb.Id,
                                        Name = kb.Name,
                                        Description = kb.Description,
                                        ServiceAgentName = sa != null ? sa.Name : null
                                    })
                                    .Skip(skip)
                                    .Take(getAllKnowledgeBaseFilter.PageSize)
                                    .ToListAsync();

                if (result != null)
                {
                    knowledgeBaseGridItems = _mapper.Map<List<KnowledgeBaseGridItem>>(result);
                    Log.Information("Showing:{filtredCount} records from all {Count} records ", result.Count, count);
                    return new KnowledgeBasesList { KnowledgeBases = knowledgeBaseGridItems, Count = count };
                }

                Log.Information("Get all Loged in user Knowledge Base succeeded: Count = " + 0);

                return new KnowledgeBasesList { KnowledgeBases = new List<KnowledgeBaseGridItem>(), Count = 0 };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while retrieving KnowledgeBases for UserId: {UserId}", getAllKnowledgeBaseFilter.UserId);
                throw;
            }
        }

        public async Task<KnowledgeBaseDto?> GetKnowledgeBaseByIdAsync(long id)
        {
            try
            {
                Log.Information("Fetching KnowledgeBase with ID: {Id}", id);
                var knowledgeBase = await _knowledgeBaseRepository.FindById(id);
                if (knowledgeBase == null)
                {
                    Log.Warning("No KnowledgeBase found with ID: {Id}", id);
                    return null;
                }

                if (knowledgeBase.IsDeleted)
                {
                    Log.Warning("KnowledgeBase with ID: {Id} is marked as deleted.", id);
                    return null;
                }
                var knowledgeBaseDto = _mapper.Map<KnowledgeBaseDto>(knowledgeBase);
                Log.Information("Successfully fetched KnowledgeBase with ID: {Id}", id);
                return knowledgeBaseDto;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while getting KnowledgeBase by id. {id}", id);
                throw;
            }
        }

        public async Task<List<DropdownItem>> GetKnowledgeBaseItemsForDropdownAsync()
        {
            try
            {
                var dropDownItems = await _knowledgeBaseRepository.SelectAll(x => !x.IsDeleted).Select(x => new DropdownItem()
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
                Log.Error(ex, "Error occurred while getting Knowledge base Items For Dropdown");
                throw;
            }
        }

        public async Task<bool> IsExsist(long id)
        {
            try
            {
                var itemId = await _knowledgeBaseRepository.SelectAll(x => x.Id == id && !x.IsDeleted).Select(x => x.Id).FirstOrDefaultAsync();
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
