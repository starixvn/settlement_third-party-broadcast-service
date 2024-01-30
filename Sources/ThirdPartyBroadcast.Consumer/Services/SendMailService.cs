using Fanex.MailDeliver.MailClient.Model;
using Fanex.MailDelivery.Client;
using Fanex.MailDelivery.Core.Models;
using RazorEngine;
using RazorEngine.Templating;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface SendMailService
{
    void SendMail(NotificationModel item);

    Task SendMailByFunctionId(NotificationModel item);
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
            Subject = " Virtual Sport",
            Body = MailBody(item)
        };

        using var mailClient = new Fanex.MailDeliver.MailClient.API.MailDeliver();

        mailClient.SendMail(mailRecord);
    }

    public async Task SendMailByFunctionId(NotificationModel item)
    {
        var mail = new Mail
        {
            Subject = "Virtual Sport",
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
}