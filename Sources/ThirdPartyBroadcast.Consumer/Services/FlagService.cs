using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface FlagService
{
    Task<bool> IsDeployedFromCache(string key);

    uint GetMessageWaitingCount();
}

public class FlagServiceImp : FlagService
{
    private readonly ICache cache;

    private readonly AppSettingsKey appSettingsKey;

    public FlagServiceImp(ICache cache, IOptions<AppSettingsKey> appSettingsKey)
    {
        this.cache = cache;
        this.appSettingsKey = appSettingsKey.Value;
    }

    public async Task<bool> IsDeployedFromCache(string key)
    {
        try
        {
            var isExistedKey = await cache.ContainAsync(key);
            if (isExistedKey)
            {
                var (_, value) = await cache.TryGetAsync<bool>(key);
                return value;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public uint GetMessageWaitingCount()
    {
        var rabbitMq = appSettingsKey.RabbitMq;
        var connectionFactory = new ConnectionFactory
        {
            UserName = rabbitMq.Username,
            Password = rabbitMq.Password,
            HostName = rabbitMq.Hosting,
            VirtualHost = rabbitMq.VirtualHost
        };

        using var connection = connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();
        return channel.MessageCount(rabbitMq.WaitingQueue);
    }
}