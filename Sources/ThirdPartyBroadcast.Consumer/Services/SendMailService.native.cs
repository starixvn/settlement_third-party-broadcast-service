using Fanex.MailDelivery.Client;
using Fanex.MailDelivery.Core.Models;
using RazorEngineCore;
using System.Collections.Concurrent;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface SendMailService
{
    Task SendMailByFunctionId(NotificationModel item);
}

public class SendMailServiceImp : SendMailService
{
    private static readonly ConcurrentDictionary<int, IRazorEngineCompiledTemplate> CompiledTemplateCache = new();

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
        var templateFileName = item.Type.Equals(EmailType.Credit)
            ? "credit-template.cshtml"
            : "deposit-template.cshtml";

        var templatePath = Path.Combine("Templates", templateFileName);
        var hashCode = templatePath.GetHashCode();
        var compiledTemplate = CompiledTemplateCache.GetOrAdd(hashCode, _ =>
        {
            var template = File.ReadAllText(templatePath);
            var razorEngine = new RazorEngine();
            return razorEngine.Compile(template);
        });

        return compiledTemplate.Run(item);
    }

    private static string Subject(NotificationModel item)
    {
        var to = string.Equals(item.Type, EmailType.Credit) ? "MBC" : "Licensee";
        return $"[Virtual Sports] {item.Action} Ticket Numbers ({to}) -- {item.Action} MatchId: {item.MatchInfo.MatchId} ({item.MatchInfo.SportName})";
    }
}