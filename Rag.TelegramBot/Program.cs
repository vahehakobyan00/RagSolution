using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rag.Service.DI;
using Rag.TelegramBot.HostService;
using Rag.TelegramBot.Wrapper;
using Rag.TelegramBot.Wrapper.Interface;
using Rag.TelegramBot.Wrapper.Models.Models;
using Serilog;
using Telegram.Bot;

Console.WriteLine("Hello, World!");
var configuration = new ConfigurationBuilder()
             .AddJsonFile("appsettings.json")
             .AddEnvironmentVariables()
             .Build();
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();
try
{
    IHost host = Host.CreateDefaultBuilder(args)
        .ConfigureServices(services =>
        {
            services.Configure<RagWebApiSettings>(
                configuration.GetSection("RagWebApiSettings"));
            services.AddScoped<IRagWebApiWrapper, RagWebApiWrapper>();
            services.AddServices(configuration);
            services.AddSingleton<ITelegramBotClient>(provider =>
            {
                var botSettings = configuration.GetSection("TelegramBotSettings").Get<TelegramBotSettings>();
                return new TelegramBotClient(botSettings.BotToken);
            });
            services.AddHostedService<TelegramBotHostedService>();
        })
        .UseSerilog()
        .Build();
    await host.RunAsync();
}
catch (Exception ex)
{

    Log.Fatal(ex, "There was a problem starting the telegram service");
    return;
}
finally
{
    Log.CloseAndFlush();
}
