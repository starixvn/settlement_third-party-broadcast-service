namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria;

public interface OutrightService
{
    Task<Result> Settle(OutrightRequest request);
}

public class OutrightServiceImp(
    IMapper mapper,
    CalculateService calculateService,
    ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService)
    : OutrightService
{
    public async Task<Result> Settle(OutrightRequest request)
    {
        var validation = await ValidateBeforeSettle(request);

        if (!validation.IsSuccessful)
        {
            return validation;
        }

        var criteria = mapper.Map<OutrightCriteria>(request);
        return await calculateService.Execute(criteria);
    }

    private async Task<Result> ValidateBeforeSettle(OutrightRequest request)
    {
        const string eventTable = "outright";
        var result = await scaleOutSettleThirdPartyService.ValidateMoveBetDay(request.MatchId.ToString(), eventTable);
        return new Result
        {
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };
    }
}