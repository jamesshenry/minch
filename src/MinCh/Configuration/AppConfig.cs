using System.Text.Json.Serialization;

namespace MinCh.Configuration;

public class AppConfig
{
    public int Port { get; set; }
    public bool Enabled { get; set; }
    public string? ApiUrl { get; set; }
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    AllowTrailingCommas = true,
    UseStringEnumConverter = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true
)]
[JsonSerializable(typeof(AppConfig))]
public partial class AppConfigContext : JsonSerializerContext;
