using System.Text;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services.Products.BetBuilder;

using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.ScaleOut.Constants;
using Fanex.Settlement.ScaleOut.Services;
using Requests;
using System.Linq;
using ThirdPartyBroadcast.Consumer.Criteria.BetBuilder;

public interface BetBuilderGenerateService
{
    Task<(IEnumerable<Result>, IEnumerable<ReasonModel>)> ValidationBeforeSettle(BetBuilderRequest request);

    Task WriteLogReason(BetBuilderRequest request, IEnumerable<ReasonModel> reasonError);

    string RemoveSelectionWarning(string resources, IEnumerable<ReasonModel> reasonError);

    Task<string> GetInfoMarket(BetBuilderRequest request);

    Task UpdateMatchInfo(BetBuilderRequest request, string selectedMarket);

    Task WriteLog(BetBuilderRequest request);
}

public class BetBuilderGenerateServiceImp(
    ScaleOutSettleService scaleOutSettleService,
    IDynamicRepository repository,
    LogDirectlyService logDirectlyService,
    XmlConvertService xmlConvertService) : BetBuilderGenerateService
{
    public async Task<(IEnumerable<Result>, IEnumerable<ReasonModel>)> ValidationBeforeSettle(BetBuilderRequest request)
    {
        var criteria = new ValidateBetBuilderCriteria
        {
            MatchId = request.MatchId,
            IsAuto = request.IsAuto,
            SelectedSelection = request.Resources,
        };
        return await repository.FetchMultipleAsync<Result, ReasonModel>(criteria);
    }

    public async Task WriteLogReason(BetBuilderRequest request, IEnumerable<ReasonModel> reasonError)
    {
        var logMessage = new StringBuilder();
        foreach (var item in reasonError)
        {
            logMessage.Append($"Failed Auto Payout: Selection: {item.SelectionType} Reason: {item.Reason}.<br/>");
        }

        var criteria = new WriteLogCriteria(request.MatchId, logMessage.ToString(), ActionId.BetBuilderCalc, request.AdminId, request.Ip);
        await repository.ExecuteAsync(criteria);
    }

    public string RemoveSelectionWarning(string resources, IEnumerable<ReasonModel> reasonError)
    {
        var selection = xmlConvertService.Deserialize<Models.BetBuilder.Resources>(resources);
        selection.ResourceItem.RemoveAll(item => reasonError.Any(reason => reason.SelectionType.Equals(item.Id)));

        return selection.ResourceItem.Count > 0 ? xmlConvertService.Serialize(selection) : string.Empty;
    }

    public async Task<string> GetInfoMarket(BetBuilderRequest request)
    {
        var getMarketIdCriteria = new GetMarketIdCriteria
        {
            MatchId = request.MatchId,
            Action = request.Action,
            SelectedSelection = request.Resources
        };

        var listMarketIds = (await repository.FetchAsync<string>(getMarketIdCriteria)).ToList();
        if (listMarketIds.Count == 0)
        {
            return string.Empty;
        }

        var resources = new ResourcesMarketModel
        {
            Items = listMarketIds.Select(item => new ResourceMarketItemModel { MarketId = item }).ToList()
        };

        return xmlConvertService.Serialize(resources);
    }

    public async Task UpdateMatchInfo(BetBuilderRequest request, string selectedMarket)
    {
        if (!string.IsNullOrWhiteSpace(request.Resources))
        {
            var requestUpdate = new UpdateMatchInfoRequest
            {
                MatchId = request.MatchId,
                Mode = Mode.BetBuilder,
                IsAuto = request.IsAuto,
                Action = request.Action,
                SelectedMarket = selectedMarket,
                SelectedSelection = request.Resources
            };

            await scaleOutSettleService.UpdateMatchInfo(requestUpdate);
        }
    }

    public async Task WriteLog(BetBuilderRequest request)
    {
        var message = GenerateLogMessage(request);
        await logDirectlyService.WriteLog(request.MatchId, message);
    }

    private static string GenerateLogMessage(BetBuilderRequest request)
    {
        var messageResource = string.IsNullOrEmpty(request.Resources) ? string.Empty : $" {request.Resources};<br/>";
        return $"NAP: Calculate {request.Action} for matchId: {request.MatchId}{messageResource} IsAuto:{request.IsAuto}; BetType:38";
    }
}