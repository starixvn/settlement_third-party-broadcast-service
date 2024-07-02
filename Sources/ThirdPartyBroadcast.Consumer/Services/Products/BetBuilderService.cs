using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.ScaleOut.Constants;
using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Fanex.Settlement.ScaleOut.Models;
using Requests;
using System;

public interface BetBuilderService
{
    Task<Result> Settle(BetBuilderRequest request);
}

public class BetBuilderServiceImp(
    ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService,
    ScaleOutSettleService scaleOutSettleService,
    CalculateService calculateService,
    IMapper mapper,
    XmlConvertService xmlConvertService,
    IDynamicRepository repository,
    LogDirectlyService logDirectlyService)
    : BetBuilderService
{

    private const string InvalidMessage = "MarketIds cannot be auto settled since they have been settled already: ";

    private const string NoResultMessage = "MarketIds cannot be settled since there is no value: ";
    public async Task<Result> Settle(BetBuilderRequest request)
    {
        var (validationResult, invalidMarkets) = await ValidationBeforeSettle(request);

        if (validationResult.ErrorCode > 0)
        {
            return validationResult;
        }

        var selectedMarket = RemoveInvalidMarket(invalidMarkets, request.Resources);


        await WriteLog(request, invalidMarkets.MarketIds, InvalidMessage);
        await WriteLog(request, invalidMarkets.NoResultMarketIds, NoResultMessage);

        if (!string.IsNullOrWhiteSpace(selectedMarket))
        {
            return await UpdateAndSettle(request, selectedMarket);
        }

        return new Result();
    }

    private async Task<Result> UpdateAndSettle(BetBuilderRequest request, string selectedMarket)
    {
        await UpdateMatchInfo(request, selectedMarket);

        var criteria = mapper.Map<BetBuilderRequest, BetBuilderCriteria>(request);
        criteria.SelectedMarket = selectedMarket;

        var message = GenerateLogMessage(criteria, request.Resources);
        await logDirectlyService.WriteLog(criteria.MatchId, message);

        return await calculateService.Execute(criteria);
    }

    private static string GenerateLogMessage(BetBuilderCriteria criteria, string resources)
    {
        var messageResource = string.IsNullOrEmpty(resources) ? string.Empty : $"{resources}<br/>";
        return $"NAP: Calculate {criteria.Action} for matchId: {criteria.MatchId} {messageResource}; IsAuto:{criteria.IsAuto}; BetType:38";
    }

    private async Task WriteLog(BetBuilderRequest request, string invalidMarkets, string message)
    {
        if (!string.IsNullOrEmpty(invalidMarkets))
        {
            var logMessage = InvalidMarketMessage(invalidMarkets, message);
            await ExecuteWriteLog(request, logMessage);
        }
    }

    private string InvalidMarketMessage(string marketIds, string logMessage)
    {
        const int MaxLength = 115;
        return marketIds.Length > MaxLength ? $"{logMessage} {marketIds[..MaxLength]}..." : $"{logMessage} {marketIds}";
    }

    private async Task ExecuteWriteLog(BetBuilderRequest request, string logMessage)
    {
        var actionId = string.Equals(request.Action, Constants.ActionName.Calculate, StringComparison.OrdinalIgnoreCase) ? ActionId.BetBuilderCalc : ActionId.BetBuilderRefund;

        var criteria = new WriteLogCriteria(request.MatchId, logMessage, actionId, request.AdminId, request.Ip);
        await repository.ExecuteAsync(criteria);
    }

    private async Task UpdateMatchInfo(BetBuilderRequest request, string selectedMarket)
    {
        var requestUpdate = new UpdateMatchInfoRequest
        {
            MatchId = request.MatchId,
            Mode = Mode.BetBuilder,
            IsAuto = request.IsAuto,
            Action = request.Action,
            SelectedMarket = selectedMarket
        };

        await scaleOutSettleService.UpdateMatchInfo(requestUpdate);
    }

    private string RemoveInvalidMarket(InvalidMarkets invalidMarkets, string selectionMarket)
    {
        var listMarket = xmlConvertService.Deserialize<ResourcesMarketModel>(selectionMarket);
        var listNoResult = invalidMarkets.NoResultMarketIds.Split(',');
        var listInvalidMarket = listNoResult.Concat(invalidMarkets.MarketIds.Split(',')).ToList();
        listInvalidMarket.ForEach(item =>
        {
            var index = listMarket.Items.FindIndex(i => i.MarketId == item);
            if (index > -1)
            {
                listMarket.Items.RemoveAt(index);
            }
        });

        return listMarket.Items.Count > 0 ? xmlConvertService.Serialize(listMarket) : string.Empty;
    }

    private async Task<(Result, InvalidMarkets)> ValidationBeforeSettle(BetBuilderRequest request)
    {
        var eventTicketThirdPartyRequest = new Fanex.Settlement.ScaleOut.Request.BetBuilderRequest
        {
            MatchId = request.MatchId,
            Action = request.Action,
            IsAuto = request.IsAuto,
            SelectedMarket = request.Resources,
            VendorId = request.VendorId
        };

        var (resultValidate, invalidMarkets) = await scaleOutSettleThirdPartyService.ValidateBetBuilder(eventTicketThirdPartyRequest);

        var result = new Result
        {
            ErrorCode = resultValidate.ErrorCode,
            ErrorMessage = resultValidate.ErrorMessage
        };

        return (result, invalidMarkets);
    }
}