namespace ProxyAccessHub.Infrastructure.Telemt.Models;

/// <summary>
/// Успешная JSON-обёртка telemt API.
/// </summary>
/// <typeparam name="TData">Тип полезных данных.</typeparam>
internal sealed class TelemtSuccessEnvelope<TData>
{
    /// <summary>
    /// Признак успешного ответа.
    /// </summary>
    public bool Ok { get; set; }

    /// <summary>
    /// Полезные данные ответа.
    /// </summary>
    public TData? Data { get; set; }

    /// <summary>
    /// Ревизия конфигурации telemt.
    /// </summary>
    public string Revision { get; set; } = string.Empty;
}
