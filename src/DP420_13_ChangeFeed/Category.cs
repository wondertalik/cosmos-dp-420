using Newtonsoft.Json;

namespace DP420_13_ChangeFeed;

public record Category
{
    [JsonProperty("name")] public required string Name { get; init; }
}
