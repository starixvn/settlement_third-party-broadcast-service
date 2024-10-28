using ThirdPartyBroadcast.Consumer.Services.Products.HappyFive;
using ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;
using ThirdPartyBroadcast.Consumer.Services.Products.VirtualRacing;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface GroupThreeService
{
    Task SettleHappyFive(string productType, string data);

    Task SettleVirtualRacing(string productType, string data);

    Task SettleVirtualBetby(string productType, string data);
}

public class GroupThreeServiceImp(
    HappyFiveService happyFiveService,
    VirtualRacingService virtualRacingService,
    VirtualBetbyService virtualBetbyService,
    LogDirectlyService logDirectlyService) : GroupThreeService
{
    public async Task SettleHappyFive(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<HappyFiveRequest>(data);
        var result = await happyFiveService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }

    public async Task SettleVirtualRacing(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<VirtualRacingRequest>(data);
        var result = await virtualRacingService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }

    public async Task SettleVirtualBetby(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<VirtualBetbyRequest>(data);
        var result = await virtualBetbyService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }
}