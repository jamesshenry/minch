using System.Text.Json;
using System.Text.Json.Serialization;
using MinCh.Library.Git;

namespace MinCh.Library.Rendering;

public class JsonChangeSetRenderer : IChangeSetRenderer
{
    public string Render(ChangeSet changeSet)
    {
        return JsonSerializer.Serialize(changeSet, ChangeSetContext.Default.ChangeSet);
    }
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    AllowTrailingCommas = true,
    UseStringEnumConverter = true,
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
[JsonSerializable(typeof(ChangeSet))]
public partial class ChangeSetContext : JsonSerializerContext { }
