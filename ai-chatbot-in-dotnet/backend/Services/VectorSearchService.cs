using ChatBot.Models;

namespace ChatBot.Services;

public class VectorSearchService(StringEmbeddingGenerator embeddingGenerator,
                           IVectorIndex vectorIndex,
                           DocumentChunkStore contentStore)
{
    public async Task<List<DocumentChunk>> FindTopKChunks(string query, int k)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var embeddings = await embeddingGenerator.GenerateAsync([query],
                new Microsoft.Extensions.AI.EmbeddingGenerationOptions
                {
                    Dimensions = 512
                });

        var vector = embeddings[0].Vector;

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

    public Task<List<DocumentChunk>> FindInDatabase(string query) => FindTopKChunks(query, 5);
}