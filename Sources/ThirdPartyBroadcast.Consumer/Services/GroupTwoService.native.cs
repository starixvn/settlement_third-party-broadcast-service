using Newtonsoft.Json;
using ThirdPartyBroadcast.Consumer.Services.Products;
using ThirdPartyBroadcast.Consumer.Services.Products.VirtualSportFive;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface GroupTwoService
{
    Task SettleVirtualSoccer(string productType, string data);

    Task SettleVirtualBetRadar(string productType, string data);

    Task SettleLeapGaming(string productType, string data);

    Task SettlePinGoal(string productType, string data);

    Task SettleVirtualSportFive(string productType, string data);
}

public class GroupTwoServiceImp(
    LogDirectlyService logDirectlyService,
    VirtualBetRadarService virtualBetRadarService,
    VirtualSportsService virtualSoccerService,
    LeapGamingService leapGamingService,
    PinGoalService pinGoalService,
    Product3rdService product3rdService)
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
        var result = await leapGamingService.Settle(request, productType);
        await logDirectlyService.HandleLog(result, productType, data);
    }

    public async Task SettlePinGoal(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<PinGoalRequest>(data);
        var result = await pinGoalService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }

    public async Task SettleVirtualSportFive(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<VirtualSportFiveRequest>(data);
        var result = await product3rdService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }
}