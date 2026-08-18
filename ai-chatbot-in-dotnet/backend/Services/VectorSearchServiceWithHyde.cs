using ChatBot.Models;
using Microsoft.Extensions.AI;

namespace ChatBot.Services;

/// <summary>
/// Drop-in replacement for VectorSearchService that adds a HYDE step for better retrieval
/// You can use the methods in <see cref="HydeFusion"/> to experiment with different fusion approaches
/// </summary>
public class VectorSearchServiceWithHyde(
    StringEmbeddingGenerator embeddingGenerator,
    IVectorIndex vectorIndex,
    DocumentChunkStore contentStore,
    IChatClient chatClient,
    ChatOptions chatOptions,
    PromptService promptService
)
{

    private async Task<string?> GenerateHypothesisAsync(string question)
    {
        var systemText = "You create concise, factual reference passages.";
        var userText = promptService.HydePrompt.Replace("{{question}}", question);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemText),
            new(ChatRole.User, userText)
        };

        var response = await chatClient.GetResponseAsync(messages, chatOptions);
        var text = response?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;
        return text.Length > 1500 ? text[..1500] : text;
    }

    public async Task<List<DocumentChunk>> FindTopKChunks(string query, int k)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        // 1) Generate a hypothetical passage for HYDE (assume success for simplicity)
        var hypothesis = await GenerateHypothesisAsync(query);

        // In this tutorial we always use the HYDE hypothesis for search.
        var textToEmbed = hypothesis ?? query;

        // 2) Embed the HYDE hypothesis text
        var embs = await embeddingGenerator.GenerateAsync(
            new[] { textToEmbed },
            new EmbeddingGenerationOptions { Dimensions = 512 }
        );

        // 3) Use the single embedding as the search vector
        var vector = embs[0].Vector;

        var matches = await vectorIndex.QueryAsync(vector, k);
        if (matches.Count == 0)
            return [];

        var ids = matches.Select(m => m.Id);
        var articles = contentStore.GetDocumentChunks(ids);

        var scoreById = matches.ToDictionary(m => m.Id, m => m.Score);

        var ordered = articles.OrderByDescending(a => scoreById.GetValueOrDefault(a.Id, 0f))
                              .Take(k)
                              .ToList();

        return ordered;
    }
}