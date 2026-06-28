using Newtonsoft.Json;

namespace DP420_13_ChangeFeed_AzureFunction.Models;

public record Product
{
    [JsonProperty("id")] public required string Id { get; init; }

    [JsonProperty("name")] public required string Name { get; init; }

    [JsonProperty("category")] public required Category Category { get; init; }

    [JsonProperty("categoryId")] public required string CategoryId { get; init; }
}
