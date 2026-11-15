using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Rag.Data.Repositories.Interfaces;
using Rag.Service.Dtos;
using Rag.Service.Enums;
using Rag.Service.Services.Interfaces;
using Rag.Service.Wrapper.Interfaces;
using RagLib.Storage;
using Serilog;

namespace Rag.Service.Services
{
    public class ConversationStatisticsService : IConversationStatisticsService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IIdentityWrapper _identityWrapper;
        VectorStore _store;
        private readonly IKnowledgeBaseDocumentRepository _knowledgeBaseDocumentRepository;
        private readonly IKnowledgeBaseCategoryRepository _knowledgeBaseCategoryRepository;
        private readonly IKnowledgeBaseRepository _knowledgeBaseRepository;
        private readonly IFileItemRepository _fileItemRepository;
        private readonly IMapper _mapper;
        private readonly ITurnRepository _turnRepository;
        public ConversationStatisticsService(IConversationRepository conversationRepository, VectorStore store,
            IKnowledgeBaseDocumentRepository knowledgeBaseDocumentRepository, IIdentityWrapper identityWrapper, 
            IKnowledgeBaseCategoryRepository knowledgeBaseCategoryRepository, IKnowledgeBaseRepository knowledgeBaseRepository, 
            IFileItemRepository fileItemRepository,IMapper mapper,ITurnRepository turnRepository)
        {
            _conversationRepository = conversationRepository;
            _store = store;
            _knowledgeBaseDocumentRepository = knowledgeBaseDocumentRepository;
            _identityWrapper = identityWrapper;
            _knowledgeBaseCategoryRepository = knowledgeBaseCategoryRepository;
            _knowledgeBaseRepository = knowledgeBaseRepository;
            _fileItemRepository = fileItemRepository;
            _mapper = mapper;
            _turnRepository = turnRepository;
        }

        public async Task<ConversationsStatisticsList> GetAllConversationsStatisticsAsync(GetAllConversationsStatisticsFilter filter)
        {
            Log.Information("Starting GetAllConversationsStatisticsAsync with {@Filter}", filter);

            filter.PageSize = filter.PageSize == 0 ? int.MaxValue : filter.PageSize;

            var (conversationDtos, totalCount) = await GetConversationsStatisticsAsync(filter);

            if (!conversationDtos.Any())
            {
                Log.Information("No conversations found for given filter.");
                return new ConversationsStatisticsList
                {
                    Conversations = new List<ConversationStatisticsGridItem>(),
                    Count = 0
                };
            }

            var allChunkIds = ExtractAllChunkIds(conversationDtos, c => c.ChunkIds);

            var chunkToDoc = await _store.GetDocIdsByChunkIdsAsync(allChunkIds, CancellationToken.None);
            var docIds = chunkToDoc.Values.Distinct().ToList();

            var kbDocs = await LoadKnowledgeBaseDocsAsync(docIds);


            var userIds = conversationDtos.Select(c => c.UserId).Distinct().ToList();
            var lookupData = await LoadLookupDataAsync(kbDocs, userIds);

            var items = conversationDtos
                .Select(conv => BuildConversationItem(conv, chunkToDoc, kbDocs, lookupData))
                .ToList();

            Log.Information("Successfully processed {ConversationCount} conversations", items.Count);

            return new ConversationsStatisticsList
            {
                Conversations = items,
                Count = totalCount
            };
        }

        #region Private Helpers

