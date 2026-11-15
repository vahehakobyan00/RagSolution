using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rag.Data.DBProviderConfiguration;
using Rag.Data.Repositories;
using Rag.Data.Repositories.Interfaces;

namespace Rag.Data.DI
{
    public static class IServiceCollectionExtension
    {
        public static void AddDataRepositoryServices(this IServiceCollection services, IConfiguration config)
        {
            services.ConfigureProvider(config);
            services.AddScoped<IServiceAgentRepository, ServiceAgentRepository>();
            services.AddScoped<IKnowledgeBaseRepository, KnowledgeBaseRepository>();
            services.AddScoped<IKnowledgeBaseCategoryRepository, KnowledgeBaseCategoryRepository>();
            services.AddScoped<IFileItemRepository, FileItemRepository>();
            services.AddScoped<IKnowledgeBaseDocumentRepository, KnowledgeBaseDocumentRepository>();
            services.AddScoped<IConversationRepository, ConversationRepository>();
            services.AddScoped<ITurnRepository, TurnRepository>();
            services.AddScoped<ITurnFeedbackRepository, TurnFeedbackRepository>();
            services.AddScoped<ITelegramConversationRepository, TelegramConversationRepository>();
            services.AddScoped<IMenuItemRepository, MenuItemRepository>();
            services.AddScoped<IRestaurantTableRepository, RestaurantTableRepository>();
            services.AddScoped<IBookingRepository, BookingRepository>();
        }
    }
}
