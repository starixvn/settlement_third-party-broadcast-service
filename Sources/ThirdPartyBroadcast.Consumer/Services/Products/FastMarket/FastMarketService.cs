using Fanex.Settlement.MessageBridge.Constants;
using Fanex.Settlement.MessageBridge.ResultAmendment;
using Fanex.Settlement.ScaleOut.Constants;
using ThirdPartyBroadcast.Consumer.Models.FM;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria;
using Fanex.Settlement.Converter.Services;
using Requests;
using ResourceItem = Fanex.Settlement.MessageBridge.ResultAmendment.ResourceItem;

public interface FastMarketService
{
    Task<Result> Settle(FastMarketRequest request);
}

public class FastMarketServiceImp(
    ScaleOutSettleService scaleOutSettleService,
    IMapper mapper,
    CalculateService calculateService,
    LogDirectlyService logDirectlyService,
    MatchService matchService,
    XmlConvertService xmlConvertService,
    IPublishEndpoint publishEndpoint)
    : FastMarketService
{
    private const int TimeMachineHdp = 229;

    private const short TimeMachineHandicapScoreRange = 10;

    public async Task<Result> Settle(FastMarketRequest request)
    {
        var criteria = mapper.Map<FastMarketRequest, FastMarketCriteria>(request);
        var requestValidation = mapper.Map<FastMarketCriteria, EventTicketFastMarketRequest>(criteria);
        var validationResult = await scaleOutSettleService.ValidateEventAndTicketFastMarket(requestValidation);

        if (validationResult.ErrorCode != 0)
        {
            return new Result
            {
                ErrorCode = validationResult.ErrorCode,
                ErrorMessage = validationResult.ErrorMessage
            };
        }

        await UpdateMatchInfo(criteria);

        var message = GenerateLogMessage(criteria);
        await logDirectlyService.WriteLog(criteria.MatchId, message);

        var result = await calculateService.Execute(criteria);
        if (result.IsSuccessful)
        {
            await PushResultAmendment(request, criteria);
        }

        return result;

    }

    private async Task PushResultAmendment(FastMarketRequest request, FastMarketCriteria criteria)
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
    }
    private static string GenerateLogMessage(FastMarketCriteria criteria)
        => $"NAP: Fast Market {criteria.Action} for matchId: {criteria.MatchId}";

    private async Task UpdateMatchInfo(FastMarketCriteria criteria)
    {
        var requestUpdateInfo = new UpdateMatchInfoRequest
        {
            MatchId = criteria.MatchId,
            Action = criteria.Action,
            AdminId = criteria.AdminId,
            Results = criteria.Results,
            Mode = Mode.FastMarket,
            IsSettleDetermined = criteria.IsSettleDetermined,
            IsAuto = criteria.IsAuto
        };

        await scaleOutSettleService.UpdateMatchInfo(requestUpdateInfo);
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