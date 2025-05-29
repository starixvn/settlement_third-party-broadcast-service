using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.MessageBridge.Constants;
using Fanex.Settlement.MessageBridge.ResultAmendment;
using Fanex.Settlement.ScaleOut.Constants;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Models.FM;

namespace ThirdPartyBroadcast.Consumer.Services.Products.FastMarket;

public interface FastMarketResultAmendmentService
{
    Task PushResultAmendment(FastMarketRequest request, FastMarketCriteria criteria);
}

public class FastMarketResultAmendmentServiceImp(
    LogDirectlyService logDirectlyService,
    MatchService matchService,
    XmlConvertService xmlConvertService,
    IPublishEndpoint publishEndpoint) : FastMarketResultAmendmentService
{
    private const int TimeMachineHdp = 229;

    private const short TimeMachineHandicapScoreRange = 10;

    public async Task PushResultAmendment(
        FastMarketRequest request,
        FastMarketCriteria criteria)
    {
        var resources = await GetSelectedResource(request);

        var resourcesXml = new Resources
        {
            ResourceItems = resources.ToList()
        };
        var resourcesXmlString = xmlConvertService.Serialize(resourcesXml);

        await logDirectlyService.WriteLog(new InsertLogCriteria
        {
            Action = criteria.Action,
            AdminId = criteria.AdminId,
            MatchId = criteria.MatchId,
            SelectedResources = resourcesXmlString,
            Mode = Mode.FastMarket
        });

        var resultChanged = await matchService.GetResultChanged(request.MatchId, request.Action, resourcesXmlString);
        if (!resultChanged.Any())
        {
            return;
        }

        await publishEndpoint.Publish(new ResultAmended(
                request.MatchId,
                criteria.BroadcastSessionId,
                1,
                request.Action,
                resources.ToList(),
                resultChanged.ToList(),
                ResultAmendmentType.FastMarket
            ),
            new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);

        await publishEndpoint.Publish(
            new ResultAmendSettled(request.MatchId, criteria.BroadcastSessionId, criteria.IsOrg),
            new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);
    }

    private async Task<IEnumerable<ResourceItem>> GetSelectedResource(FastMarketRequest request)
    {
        var results = xmlConvertService.Deserialize<BetTypeXML>(request.Results);
        var betTypes = await matchService.GetFastMarketResult(request.MatchId);

        return results.BetType.SelectMany(betType =>
            betType.Items.Select(item => CreateResourceItem(betType, item, betTypes)));
    }

    private static ResourceItem CreateResourceItem(
        BetTypeElement betType,
        ItemElement item,
        IEnumerable<FastMarketResult> betTypes)
    {
        var matchingBetType = betTypes.FirstOrDefault(x => x.BetType == betType.Id && x.Position == item.Position);

        if (matchingBetType is null)
        {
            return new ResourceItem
            {
                ResourceId = betType.Id.ToString(),
                OtherInfo = item.Position.ToString()
            };
        }

        var isTimeMachineHdp = betType.Id == TimeMachineHdp;
        var homeValue = isTimeMachineHdp
            ? (Convert.ToInt32(matchingBetType.Result) % TimeMachineHandicapScoreRange).ToString()
            : matchingBetType.Result.ToString();

        return new ResourceItem
        {
            ResourceId = betType.Id.ToString(),
            OtherInfo = item.Position.ToString(),
            HomeValue = homeValue,
            AwayValue = isTimeMachineHdp ? homeValue : string.Empty
        };
    }
}