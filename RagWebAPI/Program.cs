using Microsoft.OpenApi.Models;
using Rag.Service.DI;
using RagLib.DI;
using RagLib.Embeddings;
using RagLib.Models;
using RagLib.Storage;
using RagWebAPI.Context;
using RagWebAPI.Middleware;
using RagWebAPI.Agent;
using RagWebAPI.Voice;
using Serilog;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

#region serilog

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

#endregion
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddServices(builder.Configuration); // also calls AddRagLibDI(configuration)
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
{
    builder.WithOrigins("http://localhost:4000")
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials();
}));

// Connection settings live in appsettings.json ("VectorStore", "Ollama"); env vars still override.
var pg = Environment.GetEnvironmentVariable("POSTGRES_CS") ?? builder.Configuration["VectorStore:ConnectionString"]
    ?? throw new InvalidOperationException("Missing VectorStore:ConnectionString in appsettings.json");
var ollamaBase = Environment.GetEnvironmentVariable("OLLAMA_BASE") ?? builder.Configuration["Ollama:BaseUrl"]
    ?? throw new InvalidOperationException("Missing Ollama:BaseUrl in appsettings.json");
var model = Environment.GetEnvironmentVariable("EMBED_MODEL") ?? builder.Configuration["Ollama:EmbedModel"] ?? "bge-m3";
var dim = int.TryParse(Environment.GetEnvironmentVariable("EMBED_DIM") ?? builder.Configuration["Ollama:EmbedDim"], out var d) ? d : 1024;
var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "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";
var openAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
var modelMaxTokens = int.TryParse(Environment.GetEnvironmentVariable("MODEL_MAX_TOKENS"), out var _mx) ? _mx : 128_000;
var responseReserve = int.TryParse(Environment.GetEnvironmentVariable("RESPONSE_RESERVE"), out var _rr) ? _rr : 1024;

builder.Services.AddSingleton(new AppConfig(pg, ollamaBase, model, dim, OpenAIKey: openAiKey, OpenAIModel: openAiModel));
builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(ollamaBase) });
builder.Services.AddSingleton<IEmbeddingClient, OllamaEmbeddingClient>(sp => new OllamaEmbeddingClient(sp.GetRequiredService<HttpClient>(), model));
builder.Services.AddSingleton(sp => new VectorStore(pg, dim));
builder.Services.AddSingleton(new ConcurrentDictionary<string, ContextWindow>());

// Restaurant voice agent
builder.Services.Configure<VoiceOptions>(builder.Configuration.GetSection("Voice"));
builder.Services.AddScoped<IVoiceService, OpenAiVoiceService>();
builder.Services.AddScoped<RestaurantAgent>();
builder.Services.AddHealthChecks();

builder.Services.AddSwaggerGen(c =>
{
    // Add an API Key security scheme that lives in the header
    c.AddSecurityDefinition("UserIdHeader", new OpenApiSecurityScheme
    {
        Description = "Put your user id here (e.g., a GUID).",
        Type = SecuritySchemeType.ApiKey,
        Name = "X-User-Id",             // <-- your header name
        In = ParameterLocation.Header
    });

    // Require it for all operations (optional; you can also do this per-controller)
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "UserIdHeader"
            }
        }
        ] = Array.Empty<string>()
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.UseStaticFiles(); // wwwroot/voice.html — before UseUserIdHeader so static files need no X-User-Id
app.UseAuthorization();

app.UseUserIdHeader(opts =>
{
    opts.HeaderName = "X-User-Id";   // change if you like
    opts.Required = true;             // 400 when missing/invalid
    opts.AcceptGuidOnly = true;       // enforce GUID format
    opts.StampClaimsPrincipal = true; // add NameIdentifier claim
});

app.MapControllers();
app.MapHealthChecks("/healthz");
app.Run();
