namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria;

public interface OutrightService
{
    Task<Result> Settle(OutrightRequest request);
}

public class OutrightServiceImp : OutrightService
{
    private readonly IMapper mapper;

    private readonly CalculateService calculateService;

    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    public OutrightServiceImp(
        IMapper mapper,
        CalculateService calculateService,
        ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService)
    {
        this.mapper = mapper;
        this.calculateService = calculateService;
        this.scaleOutSettleThirdPartyService = scaleOutSettleThirdPartyService;
    }

    public async Task<Result> Settle(OutrightRequest request)
    {
        var validation = await ValidateBeforeSettle(request);

        if (validation.IsSuccessful)
        {
            var criteria = mapper.Map<OutrightRequest, OutrightCriteria>(request);
            return await calculateService.Execute(criteria);
        }

        return validation;
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