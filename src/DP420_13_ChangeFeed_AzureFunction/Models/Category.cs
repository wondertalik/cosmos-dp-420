using Newtonsoft.Json;

namespace DP420_13_ChangeFeed_AzureFunction.Models;

public record Category
{
    [JsonProperty("name")] public required string Name { get; init; }
}
