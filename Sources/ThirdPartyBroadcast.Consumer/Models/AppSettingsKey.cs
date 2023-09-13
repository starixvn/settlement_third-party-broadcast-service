namespace ThirdPartyBroadcast.Consumer.Models;

public class AppSettingsKey
{
    public List<string> SiteDbServer { get; set; }

    public RabbitMq RabbitMq { get; set; }
}