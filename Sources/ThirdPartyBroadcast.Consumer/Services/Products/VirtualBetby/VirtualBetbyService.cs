using System.Diagnostics;
using ThirdPartyBroadcast.Consumer.Criteria.VirtualBetby;
using ThirdPartyBroadcast.Consumer.Extensions;

namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

public interface VirtualBetbyService
{
    Task<Result> Settle(VirtualBetbyRequest request);
}

public class VirtualBetbyServiceImp(
    CalculateService calculateService,
    MatchService matchService,
    ResultDetailService resultDetailService,
    AnnouncementService announcementService,
    RecipientMessageService recipientMessageService,
    TicketInfoService ticketService,
    VirtualBetbyGenerateService virtualBetbyGenerateService)
    : VirtualBetbyService
{
    public async Task<Result> Settle(VirtualBetbyRequest request)
    {
        var matchInfo = await matchService.GetMatch3rdInfo(request.MatchId);
        var currentResources = await resultDetailService.GetResultDetail3rd(request.MatchId);

        var validationResult = virtualBetbyGenerateService.ValidateResources(request, currentResources);
        if (!validationResult.IsSuccessful)
        {
            return validationResult;
        }

        var currentTickets = await ticketService.GetTicketsByAction(request.MatchId, request.SportType, request.Action, request.Resources);

        request.SelectedResources = request.Resources.ToStrings();

        await virtualBetbyGenerateService.UpdateResultDetail3rdInfo(request);

        await virtualBetbyGenerateService.UpdateMatch3rdInfo(request, matchInfo);

        await virtualBetbyGenerateService.WriteLogPreSettle(request);

        return await HandleSettle(currentTickets, matchInfo, request);
    }

    private async Task<Result> HandleSettle(List<TicketSettleInfo> currentTickets, MatchInfo matchInfo, VirtualBetbyRequest request)
    {
        var criteria = new SettleVirtualBetbyCriteria
        {
            MatchId = request.MatchId,
            SportType = matchInfo.SportType,
            Action = request.Action,
            ActionId = request.ActionId,
            SelectedResources = request.SelectedResources,
            IsReSettledTickets = request.IsReSettledTickets,
            AdminId = request.AdminId,
            Ip = request.Ip,
            GameSession = request.GameSession
        };
        var watch = Stopwatch.StartNew();
        var settleResult = await calculateService.Execute(criteria);
        watch.Stop();

        await virtualBetbyGenerateService.WriteLogSettled(request, settleResult, watch.ElapsedMilliseconds);

        await recipientMessageService.Message3rd(request.Action, matchInfo, request.Resources);

        await announcementService.SendAnnounceRefund(currentTickets, matchInfo, request.AdminId);

        return settleResult;
    }
}