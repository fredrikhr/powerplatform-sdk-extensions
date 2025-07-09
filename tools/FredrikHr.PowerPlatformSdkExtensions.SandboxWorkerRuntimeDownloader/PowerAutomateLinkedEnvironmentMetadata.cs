using System.Text.Json.Serialization;

namespace FredrikHr.PowerPlatformSdkExtensions.SandboxWorkerRuntimeDownloader;

internal sealed record class PowerAutomateLinkedEnvironmentMetadata
{
    [JsonPropertyName("type")] public string? Type { get; init; }
    [JsonPropertyName("resourceId")] public required string ResourceId { get; init; }
    [JsonPropertyName("friendlyName")] public string? FriendlyName { get; init; }
    [JsonPropertyName("uniqueName")] public required string UniqueName { get; init; }
    [JsonPropertyName("domainName")] public required string DomainName { get; init; }
    [JsonPropertyName("version")] public required string Version { get; init; }
    [JsonPropertyName("instanceUrl")] public required string InstanceUrl { get; init; }
    [JsonPropertyName("instanceApiUrl")] public required string InstanceApiUrl { get; init; }
    [JsonPropertyName("baseLanguage")] public int BaseLanguage { get; init; }
    [JsonPropertyName("instanceState")] public required string InstanceState { get; init; }
    [JsonPropertyName("createdTime")] public required DateTimeOffset CreatedTime { get; init; }
}