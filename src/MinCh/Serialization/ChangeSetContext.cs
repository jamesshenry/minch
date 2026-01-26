using System.Text.Json.Serialization;
using MinCh.Library.Git;

namespace MinCh.Services;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    AllowTrailingCommas = true,
    UseStringEnumConverter = true,
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
[JsonSerializable(typeof(ChangeSet))]
public partial class ChangeSetContext : JsonSerializerContext { }
