using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Rag.Data.Data;

namespace Rag.Data.DBProviderConfiguration
{
    public static class ProviderConfiguration
    {
        public static void ConfigureProvider(this IServiceCollection services, IConfiguration config)
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(
                config["Rag:ConnectionString"]);

            dataSourceBuilder.UseJsonNet();

            var dataSource = dataSourceBuilder.Build();

            services.AddDbContext<ApplicationDbContext>(
            (sp, options) => options.UseNpgsql(dataSource),
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Scoped);
        }
    }
}
