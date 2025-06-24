using Fanex.Caching;
using ONEbook.UM.Services;
using StackExchange.Redis;
using System.Data;

namespace ThirdPartyBroadcast.Consumer.Services.Consumer;

public class ProcessingConsumerService(
    GenerateConsumerService service,
    IMaintenanceService maintenanceService,
    IConnectionMultiplexer connectionMultiplexer,
    ICache cache)
    : IConsumer<IBroadcastApiProcessing>
{
    private const string SpecifiedWord = ":space:";

    private IDatabase RedisDatabase => connectionMultiplexer.GetDatabase();

    public async Task Consume(ConsumeContext<IBroadcastApiProcessing> context)
    {
        var message = context.Message;
        var isMaintenance = await maintenanceService.IsUnderMaintenanceAsync();
        var isFlagUnderMaintenance = await GetStatusOnCache(CacheKeys.BroadcastSettleUnderMaintenanceCacheKey);
        if (isMaintenance && !isFlagUnderMaintenance)
        {
            await RedisDatabase.ListRightPushAsync(CacheKeys.BroadcastServiceProductWaitingCacheKey,
                  $"{message.ProductType}{SpecifiedWord}{message.Data}");
        }
        else
        {
            await service.SettleByProduct(message.ProductType, message.Data);
        }
    }

    private async Task<bool> GetStatusOnCache(string key)
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
}

public class MyConsumerDefinition : ConsumerDefinition<ProcessingConsumerService>
{
    private const int RetryInterval = 1000;

    private const int RetryCount = 1;

    private const int FetchItemNumber = 1;

    private const string IgnoreMessage = "SQL";

    public MyConsumerDefinition()
    {
        EndpointName = QueueName.Processing;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<ProcessingConsumerService> consumerConfigurator,
        IRegistrationContext context)
    {
        if (endpointConfigurator is not IRabbitMqReceiveEndpointConfigurator rmq)
        {
            return;
        }

        rmq.PrefetchCount = FetchItemNumber;
        rmq.DiscardSkippedMessages();
        rmq.DiscardFaultedMessages();
        rmq.Consumer<ProcessingFaultConsumerService>();
        rmq.UseMessageRetry(retryConfig =>
            {
                retryConfig.Ignore<ArgumentNullException>();
                retryConfig.Ignore<DataException>(x => x.Message.Contains(IgnoreMessage));
                retryConfig.Interval(RetryCount, TimeSpan.FromMilliseconds(RetryInterval));
            }
        );
    }
}