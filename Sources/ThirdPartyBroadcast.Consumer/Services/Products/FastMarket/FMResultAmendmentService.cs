using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.MessageBridge.Constants;
using Fanex.Settlement.MessageBridge.ResultAmendment;
using Fanex.Settlement.ScaleOut.Constants;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Models.FM;

namespace ThirdPartyBroadcast.Consumer.Services.Products.FastMarket;

public interface FMResultAmendmentService
{
    Task PushResultAmendment(FastMarketRequest request, FastMarketCriteria criteria);
}
public class FMResultAmendmentServiceImp(
    LogDirectlyService logDirectlyService,
    MatchService matchService,
    XmlConvertService xmlConvertService,
    IPublishEndpoint publishEndpoint) : FMResultAmendmentService
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

        var resultchanged = await matchService.GetResultChanged(request.MatchId, request.Action, resourcesXmlString);
        if (!resultchanged.Any())
        {
            return;
        }

        await publishEndpoint.Publish(new ResultAmended(
                request.MatchId,
                criteria.BroadcastSessionId,
                1,
                request.Action,
                resources.ToList(),
                resultchanged.ToList(),
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

        var bettypes = await matchService.GetFastMarketResult(request.MatchId);

        var resources = results.BetType.SelectMany(b =>
        {
            return b.Items.Select(i =>
            {
                var value = bettypes.FirstOrDefault(x => x.BetType == b.Id && x.Position == i.Position);
                if (value is null)
                {
                    return new ResourceItem
                    {
                        ResourceId = b.Id.ToString(),
                        OtherInfo = i.Position.ToString()
                    };
                }
                return new ResourceItem
                {
                    ResourceId = b.Id.ToString(),
                    OtherInfo = i.Position.ToString(),
                    HomeValue = b.Id == TimeMachineHdp
                        ? (Convert.ToInt32(value.Result) % TimeMachineHandicapScoreRange).ToString()
                        : value.Result.ToString(),
                    AwayValue = b.Id == TimeMachineHdp
                        ? (Convert.ToInt32(value.Result) % TimeMachineHandicapScoreRange).ToString()
                        : string.Empty

                };
            });
        });

        return resources;
    }
}
