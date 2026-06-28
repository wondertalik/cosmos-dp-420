using Newtonsoft.Json;

namespace DP420_13_ChangeFeed_Processor.Models;

public record Category
{
    [JsonProperty("name")] public required string Name { get; init; }
}
