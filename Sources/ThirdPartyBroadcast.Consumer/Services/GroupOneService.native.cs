using ThirdPartyBroadcast.Consumer.Services.Products;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface GroupOneService
{
    Task SettleOutright(string productType, string data);

    Task SettleBitCoin(string productType, string data);
}

public class GroupOneServiceImp : GroupOneService
{
    private readonly LogDirectlyService logDirectlyService;

    private readonly OutrightService outrightService;

    private readonly BitCoinService bitCoinService;

    public GroupOneServiceImp(
        OutrightService outrightService,
        BitCoinService bitCoinService,
        LogDirectlyService logDirectlyService)
    {
        this.outrightService = outrightService;
        this.bitCoinService = bitCoinService;
        this.logDirectlyService = logDirectlyService;
    }

    public async Task SettleOutright(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<AutoOutrightRequest>(data);
        var result = await outrightService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }

    public async Task SettleBitCoin(string productType, string data)
    {
        var request = JsonConvert.DeserializeObject<AutoBitCoinRequest>(data);
        var result = await bitCoinService.Settle(request);
        await logDirectlyService.HandleLog(result, productType, data);
    }
}