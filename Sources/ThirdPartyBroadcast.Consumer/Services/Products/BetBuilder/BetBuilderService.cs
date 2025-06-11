namespace ThirdPartyBroadcast.Consumer.Services.Products.BetBuilder;

using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.ThirdPartyBroadcast.Shared;
using Fanex.SiteDbConnectionManager.Client.Constants;
using Fanex.SiteDbConnectionManager.Client.Models;
using Models;
using Requests;
using ThirdPartyBroadcast.Consumer.Criteria;
using ThirdPartyBroadcast.Consumer.Criteria.BetBuilder;
using MatchService = Services.MatchService;
using ResourceItem = Fanex.Settlement.MessageBridge.ResultAmendment.ResourceItem;

public interface BetBuilderService
{
    Task<Result> Settle(BetBuilderRequest request);
}

public class BetBuilderServiceImp(
    CalculateService calculateService,
    IMapper mapper,
    BetBuilderGenerateService betBuilderGenerateService,
    XmlConvertService xmlConvertService,
    LogDirectlyService logDirectlyService,
    IBetBuilderResultAmendmentService betBuilderResultAmendmentService,
    MatchService matchService
    )
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
        var matchInfo = await matchService.GetMatchInfo(request.MatchId);

        var criteria = mapper.Map<BetBuilderCriteria>(request);
        criteria.SelectedMarket = selectedMarket;
        criteria.SelectedSelection = request.Resources;

        var resourceItems = GetResourceItems(request);

        await WriteSettlementLog(request, resourceItems, matchInfo);

        var hasResultAmend = await betBuilderResultAmendmentService.PushResultAmendment(request, resourceItems, criteria);

        var pendingCommand = CreateSiteDbPendingCommand(criteria);
        var settleResult = await calculateService.Execute(criteria, pendingCommand);

        if (hasResultAmend && settleResult.ErrorCode == 0)
        {
            await betBuilderResultAmendmentService.PushUpdateStatusResult(request.MatchId, criteria.BroadcastSessionId, criteria.IsOrg);
        }

        return settleResult;

    }

    private static SiteDbPendingCommand CreateSiteDbPendingCommand(BetBuilderCriteria criteria)
    {
        return new SiteDbPendingCommand
        {
            MatchId = criteria.MatchId,
            ProductType = ProductType.BetBuilder,
            Action = criteria.Action,
            SpName = criteria.SpName,
            Parameter = criteria.ToJson(),
            AdminId = criteria.AdminId,
            IP = criteria.Ip,
            SystemName = SystemName.Broadcast,
            StatusId = CommandStatus.Pending
        };
    }

    private async Task FilterSelectionAndLogReason(BetBuilderRequest request, IEnumerable<ReasonModel> reasonErrors)
    {
        if (reasonErrors.Any())
        {
            request.Resources = betBuilderGenerateService.RemoveSelectionWarning(request.Resources, reasonErrors);
            await betBuilderGenerateService.WriteLogReason(request, reasonErrors);
        }
    }

    private async Task WriteSettlementLog(BetBuilderRequest request, List<ResourceItem> items, MatchInfo matchInfo)
    {
        var selectionResource = GetResourcesSelectionXML(items);

        await logDirectlyService.WriteLog(new InsertLogCriteria
        {
            Action = request.Action,
            AdminId = request.AdminId,
            MatchId = request.MatchId,
            SportType = matchInfo.SportType,
            SelectedResources = selectionResource,
            Mode = "BB"
        });

    }

    private string GetResourcesSelectionXML(List<ResourceItem> items)
    {

        var resourceRoot = new Models.FM.Resources
        {
            ResourceItems = items
        };

        var selectedResources = xmlConvertService.Serialize(resourceRoot);

        return selectedResources;
    }

    private List<ResourceItem> GetResourceItems(BetBuilderRequest request)
    {
        var selection = xmlConvertService.Deserialize<Models.BetBuilder.Resources>(request.Resources);
        var items = selection.ResourceItem
            .Select(resource => new ResourceItem
            {
                ResourceId = $"{resource.Id}",
                HomeValue = resource.Result,
            })
            .ToList();

        return items;
    }
}