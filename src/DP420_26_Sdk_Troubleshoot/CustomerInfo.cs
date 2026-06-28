namespace DP420_26_Sdk_Troubleshoot;

using Newtonsoft.Json;

public class CustomerInfo
{
    [JsonProperty("id")] public string Id { get; set; } = "";

    [JsonProperty("title")] public string Title { get; set; } = "";

    [JsonProperty("firstName")] public string FirstName { get; set; } = "";

    [JsonProperty("lastName")] public string LastName { get; set; } = "";

    [JsonProperty("emailAddress")] public string EmailAddress { get; set; } = "";

    [JsonProperty("phoneNumber")] public string PhoneNumber { get; set; } = "";

    [JsonProperty("creationDate")] public string CreationDate { get; set; } = "";
}
