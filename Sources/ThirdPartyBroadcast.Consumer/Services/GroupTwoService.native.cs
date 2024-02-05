using ThirdPartyBroadcast.Consumer.Services.Products;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface GroupTwoService
{
    Task SettleVirtualSoccer(string productType, string data);

    Task SettleVirtualBetRadar(string productType, string data);

    Task SettleLeapGaming(string productType, string data);

    Task SettlePinGoal(string productType, string data);
}

public class GroupTwoServiceImp(
    LogDirectlyService logDirectlyService,
    VirtualBetRadarService virtualBetRadarService,
    VirtualSportsService virtualSoccerService,
    LeapGamingService leapGamingService,
    PinGoalService pinGoalService)
    : GroupTwoService
{
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

    public async Task SettleLeapGaming(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<LeapGamingRequest>(data);
        var result = await leapGamingService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }

    public async Task SettlePinGoal(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<PinGoalRequest>(data);
        var result = await pinGoalService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }
}