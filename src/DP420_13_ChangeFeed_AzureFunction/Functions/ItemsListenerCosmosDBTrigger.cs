using DP420_13_ChangeFeed_AzureFunction.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DP420_13_ChangeFeed_AzureFunction.Functions;

public class ItemsListenerCosmosDbTrigger(ILogger<ItemsListenerCosmosDbTrigger> logger)
{
    [Function("ItemsListenerCosmosDBTrigger")]
    public void Run([CosmosDBTrigger(
            databaseName: "cosmicworks",
            containerName: "products",
            Connection = "CosmosDBConnection",
            LeaseContainerName = "lease",
            CreateLeaseContainerIfNotExists = false)]
        IReadOnlyList<Product>? input,
        FunctionContext context)
    {
        int changesCount = input?.Count ?? 0;
        logger.LogInformation(
            "ItemsListenerCosmosDBTrigger invoked. InvocationId: {InvocationId}. Changes received: {ChangesCount}",
            context.InvocationId,
            changesCount);

        if (changesCount == 0)
        {
            logger.LogInformation("No change feed items found in this invocation");
            return;
        }

        foreach (Product product in input ?? Enumerable.Empty<Product>())
        {
            logger.LogInformation(
                "Detected Operation:\nid: [{ProductId}]\nname: {ProductName}\ncategory name: {CategoryName}\ncategoryId: {CategoryId}",
                product.Id, product.Name, product.Category.Name, product.CategoryId);
        }
    }
}
