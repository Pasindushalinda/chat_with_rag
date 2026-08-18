using Pinecone;

namespace ChatBot.Services;

public class PineconeVectorIndex(IndexClient pineconeIndex) : IVectorIndex
{
    public async Task UpsertAsync(IEnumerable<VectorRecord> records)
    {
        var vectors = records.Select(r => new Vector
        {
            Id = r.Id,
            Values = r.Values.ToArray(),
            Metadata = new Metadata
            {
                { "title", r.Title },
                { "section", r.Section },
                { "chunk_index", r.ChunkIndex }
            }
        });

        await pineconeIndex.UpsertAsync(new UpsertRequest { Vectors = vectors });
    }

    public async Task<List<VectorMatch>> QueryAsync(ReadOnlyMemory<float> vector, int topK)
    {
        var response = await pineconeIndex.QueryAsync(new QueryRequest
        {
            Vector = vector.ToArray(),
            TopK = (uint)topK,
            IncludeMetadata = true
        });

        return (response.Matches ?? [])
            .Where(m => m.Id is not null)
            .Select(m => new VectorMatch(m.Id!, m.Score ?? 0))
            .ToList();
    }
}