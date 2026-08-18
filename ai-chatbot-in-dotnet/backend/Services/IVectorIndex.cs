namespace ChatBot.Services;

public record VectorRecord(string Id, ReadOnlyMemory<float> Values, string Title, string Section, int ChunkIndex);

public record VectorMatch(string Id, float Score);

/// <summary>
/// Abstraction over the vector database backend, so callers don't depend on Pinecone or
/// Azure AI Search directly. The active implementation is chosen in Startup.cs via the
/// "VectorDbProvider" config setting.
/// </summary>
public interface IVectorIndex
{
    Task UpsertAsync(IEnumerable<VectorRecord> records);

    Task<List<VectorMatch>> QueryAsync(ReadOnlyMemory<float> vector, int topK);
}
