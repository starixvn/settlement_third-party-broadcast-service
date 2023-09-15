using Fanex.Logging;
using ThirdPartyBroadcast.Consumer.Services.Products;

namespace ThirdPartyBroadcast.Consumer.Services;

public interface GroupOneService
{
    Task SettleOutright(AutoOutrightRequest request);

    Task SettleBitCoin(AutoBitCoinRequest request);
}

public class GroupOneServiceImp : GroupOneService
{
    private const string ErrorMessage = "Error from Settlement ThirdPartyBroadcast Service";

    private readonly OutrightService outrightService;

    private readonly BitCoinService bitCoinService;

    public GroupOneServiceImp(
        OutrightService outrightService,
        BitCoinService bitCoinService)
    {
        this.outrightService = outrightService;
        this.bitCoinService = bitCoinService;
    }

    public async Task SettleOutright(AutoOutrightRequest request)
    {
        var result = await outrightService.Settle(request);
        if (result.ErrorCode > 0)
        {
            await LogService.ErrorAsync($"{ErrorMessage}: {result.ErrorMessage}");
        }
    }

    public async Task SettleBitCoin(AutoBitCoinRequest request)
    {
        var result = await bitCoinService.Settle(request);
        if (result.ErrorCode > 0)
        {
            await LogService.ErrorAsync($"{ErrorMessage}: {result.ErrorMessage}");
        }
    }
}