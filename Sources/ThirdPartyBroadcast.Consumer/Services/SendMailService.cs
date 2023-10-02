using Fanex.MailDeliver.Model;
using Microsoft.AspNetCore.Hosting;
using RazorEngine;
using RazorEngine.Templating;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface SendMailService
{
    void SendMail(NotificationModel item);
}

public class SendMailServiceImp : SendMailService
{
    private readonly IWebHostEnvironment environment;

    public SendMailServiceImp(IWebHostEnvironment environment)
    {
        this.environment = environment;
    }

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
        var to = item.Type == EmailType.Credit ? "MBC" : "Licensee";
        return $"[Virtual Sports 3] {item.Action} Ticket Numbers ({to}) -- {item.Action} MatchID: {item.MatchInfo.MatchId} ({item.MatchInfo.SportName})";
    }

    private string MailBody(NotificationModel item)
    {
        var filePath = environment.WebRootPath;
        var templateKey = "deposit-key";
        var template = $"{Path.Combine(filePath, "email-templates", "deposit-template.html")}";

        if (item.Type.Equals(EmailType.Credit))
        {
            templateKey = "credit-key";
            template = $"{Path.Combine(filePath, "email-templates", "credit-template.html")}";
        }

        return Engine.Razor.RunCompile(File.ReadAllText(template), templateKey, null, item);
    }
}