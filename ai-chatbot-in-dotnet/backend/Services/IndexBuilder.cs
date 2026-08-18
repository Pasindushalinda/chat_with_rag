using Microsoft.Extensions.AI;
using System.Collections.Immutable;

namespace ChatBot.Services;

public class IndexBuilder(
    StringEmbeddingGenerator embeddingGenerator,
    IVectorIndex vectorIndex,
    WikipediaClient wikipediaClient,
    DocumentChunkStore chunkStore,
    DocumentStore documentStore,
    ArticleSplitter splitter)
{
    public async Task BuildIndex(string[] pageTitles)
    {
        foreach (var title in pageTitles)
        {
            // Swap out the wikipediaClient here to connect to your own data source!
            var page = await wikipediaClient.GetWikipediaPageForTitle(title, full: true);
            var sections = wikipediaClient.SplitIntoSections(page.Content);

            var chunks = sections.SelectMany(section =>
                        splitter.Chunk(page.Title, section.Content, page.PageUrl, section.Title))
                        .Take(25)
                        .ToImmutableList();

            var stringsToEmbed = chunks.Select(c => $"{c.Title} > {c.Section}\n\n{c.Content}");

            // Makes a call to OpenAI to create an embedding from these strings
            var embeddings = await embeddingGenerator.GenerateAsync(
                stringsToEmbed,
                new EmbeddingGenerationOptions { Dimensions = 512 }
            );

            var records = chunks.Select((chunk, index) => new VectorRecord(
                chunk.Id,
                embeddings[index].Vector,
                chunk.Title,
                chunk.Section,
                chunk.ChunkIndex));

            await vectorIndex.UpsertAsync(records);

            foreach (var chunk in chunks)
            {
                chunkStore.SaveDocumentChunk(chunk);
            }

            // If you have rate limit issues with Pinecone (may happen based on your plan) then uncomment this Task.Delay()
            // see https://docs.pinecone.io/reference/api/database-limits#rate-limits
            // await Task.Delay(500);
        }
    }
}