        private async Task<(List<ConversationsStatisticsDto> Dtos, int Count)> GetConversationsStatisticsAsync(GetAllConversationsStatisticsFilter filter)
        {
            var query = _conversationRepository
                .SelectAll(x => x.Turns.Count > 0)
                .AsQueryable();

            if (filter.MinInteractionsCount is not null)
                query = query.Where(x => x.Turns.Count > filter.MinInteractionsCount);

            // Knowledge Base filtering
            if (filter.KnowledgeBaseId is not null ||
                filter.KnowledgeBaseCategoryId is not null ||
                filter.KnowledgeBaseDocumentId is not null)
            {
                var kbDocsQuery = _knowledgeBaseDocumentRepository
                    .SelectAll(x => !x.IsDeleted)
                    .AsQueryable();

                if (filter.KnowledgeBaseId is not null)
                    kbDocsQuery = kbDocsQuery.Where(d => d.KnowledgeBaseId == filter.KnowledgeBaseId);

                if (filter.KnowledgeBaseCategoryId is not null)
                    kbDocsQuery = kbDocsQuery.Where(d => d.KnowledgeBaseCategoryId == filter.KnowledgeBaseCategoryId);

                if (filter.KnowledgeBaseDocumentId is not null)
                    kbDocsQuery = kbDocsQuery.Where(d => d.Id == filter.KnowledgeBaseDocumentId);

                var matchingDocIds = await kbDocsQuery
                    .Select(d => d.DocId)
                    .Where(id => id != null)
                    .ToListAsync();

                // Use _store to find all chunk IDs belonging to these docs
                var docToChunk = await _store.GetChunkIdsByDocIdsAsync(matchingDocIds,CancellationToken.None);
                var matchingChunkIds = docToChunk.Values.SelectMany(c => c).Distinct().ToList();

                query = query.Where(conv =>
                    conv.Turns.Any(t => t.RetrievedChunkIds.Any(cid => matchingChunkIds.Contains(cid)))
                );
            }

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.CreatedUtc)
                .Skip(filter.PageSize * filter.PageIndex)
                .Take(filter.PageSize)
                .Select(x => new ConversationsStatisticsDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    UserId = x.UserId,
                    CreatedUtc = x.CreatedUtc,
                    InteractionsCount = x.Turns.Count,
                    ChunkIds = x.Turns.SelectMany(t => t.RetrievedChunkIds).ToList()
                })
                .ToListAsync();

            return (data, totalCount);
        }

        private async Task<List<KnowledgeBaseDocumentDto>> LoadKnowledgeBaseDocsAsync(List<string> docIds)
        {
            return await _knowledgeBaseDocumentRepository
                .SelectAll(x => docIds.Contains(x.DocId) && !x.IsDeleted && !x.KnowledgeBase.IsDeleted && !x.KnowledgeBaseCategory.IsDeleted)
                .Select(x => new KnowledgeBaseDocumentDto
                {
                    DocId = x.DocId!,
                    FileItemId = x.FileItemId,
                    KnowledgeBaseId = x.KnowledgeBaseId,
                    KnowledgeBaseCategoryId = x.KnowledgeBaseCategoryId
                })
                .ToListAsync();
        }

        private async Task<(Dictionary<long, string> KbNames,
                            Dictionary<long, string> CategoryNames,
                            Dictionary<long, string> DocumentNames,
                            Dictionary<string, string> UserNames)>
            LoadLookupDataAsync(List<KnowledgeBaseDocumentDto> kbDocs, List<string> userIds)
        {
            var kbIds = kbDocs.Select(x => x.KnowledgeBaseId).Distinct().ToList();
            var categoryIds = kbDocs.Select(x => x.KnowledgeBaseCategoryId).Distinct().ToList();
            var fileIds = kbDocs.Select(x => x.FileItemId).Distinct().ToList();

            var kbTask = await _knowledgeBaseRepository
                .SelectAll(x => kbIds.Contains(x.Id) && !x.IsDeleted)
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var categoryTask = await _knowledgeBaseCategoryRepository
                .SelectAll(x => categoryIds.Contains(x.Id) && !x.IsDeleted)
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var docTask = await _fileItemRepository
                .SelectAll(x => fileIds.Contains(x.Id) && !x.IsDeleted)
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var userTask = await _identityWrapper.GetUserNamesByUserIdsAsync(userIds);

            return (kbTask, categoryTask, docTask, userTask);
        }

        private ConversationStatisticsGridItem BuildConversationItem(
            ConversationsStatisticsDto conv,
            Dictionary<long, string> chunkToDoc,
            List<KnowledgeBaseDocumentDto> kbDocs,
            (Dictionary<long, string> KbNames,
             Dictionary<long, string> CategoryNames,
             Dictionary<long, string> DocumentNames,
             Dictionary<string, string> UserNames) lookupData)
        {
            var convChunkIds = conv.ChunkIds
                .Select(id => long.TryParse(id, out var parsed) ? parsed : (long?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var convDocIds = convChunkIds
                .Where(chunkToDoc.ContainsKey)
                .Select(id => chunkToDoc[id])
                .Distinct()
                .ToList();

            var convKbDocs = convDocIds
                .Where(docId => kbDocs.Any(k => k.DocId == docId))
                .SelectMany(docId => kbDocs.Where(k => k.DocId == docId))
                .ToList();

            var kbIds = convKbDocs.Select(x => x.KnowledgeBaseId).Distinct().ToList();
            var categoryIds = convKbDocs.Select(x => x.KnowledgeBaseCategoryId).Distinct().ToList();
            var fileIds = convKbDocs.Select(x => x.FileItemId).Distinct().ToList();

            string kbUsedStr = GetDisplayValue(kbIds, lookupData.KbNames);
            string categoryUsedStr = GetDisplayValue(categoryIds, lookupData.CategoryNames);
            string docsUsedStr = GetDisplayValue(fileIds, lookupData.DocumentNames);

            lookupData.UserNames.TryGetValue(conv.UserId, out var name);
            string userName = string.IsNullOrEmpty(name) ? "Unknown User" : name;

            return new ConversationStatisticsGridItem
            {
                Id = conv.Id,
                Title = conv.Title,
                UserName = userName,
                CreationDate = conv.CreatedUtc,
                InteractionsCount = conv.InteractionsCount,
                ChunksRetrievedCount = convChunkIds.Count,

                KnowledgeBaseIds = kbIds,
                KnowledgeBaseCategoryIds = categoryIds,
                DocumentIds = fileIds,
                ChunkIds = convChunkIds,

                KnowledgeBasesUsed = kbUsedStr,
                KnowledgeBaseCategoriesUsed = categoryUsedStr,
                DocumentsUsed = docsUsedStr
            };
        }
        #endregion

        public async Task<ConversationStatisticsDetailsList> GetConversationDetailsByIdAsync(GetAllConversationsStatisticsDetailsFilter filter)
        {
            try
            {
                if (filter.PageSize <= 0)
                    filter.PageSize = int.MaxValue;

                var conversationDto = await GetConversationByIdAsync(filter);

                if (conversationDto == null)
                    return new ConversationStatisticsDetailsList();

                var allChunkIds = ExtractAllChunkIds(conversationDto.Turns, t => t.ChunkIds);

                var chunkToDoc = await _store.GetDocIdsByChunkIdsAsync(allChunkIds, CancellationToken.None);
                var docIds = chunkToDoc.Values.Distinct().ToList();

                var kbDocs = await LoadKnowledgeBaseDocsAsync(docIds);
                var lookupData = await LoadLookupDataAsync(kbDocs);

                var allItems = conversationDto.Turns
                    .SelectMany(turn => BuildTurnItems(conversationDto.Title, turn, chunkToDoc, kbDocs, lookupData))
                    .ToList();

                var totalCount = conversationDto.TurnsCount * 2;

                return new ConversationStatisticsDetailsList
                {
                    Count = totalCount,
                    ConversationStatisticsDetails = allItems
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in GetConversationDetailsByIdAsync");
                throw;
            }
        }

        #region Private Helpers Of GetConversationDetailsByIdAsync


        private async Task<ConversationStatisticsDetailsDto?> GetConversationByIdAsync(GetAllConversationsStatisticsDetailsFilter filter)
        {
            var skip = filter.PageSize * filter.PageIndex;

            var searchText = string.IsNullOrWhiteSpace(filter.TextSearchInMessages)
                ? null
                : filter.TextSearchInMessages.Trim().ToLowerInvariant();

            HashSet<string>? matchingChunkIds = null;

            bool kbFilterExists =
                filter.KnowledgeBaseId is not null ||
                filter.KnowledgeBaseCategoryId is not null ||
                filter.KnowledgeBaseDocumentId is not null;

            if (kbFilterExists)
            {
                var kbDocsQuery = _knowledgeBaseDocumentRepository
                    .SelectAll(x => !x.IsDeleted);

                if (filter.KnowledgeBaseId != null)
                    kbDocsQuery = kbDocsQuery.Where(x => x.KnowledgeBaseId == filter.KnowledgeBaseId);

                if (filter.KnowledgeBaseCategoryId != null)
                    kbDocsQuery = kbDocsQuery.Where(x => x.KnowledgeBaseCategoryId == filter.KnowledgeBaseCategoryId);

                if (filter.KnowledgeBaseDocumentId != null)
                    kbDocsQuery = kbDocsQuery.Where(x => x.Id == filter.KnowledgeBaseDocumentId);

                var matchingDocIds = await kbDocsQuery
                    .Select(x => x.DocId)
                    .Where(id => id != null)
                    .ToListAsync();

                if (!matchingDocIds.Any())
                {
                    return new ConversationStatisticsDetailsDto
                    {
                        Id = filter.ConversationId,
                        Title = "",
                        Turns = new List<ConversationStatisticsTurnDto>(),
                        TurnsCount = 0
                    };
                }

                var docToChunks = await _store.GetChunkIdsByDocIdsAsync(matchingDocIds, CancellationToken.None);

                matchingChunkIds = docToChunks
                    .Values
                    .SelectMany(v => v)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            var turnsQuery = _turnRepository
                .SelectAll(t => t.ConversationId == filter.ConversationId);

            if (searchText != null)
            {
                turnsQuery = turnsQuery.Where(t =>
                    (t.Question != null && t.Question.ToLower().Contains(searchText)) ||
                    (t.Answer != null && t.Answer.ToLower().Contains(searchText))
                );
            }

            if (matchingChunkIds != null)
            {
                turnsQuery = turnsQuery.Where(t =>
                    t.RetrievedChunkIds != null &&
                    t.RetrievedChunkIds.Any(cid => matchingChunkIds.Contains(cid))
                );
            }

            var totalCount = await turnsQuery.CountAsync();

            var pagedTurns = await turnsQuery
                .OrderBy(t => t.Index)
                .Skip(skip)
                .Take(filter.PageSize)
                .Select(t => new ConversationStatisticsTurnDto
                {
                    QuestionDate = t.QuestionDate,
                    Question = t.Question,
                    AnswerDate = t.AnswerDate,
                    Answer = t.Answer,
                    FeedbackValue = t.Feedback != null ? t.Feedback.Value : (int?)null,
                    ChunkIds = t.RetrievedChunkIds
                })
                .ToListAsync();

            var conversation = await _conversationRepository
                .SelectAll(c => c.Id == filter.ConversationId)
                .Select(c => new { c.Id, c.Title })
                .FirstOrDefaultAsync();

            if (conversation == null)
                return null;

            return new ConversationStatisticsDetailsDto
            {
                Id = conversation.Id,
                Title = conversation.Title,
                Turns = pagedTurns,
                TurnsCount = totalCount
            };
        }


        private HashSet<long> ExtractAllChunkIds<T>(IEnumerable<T> items, Func<T, IEnumerable<string>> getChunkIds)
        {
            return new HashSet<long>(
                items
                    .SelectMany(getChunkIds)
                    .Select(id => long.TryParse(id, out var parsed) ? parsed : (long?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
            );
        }

        private async Task<(Dictionary<long, string> KbNames, Dictionary<long, string> CategoryNames, Dictionary<long, string> DocumentNames)> LoadLookupDataAsync(List<KnowledgeBaseDocumentDto> kbDocs)
        {
            var kbIds = kbDocs.Select(x => x.KnowledgeBaseId).Distinct().ToList();
            var kbNames = await _knowledgeBaseRepository
                .SelectAll(x => kbIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var categoryIds = kbDocs.Select(x => x.KnowledgeBaseCategoryId).Distinct().ToList();
            var categoryNames = await _knowledgeBaseCategoryRepository
                .SelectAll(x => categoryIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            var fileIds = kbDocs.Select(x => x.FileItemId).Distinct().ToList();
            var documentNames = await _fileItemRepository
                .SelectAll(x => fileIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            return (kbNames, categoryNames, documentNames);
        }

        private IEnumerable<ConversationStatisticsDetailsGridItem> BuildTurnItems(
            string conversationTitle,
            ConversationStatisticsTurnDto turn,
            Dictionary<long, string> chunkToDoc,
            List<KnowledgeBaseDocumentDto> kbDocs,
            (Dictionary<long, string> KbNames, Dictionary<long, string> CategoryNames, Dictionary<long, string> DocumentNames) lookupData)
        {
            var turnChunkIds = turn.ChunkIds
                .Select(id => long.TryParse(id, out var parsed) ? parsed : (long?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var turnDocIds = turnChunkIds
                .Where(chunkToDoc.ContainsKey)
                .Select(id => chunkToDoc[id])
                .Distinct()
                .ToList();

            var turnKbDocs = turnDocIds
                .Where(docId => kbDocs.Any(k => k.DocId == docId))
                .SelectMany(docId => kbDocs.Where(k => k.DocId == docId))
                .ToList();

            var kbIds = turnKbDocs.Select(x => x.KnowledgeBaseId).Distinct().ToList();
            var categoryIds = turnKbDocs.Select(x => x.KnowledgeBaseCategoryId).Distinct().ToList();
            var fileIds = turnKbDocs.Select(x => x.FileItemId).Distinct().ToList();

            var items = new List<ConversationStatisticsDetailsGridItem>();

            // User Turn
            items.Add(new ConversationStatisticsDetailsGridItem
            {
                Title = conversationTitle,
                MessageDate = turn.QuestionDate,
                interactionsRoleModel = new InteractionsRoleModel() { Id = (int)InteractionsRoleEnum.User, Name = InteractionsRoleEnum.User.ToString() },
                Message = turn.Question,
                FeedbackValue = null
            });

            // AI Turn
            items.Add(new ConversationStatisticsDetailsGridItem
            {
                Title = conversationTitle,
                MessageDate = turn.AnswerDate ?? DateTime.MinValue,
                interactionsRoleModel = new InteractionsRoleModel() { Id = (int)InteractionsRoleEnum.AI, Name = InteractionsRoleEnum.AI.ToString() },
                Message = turn.Answer,
                FeedbackValue = turn.FeedbackValue,
                ChunksRetrievedCount = turnChunkIds.Count,
                KBUsed = GetDisplayValue(kbIds, lookupData.KbNames),
                KBCategoriesUsed = GetDisplayValue(categoryIds, lookupData.CategoryNames),
                DocumentsUsed = GetDisplayValue(fileIds, lookupData.DocumentNames),
                KnowledgeBaseIds = kbIds,
                KnowledgeBaseCategoryIds = categoryIds,
                DocumentIds = fileIds,
                ChunkIds = turnChunkIds
            });

            return items;
        }

        private string GetDisplayValue(List<long> ids, Dictionary<long, string> names)
        {
            if (!ids.Any()) return "0";
            if (ids.Count == 1 && names.TryGetValue(ids.First(), out var name))
                return name ?? "0";
            return ids.Count.ToString();
        }
        #endregion
    }
}
