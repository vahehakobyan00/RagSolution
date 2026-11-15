using Microsoft.OpenApi.Models;
using Rag.Service.DI;
using RagLib.DI;
using RagLib.Embeddings;
using RagLib.Models;
using RagLib.Storage;
using RagWebAPI.Context;
using RagWebAPI.Middleware;
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
builder.Services.AddRagLibDI();
builder.Services.AddServices(builder.Configuration);
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
{
    builder.WithOrigins("http://localhost:4000")
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials();
}));

var pg = Environment.GetEnvironmentVariable("POSTGRES_CS") ?? "Host=192.168.10.100;Port=5432;Database=VBTDatabase;Username=postgres;Password=123456";
var ollamaBase = Environment.GetEnvironmentVariable("OLLAMA_BASE") ?? "http://192.168.10.100:11434";
var model = Environment.GetEnvironmentVariable("EMBED_MODEL") ?? "bge-m3";
var dim = int.TryParse(Environment.GetEnvironmentVariable("EMBED_DIM"), out var d) ? d : 1024;
var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "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";
var openAiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
var modelMaxTokens = int.TryParse(Environment.GetEnvironmentVariable("MODEL_MAX_TOKENS"), out var _mx) ? _mx : 128_000;
var responseReserve = int.TryParse(Environment.GetEnvironmentVariable("RESPONSE_RESERVE"), out var _rr) ? _rr : 1024;

builder.Services.AddSingleton(new AppConfig(pg, ollamaBase, model, dim, OpenAIKey: openAiKey, OpenAIModel: openAiModel));
builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(ollamaBase) });
builder.Services.AddSingleton<IEmbeddingClient, OllamaEmbeddingClient>(sp => new OllamaEmbeddingClient(sp.GetRequiredService<HttpClient>(), model));
builder.Services.AddSingleton(sp => new VectorStore(pg, dim));
builder.Services.AddSingleton(new ConcurrentDictionary<string, ContextWindow>());
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
