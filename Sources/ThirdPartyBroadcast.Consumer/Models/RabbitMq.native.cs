namespace ThirdPartyBroadcast.Consumer.Models;

public class RabbitMq
{
    public string Hosting { get; set; }

    public string VirtualHost { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }

    public string ProcessingQueue { get; set; }

    public string WaitingQueue { get; set; }
}