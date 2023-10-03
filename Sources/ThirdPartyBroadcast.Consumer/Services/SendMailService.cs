using Fanex.MailDeliver.Model;
using RazorEngine;
using RazorEngine.Templating;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface SendMailService
{
    void SendMail(NotificationModel item);
}

public class SendMailServiceImp : SendMailService
{
    public void SendMail(NotificationModel item)
    {
        var mailRecord = new MailRecord
        {
            MailTo = item.To,
            MailCc = item.Copy,
            Title = "NAP - Settlement System",
            Subject = Subject(item),
            Body = MailBody(item)
        };

        using var mailClient = new Fanex.MailDeliver.MailClient.API.MailDeliver();

        mailClient.SendMail(mailRecord);
    }

    private static string Subject(NotificationModel item)
    {
        var to = string.Equals(item.Type, EmailType.Credit) ? "MBC" : "Licensee";
        return $"[Virtual Sports 3] {item.Action} Ticket Numbers ({to}) -- {item.Action} MatchID: {item.MatchInfo.MatchId} ({item.MatchInfo.SportName})";
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
}