namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria;
using Fanex.Settlement.ThirdPartyBroadcast.Shared;
using Fanex.SiteDbConnectionManager.Client.Constants;
using Fanex.SiteDbConnectionManager.Client.Models;
using ThirdPartyBroadcast.Consumer.Models;

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

        var criteria = mapper.Map<OutrightRequest, OutrightCriteria>(request);
        var pendingCommand = CreateSiteDbPendingCommand(criteria);
        return await calculateService.Execute(criteria, pendingCommand);
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

    private static SiteDbPendingCommand CreateSiteDbPendingCommand(OutrightCriteria criteria)
    {
        return new SiteDbPendingCommand
        {
            MatchId = criteria.MatchId,
            ProductType = ProductType.Outright,
            Action = criteria.Action,
            SpName = criteria.SpName,
            Parameter = criteria.ToJson(),
            AdminId = criteria.AdminId,
            IP = criteria.Ip,
            SystemName = SystemName.Broadcast,
            StatusId = CommandStatus.Pending
        };
    }
}