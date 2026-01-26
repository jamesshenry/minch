using Serilog.Core;
using Serilog.Events;

namespace MinCh.Logging;

public class SourceClassEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (
            logEvent.Properties.TryGetValue("SourceContext", out var value)
            && value is ScalarValue sv
            && sv.Value is string fullName
        )
        {
            var shortName = fullName.Split('.').Last();
            var property = propertyFactory.CreateProperty("SourceClass", shortName);
            logEvent.AddOrUpdateProperty(property);
        }
    }
}
