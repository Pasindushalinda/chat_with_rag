using Azure.Search.Documents.Indexes;

namespace ChatBot.Models;

/// <summary>
/// Azure AI Search document schema for chunk vectors (parity with the fields Pinecone stores in Metadata).
/// </summary>
public class ChunkSearchDocument
{
    [SimpleField(IsKey = true, IsFilterable = true)]
    public string Id { get; set; } = default!;

    [SearchableField(IsFilterable = true, IsSortable = true)]
    public string Title { get; set; } = default!;

    [SearchableField(IsFilterable = true)]
    public string Section { get; set; } = default!;

    [SimpleField(IsFilterable = true, IsSortable = true)]
    public int ChunkIndex { get; set; }

    [VectorSearchField(VectorSearchDimensions = 512, VectorSearchProfileName = "chunk-vector-profile")]
    public ReadOnlyMemory<float> Values { get; set; }
}