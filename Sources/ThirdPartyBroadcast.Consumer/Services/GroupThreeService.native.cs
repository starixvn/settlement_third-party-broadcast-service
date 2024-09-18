using ThirdPartyBroadcast.Consumer.Services.Products.VirtualRacing;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface GroupThreeService
{
    Task SettleVirtualRacing(string productType, string data);
}

public class GroupThreeServiceImp(
    VirtualRacingService virtualRacingService,
    LogDirectlyService logDirectlyService) : GroupThreeService
{
    public async Task SettleVirtualRacing(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<VirtualRacingRequest>(data);
        var result = await virtualRacingService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }
}