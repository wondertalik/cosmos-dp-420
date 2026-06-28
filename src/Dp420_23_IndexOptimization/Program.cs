using Newtonsoft.Json;
using Dp420_23_IndexOptimization;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

IConfigurationRoot configurationRoot = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>()
    .Build();

string key = configurationRoot.GetValue<string>("Key") ?? throw new ArgumentException("Key not found");
string endpoint = configurationRoot.GetValue<string>("Endpoint") ?? throw new ArgumentException("Endpoint not found");

CosmosClient client = new CosmosClient(endpoint, key, new CosmosClientOptions
{
    ConnectionMode = ConnectionMode.Gateway
});

Container container = client.GetContainer("cosmicworks", "products");

string json = await File.ReadAllTextAsync("sample.json");

json = json.Replace(
    "<unique-identifier>",
    $"{Guid.NewGuid()}"
);

var item = JsonConvert.DeserializeObject<Product>(json);

if (item is not null)
{
    var response = await container.UpsertItemAsync(item);

    Console.WriteLine($"Item Created:\t{response.Resource.id}");
    Console.WriteLine($"RU Charge:\t{response.RequestCharge:0.00}");
}
