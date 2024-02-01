using Fanex.MailDelivery.Client;
using Fanex.MailDelivery.Core.Models;
using RazorEngine;
using RazorEngine.Templating;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface SendMailService
{
    Task SendMailByFunctionId(NotificationModel item);
}

public class SendMailServiceImp : SendMailService
{
    public async Task SendMailByFunctionId(NotificationModel item)
    {
        var mail = new Mail
        {
            Subject = Subject(item),
            Title = "NAP - Settlement System",
            Body = MailBody(item),
            MailingListId = item.MailId
        };

        await MailClientHandler.SendFunctionMailAsync(mail);
    }

    private static string MailBody(NotificationModel item)
    {
        var templateKey = "deposit-key";
        var template = $"{AppDomain.CurrentDomain.BaseDirectory}Templates{Path.DirectorySeparatorChar}deposit-template.cshtml";

        if (item.Type.Equals(EmailType.Credit))
        {
            templateKey = "credit-key";
            template = $"{AppDomain.CurrentDomain.BaseDirectory}Templates{Path.DirectorySeparatorChar}credit-template.cshtml";
        }

        return Engine.Razor.RunCompile(File.ReadAllText(template), templateKey, model: item);
    }

    private static string Subject(NotificationModel item)
    {
        var to = string.Equals(item.Type, EmailType.Credit) ? "MBC" : "Licensee";
        return $"[Virtual Sports] {item.Action} Ticket Numbers ({to}) -- {item.Action} MatchId: {item.MatchInfo.MatchId} ({item.MatchInfo.SportName})";
    }
}