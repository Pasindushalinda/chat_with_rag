using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using ChatBot.Models;

namespace ChatBot.Services;

public class AzureSearchVectorIndex(SearchClient searchClient, SearchIndexClient indexClient, string indexName) : IVectorIndex
{
    private bool _indexEnsured;

    private async Task EnsureIndexAsync()
    {
        if (_indexEnsured) return;

        var fields = new FieldBuilder().Build(typeof(ChunkSearchDocument));

        var definition = new SearchIndex(indexName, fields)
        {
            VectorSearch = new()
            {
                Profiles = { new VectorSearchProfile("chunk-vector-profile", "chunk-hnsw") },
                Algorithms = { new HnswAlgorithmConfiguration("chunk-hnsw") }
            }
        };

        await indexClient.CreateOrUpdateIndexAsync(definition);
        _indexEnsured = true;
    }

    public async Task UpsertAsync(IEnumerable<VectorRecord> records)
    {
        await EnsureIndexAsync();

        var documents = records.Select(r => new ChunkSearchDocument
        {
            Id = r.Id,
            Title = r.Title,
            Section = r.Section,
            ChunkIndex = r.ChunkIndex,
            Values = r.Values
        });

        await searchClient.MergeOrUploadDocumentsAsync(documents);
    }

    public async Task<List<VectorMatch>> QueryAsync(ReadOnlyMemory<float> vector, int topK)
    {
        var options = new SearchOptions
        {
            VectorSearch = new()
            {
                Queries = { new VectorizedQuery(vector) { KNearestNeighborsCount = topK, Fields = { "Values" } } }
            }
        };

        var response = await searchClient.SearchAsync<ChunkSearchDocument>("*", options);

        var matches = new List<VectorMatch>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            matches.Add(new VectorMatch(result.Document.Id, (float)(result.Score ?? 0)));
        }

        return matches;
    }
}