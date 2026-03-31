using Serilog.Core;
using Serilog.Events;

namespace ProxyAccessHub.Core;

/// <summary>
/// Перехватывает каждое событие логирования и заменяет полное имя источника на простое имя класса.
/// </summary>
public class SimpleClassNameEnricher : ILogEventEnricher
{
    /// <summary>
    /// Обогащает событие логирования простым именем класса из свойства SourceContext.
    /// </summary>
    /// <param name="logEvent">Событие логирования для обогащения.</param>
    /// <param name="propertyFactory">Фабрика свойств события логирования.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.TryGetValue("SourceContext", out LogEventPropertyValue? sourceContextValue)
            && sourceContextValue is ScalarValue scalar
            && scalar.Value is string fullName)
        {
            string simpleName = fullName.Split('.').Last();
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("SourceContext", simpleName));
        }
    }
}
