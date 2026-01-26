using System.Text.Json.Serialization;

namespace MinCh.Configuration;

public class AppState
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string LastRunVersion { get; set; } = "0.0.0";
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    AllowTrailingCommas = true,
    UseStringEnumConverter = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
)]
[JsonSerializable(typeof(AppState))]
public partial class AppStateContext : JsonSerializerContext;
