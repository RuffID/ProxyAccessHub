using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProxyAccessHub.Application.Abstractions.Payments;
using ProxyAccessHub.Application.Models.Payments;

namespace ProxyAccessHub.Pages.YooMoney;

/// <summary>
/// Endpoint приёма HTTP-уведомлений YooMoney.
/// </summary>
[IgnoreAntiforgeryToken]
public class NotificationModel(IYooMoneyNotificationService yooMoneyNotificationService) : PageModel
{
    /// <summary>
    /// Принимает входящее уведомление YooMoney.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        IFormCollection form = await Request.ReadFormAsync(HttpContext.RequestAborted);

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
        return new OkResult();
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
}
