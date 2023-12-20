namespace ThirdPartyBroadcast.Consumer.Models;

public class AppSettingsKey
{
    public bool EnableWriteLog { get; set; }

    public List<string> SiteDbServer { get; set; }

    public MailFunction MailFunction { get; set; }
}