using ChatBot.Services;
using Microsoft.Extensions.AI;

public static class FunctionRegistry
{
    public static IEnumerable<AITool> GetTools(this IServiceProvider sp)
    {
        // This code happens in the composition root, so pull the service from the IServiceProvider
        var vectorService = sp.GetRequiredService<VectorSearchService>();

        yield return AIFunctionFactory.Create(
            typeof(VectorSearchService).GetMethod(nameof(VectorSearchService.FindInDatabase),
            [typeof(string)])!,
            vectorService,
            new AIFunctionFactoryOptions
            {
                Name = "database_search_service",
                Description = "Searches for information about landmarks in the database based on a semantic search query.",
            });

    }
}

