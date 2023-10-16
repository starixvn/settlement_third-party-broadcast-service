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
        var flag = true;
        while (flag)
        {
            if (!maintenanceService.IsMaintenance)
            {
                flag = await ProcessOnQueue();
            }
        }
    }

    private async Task<bool> ProcessOnQueue()
    {
        var isDeployed = await flagService.IsDeployedFromCache(CacheKeys.BroadcastFlagCacheKey);

        var messageWaitingCount = flagService.GetMessageWaitingCount();

        if (isDeployed || messageWaitingCount == 0)
        {
            await exchangesService.ProcessingQueueReceiver();
        }
        else
        {
            await exchangesService.WaitingQueueReceiver();
        }

        return true;
    }
}