using System;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Pinecone;
using ChatBot.Services;

namespace ChatBot;

static class Startup
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("FrontendCors", policy =>
                policy
                    .WithOrigins("http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
            );
        });

        // Configure embedding generator (supports both OpenAI and Azure OpenAI)
        builder.Services.AddSingleton<StringEmbeddingGenerator>(s =>
        {
            var azureEndpoint = builder.RequireEnv("AZURE_OPENAI_ENDPOINT");
            var azureApiKey = builder.RequireEnv("AZURE_OPENAI_API_KEY");
            var embeddingDeployment = builder.RequireEnv("AZURE_OPENAI_EMBEDDING_DEPLOYMENT");

            var azureClient = new Azure.AI.OpenAI.AzureOpenAIClient(
                new Uri(azureEndpoint),
                new Azure.AzureKeyCredential(azureApiKey)
            );

            return azureClient.GetEmbeddingClient(embeddingDeployment).AsIEmbeddingGenerator();
        });

        // Configure vector index (supports both Pinecone and Azure AI Search)
        const string vectorIndexName = "landmark-chunks";
        var vectorDbProvider = builder.Configuration.GetValue<string>("VectorDbProvider") ?? "Pinecone";

        if (string.Equals(vectorDbProvider, "AzureSearch", StringComparison.OrdinalIgnoreCase))
        {
            var searchEndpoint = builder.RequireEnv("AZURE_SEARCH_ENDPOINT");
            var searchKey = builder.RequireEnv("AZURE_SEARCH_API_KEY");
            var credential = new Azure.AzureKeyCredential(searchKey);

            builder.Services.AddSingleton(s => new SearchIndexClient(new Uri(searchEndpoint), credential));
            builder.Services.AddSingleton(s => new SearchClient(new Uri(searchEndpoint), vectorIndexName, credential));
            builder.Services.AddSingleton<IVectorIndex>(s => new AzureSearchVectorIndex(
                s.GetRequiredService<SearchClient>(),
                s.GetRequiredService<SearchIndexClient>(),
                vectorIndexName));
        }
        else
        {
            var pineconeKey = builder.RequireEnv("PINECONE_API_KEY");
            builder.Services.AddSingleton<IndexClient>(s => new PineconeClient(pineconeKey).Index(
                vectorIndexName
                // "https://landmark-chunks-jbshle9.svc.aped-4627-b74a.pinecone.io"
            ));
            builder.Services.AddSingleton<IVectorIndex>(s => new PineconeVectorIndex(s.GetRequiredService<IndexClient>()));
        }

        builder.Services.AddSingleton<DocumentChunkStore>(s => new DocumentChunkStore());

        builder.Services.AddSingleton<VectorSearchService>();

        builder.Services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Information));

        builder.Services.AddSingleton<ILoggerFactory>(sp =>
            LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information)));

        builder.Services.AddSingleton<IChatClient>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            IChatClient client;

            var azureEndpoint = builder.RequireEnv("AZURE_OPENAI_ENDPOINT");
            var azureApiKey = builder.RequireEnv("AZURE_OPENAI_API_KEY");
            var chatDeployment = builder.RequireEnv("AZURE_OPENAI_CHAT_DEPLOYMENT");

            var azureClient = new Azure.AI.OpenAI.AzureOpenAIClient(
                new Uri(azureEndpoint),
                new Azure.AzureKeyCredential(azureApiKey)
            );

            client = azureClient.GetChatClient(chatDeployment).AsIChatClient();

            return new ChatClientBuilder(client)
                .UseLogging(loggerFactory)
                .UseFunctionInvocation(loggerFactory, c => { c.IncludeDetailedErrors = true; })
                .Build(sp);
        });

        builder.Services.AddTransient<ChatOptions>(sp => new ChatOptions
        {
            Tools = FunctionRegistry.GetTools(sp).ToList(),
        });

        builder.Services.AddSingleton<WikipediaClient>();
        builder.Services.AddSingleton<IndexBuilder>();
        builder.Services.AddSingleton<RagQuestionService>();
        builder.Services.AddSingleton<ArticleSplitter>();
        builder.Services.AddSingleton<PromptService>();
        builder.Services.AddSingleton<DocumentStore>();
    }
}