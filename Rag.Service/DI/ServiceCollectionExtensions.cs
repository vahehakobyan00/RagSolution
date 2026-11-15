using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rag.Data.DI;
using Rag.Service.Agent;
using Rag.Service.Factory;
using Rag.Service.Factory.Interfaces;
using Rag.Service.FileServices.Interfaces;
using Rag.Service.FileServices.Services;
using Rag.Service.Mapping;
using Rag.Service.Services;
using Rag.Service.Services.Interfaces;
using Rag.Service.Wrapper;
using Rag.Service.Wrapper.Interfaces;
using RagLib.DI;

namespace Rag.Service.DI
{
    public static class ServiceCollectionExtensions
    {
        public static void AddServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddAutoMapper(typeof(ServiceToDataMappingProfile).Assembly);
            services.AddDataRepositoryServices(config);
            services.AddRagLibDI(config);
            services.AddScoped<IEnumService, EnumService>();
            services.AddScoped<IServiceAgentService, ServiceAgentService>();
            services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
            services.AddScoped<IKnowledgeBaseCategoryService, KnowledgeBaseCategoryService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IUploaderService, UploaderService>();
            services.AddScoped<IKnowledgeBaseDocumentService, KnowledgeBaseDocumentService>();
            services.AddScoped<IIngestService, IngestService>();
            services.AddScoped<IHistoryService, HistoryService>();
            services.AddScoped<IConversationStatisticsService, ConversationStatisticsService>();
            services.AddScoped<IIdentityWrapper, IdentityWrapper>();
            services.AddScoped<IChunkService, ChunkService>();
            services.AddScoped<ISyncVectoreService, SyncVectoreService>();          
            services.AddScoped<ILlmProviderFactory, LlmProviderFactory>();
            services.AddScoped<ITelegramConversationService, TelegramConversationService>();
            services.AddScoped<IDeleteValidationService, DeleteValidationService>();
            services.AddScoped<ITokenValidationService, TokenValidationService>();
            services.AddScoped<IRestaurantService, RestaurantService>();
            services.AddScoped<RestaurantToolbox>();
        }
    }
}
