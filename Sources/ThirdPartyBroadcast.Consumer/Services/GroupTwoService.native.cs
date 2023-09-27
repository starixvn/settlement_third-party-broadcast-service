using ThirdPartyBroadcast.Consumer.Services.Products;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface GroupTwoService
{
    Task SettleVirtualSoccer(string productType, string data);

    Task SettleVirtualBetRadar(string productType, string data);
}

public class GroupTwoServiceImp : GroupTwoService
{
    private readonly LogDirectlyService logDirectlyService;

    private readonly VirtualSoccerService virtualSoccerService;

    private readonly VirtualBetRadarService virtualBetRadarService;

    public GroupTwoServiceImp(
         LogDirectlyService logDirectlyService,
         VirtualBetRadarService virtualBetRadarService,
         VirtualSoccerService virtualSoccerService)
    {
        this.logDirectlyService = logDirectlyService;
        this.virtualBetRadarService = virtualBetRadarService;
        this.virtualSoccerService = virtualSoccerService;
    }

    public async Task SettleVirtualSoccer(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<VirtualSoccerRequest>(data);
        var result = await virtualSoccerService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }

    public async Task SettleVirtualBetRadar(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<VirtualBetRadarRequest>(data);
        var result = await virtualBetRadarService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }
}