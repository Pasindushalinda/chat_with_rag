using System.Globalization;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.AI;
using OpenAI;

class Program
{
    static async Task Main(string[] args)
    {
        var azureEndpoint = RequireEnv("AZURE_OPENAI_ENDPOINT");
        var azureApiKey = RequireEnv("AZURE_OPENAI_API_KEY");
        var embeddingDeployment = RequireEnv("AZURE_OPENAI_EMBEDDING_DEPLOYMENT");

        var vectors = new List<(string Word, float[] Vector)>();

        // Use Azure OpenAI client
        var azureClient = new Azure.AI.OpenAI.AzureOpenAIClient(
            new Uri(azureEndpoint),
            new Azure.AzureKeyCredential(azureApiKey)
        );

        var embeddingClient = azureClient.GetEmbeddingClient(embeddingDeployment);

        // Generate embeddings using Azure OpenAI directly (without Microsoft.Extensions.AI wrapper)

        var words = new[] { "cat", "mouse", "lion", "tiger", "helicopter", "train", "blue", "carrot", "space" };

        foreach (var word in words)
        {
            var embeddingOptions = new OpenAI.Embeddings.EmbeddingGenerationOptions
            {
                Dimensions = 512
            };

            var embedding = await embeddingClient.GenerateEmbeddingAsync(word, embeddingOptions);

            vectors.Add((word, embedding.Value.ToFloats().ToArray()));
        }

        SaveCsv(vectors, "embeddings.csv");
        Console.WriteLine("Saved embeddings.csv");
    }

    #region Helpers

    static string RequireEnv(string key)
    {
        var v = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(v))
            throw new Exception($"Missing env var: {key}");
        return v!;
    }

    static void SaveCsv(
        List<(string Word, float[] Vec)> data,
        string path)
    {
        if (data.Count == 0)
        {
            Console.WriteLine("No vectors to project.");
            return;
        }

        // Build an n x d matrix (double) and mean-center
        int n = data.Count;
        int d = data[0].Vec.Length;

        var X = Matrix<double>.Build.Dense(n, d, (i, j) => data[i].Vec[j]);

        // Mean-center columns
        var means = Vector<double>.Build.Dense(d);
        for (int j = 0; j < d; j++)
        {
            means[j] = X.Column(j).Average();
            for (int i = 0; i < n; i++)
                X[i, j] -= means[j];
        }

        // PCA via SVD of mean-centered X
        // X = U * S * V^T, principal directions = V columns
        var svd = X.Svd(computeVectors: true);
        var V = svd.VT.Transpose(); // d x d

        // Take first two principal components
        var V2 = V.SubMatrix(0, d, 0, 2); // d x 2
        var Y = X * V2; // n x 2

        // Write CSV: title,x,y (culture-invariant)
        using var sw = new StreamWriter(path, false, Encoding.UTF8);
        sw.WriteLine("title,x,y");

        for (int i = 0; i < n; i++)
        {
            var x = Y[i, 0];
            var y = Y[i, 1];
            sw.WriteLine($"{CsvEscape(data[i].Word)},{x.ToString(CultureInfo.InvariantCulture)},{y.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    static string CsvEscape(string s)
    {
        if (s == null) return "";
        var needsQuotes = s.Contains(',') || s.Contains('"') || s.Contains('\n');
        if (needsQuotes)
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    #endregion
}