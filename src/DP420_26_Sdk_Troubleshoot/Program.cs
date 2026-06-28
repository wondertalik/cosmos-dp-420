using DP420_26_Sdk_Troubleshoot;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

IConfigurationRoot configurationRoot = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>()
    .Build();

string key = configurationRoot.GetValue<string>("Key") ?? throw new ArgumentException("Key not found");
string endpoint = configurationRoot.GetValue<string>("Endpoint") ?? throw new ArgumentException("Endpoint not found");

string connectionString = "AccountEndpoint=" + endpoint + ";AccountKey=" + key;

CosmosClient client = new(connectionString,
    new CosmosClientOptions
    {
        AllowBulkExecution = true,
        MaxRetryAttemptsOnRateLimitedRequests = 50,
        MaxRetryWaitTimeOnRateLimitedRequests = new TimeSpan(0, 1, 30)
    });

Console.WriteLine("Creating Azure Cosmos DB Databases and containers");

Database customersDb = await client.CreateDatabaseIfNotExistsAsync("CustomersDB");
Container customersDbCustomerContainer =
    await customersDb.CreateContainerIfNotExistsAsync(id: "container", partitionKeyPath: "/id", throughput: 400);

Console.Clear();
Console.WriteLine("1) Add Document 1 with id = '0C297972-BE1B-4A34-8AE1-F39E6AA3D828'");
Console.WriteLine("2) Add Document 2 with id = 'AAFF2225-A5DD-4318-A6EC-B056F96B94B7'");
Console.WriteLine("3) Delete Document 1 with id = '0C297972-BE1B-4A34-8AE1-F39E6AA3D828'");
Console.WriteLine("4) Delete Document 2 with id = 'AAFF2225-A5DD-4318-A6EC-B056F96B94B7'");
Console.WriteLine("5) Exit");
Console.Write("\r\nSelect an option: ");

string? consoleinputcharacter;

while ((consoleinputcharacter = Console.ReadLine()) != "5")
{
    try
    {
        await CompleteTaskOnCosmosDb(consoleinputcharacter, customersDbCustomerContainer);
    }
    catch (CosmosException ex)
    {
        switch (ex.StatusCode.ToString())
        {
            case "Conflict":
                Console.WriteLine("Insert Failed. Response Code (409).");
                Console.WriteLine(
                    "Can not insert a duplicate partition key, customer with the same ID already exists.");
                break;
            case "Forbidden":
                Console.WriteLine("Response Code (403).");
                Console.WriteLine(
                    "The request was forbidden to complete. Some possible reasons for this exception are:");
                Console.WriteLine("Firewall blocking requests.");
                Console.WriteLine("Partition key exceeding storage.");
                Console.WriteLine("Non-data operations are not allowed.");
                break;
            case "TooManyRequests":
            case "ServiceUnavailable":
            case "RequestTimeout":
                // Check if the issues are related to connectivity and if so, wait 10 seconds to retry.
                await Task.Delay(10000); // Wait 10 seconds
                try
                {
                    Console.WriteLine("Try one more time...");
                    await CompleteTaskOnCosmosDb(consoleinputcharacter, customersDbCustomerContainer);
                }
                catch (CosmosException e2)
                {
                    Console.WriteLine("Insert Failed. " + e2.Message);
                    Console.WriteLine("Can not insert a duplicate partition key, Connectivity issues encountered.");
                }

                break;
            case "NotFound":
                Console.WriteLine("Delete Failed. Response Code (404).");
                Console.WriteLine("Can not delete customer, customer not found.");
                break;
            default:
                Console.WriteLine(ex.Message);
                break;
        }
    }

    Console.WriteLine("Choose an action:");
    Console.WriteLine("1) Add Document 1 with id = '0C297972-BE1B-4A34-8AE1-F39E6AA3D828'");
    Console.WriteLine("2) Add Document 2 with id = 'AAFF2225-A5DD-4318-A6EC-B056F96B94B7'");
    Console.WriteLine("3) Delete Document 1 with id = '0C297972-BE1B-4A34-8AE1-F39E6AA3D828'");
    Console.WriteLine("4) Delete Document 2 with id = 'AAFF2225-A5DD-4318-A6EC-B056F96B94B7'");
    Console.WriteLine("5) Exit");
    Console.Write("\r\nSelect an option: ");
}

return;

async Task CompleteTaskOnCosmosDb(string? input, Container container)
{
    switch (input)
    {
        case "1":
            await CreateDocument1(container);
            break;
        case "2":
            await CreateDocument2(container);
            break;
        case "3":
            await DeleteDocument1(container);
            break;
        case "4":
            await DeleteDocument2(container);
            break;
        case "5":
            break;
        default:
            Console.WriteLine("Default");
            break;
    }

    Console.Clear();
}

async Task CreateDocument1(Container container)
{
    string customerId = "0C297972-BE1B-4A34-8AE1-F39E6AA3D828";
    var partitionKey = new PartitionKey(customerId);

    var customer = new CustomerInfo
    {
        Id = customerId,
        Title = "",
        FirstName = "Franklin",
        LastName = "Ye",
        EmailAddress = "franklin9@adventure-works.com",
        PhoneNumber = "1 (11) 500 555-0139",
        CreationDate = "2014-02-05T00:00:00"
    };

    Console.Clear();

    ItemResponse<CustomerInfo> response = await container.CreateItemAsync(customer, partitionKey);
    Console.WriteLine("Insert Successful.");
    Console.WriteLine("Document for customer with id = '" + customerId + "' Inserted.");

    Console.WriteLine("Press [ENTER] to continue");
    Console.ReadLine();
}

async Task CreateDocument2(Container container)
{
    string customerId = "AAFF2225-A5DD-4318-A6EC-B056F96B94B7";

    var customer = new CustomerInfo
    {
        Id = customerId,
        Title = "",
        FirstName = "Michael",
        LastName = "Gonzalez",
        EmailAddress = "mgonz01@adventure-works.com",
        PhoneNumber = "1 (44) 500 555-6612",
        CreationDate = "2016-08-27T00:00:00"
    };

    Console.Clear();

    ItemResponse<CustomerInfo> response = await container.CreateItemAsync(customer, new PartitionKey(customerId));
    Console.WriteLine("Insert Successful.");
    Console.WriteLine("Document for customer with id = '" + customerId + "' Inserted.");

    Console.WriteLine("Press [ENTER] to continue");
    Console.ReadLine();
}

async Task DeleteDocument1(Container container)
{
    string customerId = "0C297972-BE1B-4A34-8AE1-F39E6AA3D828";

    Console.Clear();

    ItemResponse<CustomerInfo> response =
        await container.DeleteItemAsync<CustomerInfo>(partitionKey: new PartitionKey(customerId), id: customerId);
    Console.WriteLine("Delete Successful.");
    Console.WriteLine("Document for customer with id = '" + customerId + "' Deleted.");


    Console.WriteLine("Press [ENTER] to continue");
    Console.ReadLine();
}

static async Task DeleteDocument2(Container container)
{
    string customerId = "AAFF2225-A5DD-4318-A6EC-B056F96B94B7";

    Console.Clear();

    ItemResponse<CustomerInfo> response =
        await container.DeleteItemAsync<CustomerInfo>(partitionKey: new PartitionKey(customerId), id: customerId);
    Console.WriteLine("Delete Successful.");
    Console.WriteLine("Document for customer with id = '" + customerId + "' Deleted.");

    Console.WriteLine("Press [ENTER] to continue");
    Console.ReadLine();
}
