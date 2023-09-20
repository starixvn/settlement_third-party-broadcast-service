namespace ThirdPartyBroadcast.Consumer.Services.Consumer;

public interface BroadcastConsumerController
{
    Task Process();
}

public class BroadcastConsumerControllerImp : BroadcastConsumerController
{
    private readonly IMaintenanceService maintenanceService;

    private readonly FlagService flagService;

    private readonly ExchangesService exchangesService;

    public BroadcastConsumerControllerImp(
        IMaintenanceService maintenanceService,
        FlagService flagService,
        ExchangesService exchangesService)
    {
        this.maintenanceService = maintenanceService;
        this.flagService = flagService;
        this.exchangesService = exchangesService;
    }

    public async Task Process()
    {
        while (true)
        {
            if (!maintenanceService.IsMaintenance)
            {
                await ProcessOnQueue();
            }
        }
    }

    private async Task ProcessOnQueue()
    {
        var isDeployed = await flagService.IsDeployedFromCache(CacheKeys.BroadcastFlagCacheKey);

        if (isDeployed)
        {
            await exchangesService.ProcessingQueueReceiver();
        }
        else
        {
            await exchangesService.WaitingQueueReceiver();
            await exchangesService.ProcessingQueueReceiver();
        }
    }
}