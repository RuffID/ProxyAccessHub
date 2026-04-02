using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProxyAccessHub.Application.Abstractions.Payments;
using ProxyAccessHub.Application.Models.Payments;

namespace ProxyAccessHub.Controllers;

/// <summary>
/// Контроллер приёма HTTP-уведомлений YooMoney.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("yoomoney/notification")]
public class YooMoneyNotificationController(
    IYooMoneyNotificationService yooMoneyNotificationService,
    ILogger<YooMoneyNotificationController> logger) : ControllerBase
{
    /// <summary>
    /// Принимает входящее уведомление YooMoney.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PostAsync()
    {
        IFormCollection form = await Request.ReadFormAsync(HttpContext.RequestAborted);
        bool isTestNotification = IsTestNotification(form);

        logger.LogInformation(
            "Получено уведомление YooMoney: Test={IsTestNotification}, NotificationType={NotificationType}, OperationId={OperationId}, Label={Label}, Amount={Amount}",
            isTestNotification,
            GetOptionalValue(form, "notification_type"),
            GetOptionalValue(form, "operation_id"),
            GetOptionalValue(form, "label"),
            GetOptionalValue(form, "amount"));

        if (isTestNotification)
        {
            return Ok();
        }

        YooMoneyNotificationModel notification = new(
            GetRequiredValue(form, "notification_type"),
            GetRequiredValue(form, "operation_id"),
            ParseRequiredDecimal(form, "amount"),
            ParseRequiredDecimal(form, "withdraw_amount"),
            GetRequiredValue(form, "currency"),
            GetRequiredValue(form, "datetime"),
            GetOptionalValue(form, "sender"),
            GetRequiredValue(form, "codepro"),
            GetRequiredValue(form, "label"),
            GetRequiredValue(form, "sha1_hash"),
            GetOptionalValue(form, "unaccepted"));

        await yooMoneyNotificationService.ProcessAsync(notification, HttpContext.RequestAborted);
        return Ok();
    }

    private static string GetRequiredValue(IFormCollection form, string key)
    {
        string value = GetOptionalValue(form, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"В уведомлении YooMoney отсутствует обязательное поле '{key}'.");
        }

        return value;
    }

    private static string GetOptionalValue(IFormCollection form, string key)
    {
        return form.TryGetValue(key, out Microsoft.Extensions.Primitives.StringValues values)
            ? values.ToString().Trim()
            : string.Empty;
    }

    private static decimal ParseRequiredDecimal(IFormCollection form, string key)
    {
        string value = GetRequiredValue(form, key);
        if (!decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedValue))
        {
            throw new InvalidOperationException($"Поле '{key}' уведомления YooMoney имеет неверный числовой формат.");
        }

        return parsedValue;
    }

    private static bool IsTestNotification(IFormCollection form)
    {
        string value = GetOptionalValue(form, "test_notification");
        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
