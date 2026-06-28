using DP420_13_ChangeFeed;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

// Process change feed events using the Azure Cosmos DB for NoSQL SDK
// @link https://microsoftlearning.github.io/dp-420-cosmos-db-dev/instructions/13-change-feed.html

IConfigurationRoot configurationRoot = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>()
    .Build();

string key = configurationRoot.GetValue<string>("Key") ?? throw new ArgumentException("Key not found");
string endpoint = configurationRoot.GetValue<string>("Endpoint") ?? throw new ArgumentException("Endpoint not found");

CosmosClient client = new CosmosClient(endpoint, key);

Container sourceContainer = client.GetContainer("cosmicworks", "products");
Container leaseContainer = client.GetContainer("cosmicworks", "lease");

Container.ChangesHandler<Product> handleChanges =
    async (IReadOnlyCollection<Product> changes, CancellationToken cancellationToken) =>
    {
        Console.WriteLine($"START\tHandling batch of changes...");
        foreach (var product in changes)
        {
            await Console.Out.WriteLineAsync(
                $"Detected Operation:\nid: [{product.Id}]\nname: {product.Name}\ncategory name: {product.Category.Name}\ncategoryId: {product.CategoryId}");
        }
    };

ChangeFeedProcessor processor = sourceContainer
    .GetChangeFeedProcessorBuilder(
        processorName: "productsProcessor",
        onChangesDelegate: handleChanges
    )
    .WithInstanceName("consoleApp")
    .WithLeaseContainer(leaseContainer).Build();


await processor.StartAsync();

Console.WriteLine("RUN\tListening for changes...");
Console.WriteLine("Press any key to stop");
Console.ReadKey();

await processor.StopAsync();
