namespace ThirdPartyBroadcast.Consumer.Services.Products.BetBuilder;

using Models;
using Requests;
using ThirdPartyBroadcast.Consumer.Criteria.BetBuilder;

public interface BetBuilderService
{
    Task<Result> Settle(BetBuilderRequest request);
}

public class BetBuilderServiceImp(

    CalculateService calculateService,
    IMapper mapper,
    BetBuilderGenerateService betBuilderGenerateService)
    : BetBuilderService
{
    public async Task<Result> Settle(BetBuilderRequest request)
    {
        var (validationResult, reasonErrors) = await betBuilderGenerateService.ValidationBeforeSettle(request);
        var result = validationResult.FirstOrDefault();

        if (result is { ErrorCode: > 0 })
        {
            return result;
        }

        await FilterSelectionAndLogReason(request, reasonErrors);

        var selectedMarket = await betBuilderGenerateService.GetInfoMarket(request);

        await betBuilderGenerateService.UpdateMatchInfo(request, selectedMarket);

        await betBuilderGenerateService.WriteLog(request);

        if (string.IsNullOrWhiteSpace(selectedMarket))
        {
            return new Result();
        }

        var criteria = mapper.Map<BetBuilderCriteria>(request);
        criteria.SelectedMarket = selectedMarket;
        criteria.SelectedSelection = request.Resources;

        return await calculateService.Execute(criteria);
    }

    private async Task FilterSelectionAndLogReason(BetBuilderRequest request, IEnumerable<ReasonModel> reasonErrors)
    {
        if (reasonErrors.Any())
        {
            request.Resources = betBuilderGenerateService.RemoveSelectionWarning(request.Resources, reasonErrors);
            await betBuilderGenerateService.WriteLogReason(request, reasonErrors);
        }
    }
}