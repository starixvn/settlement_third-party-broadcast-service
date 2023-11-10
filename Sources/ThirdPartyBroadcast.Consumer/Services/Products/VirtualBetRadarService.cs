using ThirdPartyBroadcast.Consumer.Criteria;

namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Constants;
using Criteria.VirtualBetRadar;
using Models;
using System;

public interface VirtualBetRadarService
{
    Task<Result> Settle(VirtualBetRadarRequest request);
}

public class VirtualBetRadarServiceImp : VirtualBetRadarService
{
    private readonly IDynamicRepository repository;

    private readonly IMapper mapper;

    private readonly CalculateService calculateService;

    private readonly LogDirectlyService logDirectlyService;

    private readonly TicketSettleService ticketSettleService;

    private readonly AnnouncementService announcementService;

    public VirtualBetRadarServiceImp(
        IDynamicRepository repository,
        IMapper mapper,
        CalculateService calculateService,
        LogDirectlyService logDirectlyService,
        TicketSettleService ticketSettleService,
        AnnouncementService announcementService)
    {
        this.repository = repository;
        this.mapper = mapper;
        this.calculateService = calculateService;
        this.logDirectlyService = logDirectlyService;
        this.ticketSettleService = ticketSettleService;
        this.announcementService = announcementService;
    }

    public async Task<Result> Settle(VirtualBetRadarRequest request)
    {
        var betRadarValidationCriteria = mapper.Map<VirtualBetRadarRequest, BetRadarValidationCriteria>(request);
        var resultValidation = await calculateService.ExecuteValidationMultiServer(betRadarValidationCriteria);
        var criteria = mapper.Map<VirtualBetRadarRequest, VirtualBetRadarCriteria>(request);

        if (!resultValidation.IsSuccessful)
        {
            return resultValidation;
        }

        var currentMatchInfo = await GetMatchStatus(request.MatchId);
        var currentTickets = await ticketSettleService.GetTicketsByAction(request.MatchId, request.Action);

        await UpdateMatchInfo(request);

        var message = GenerateMessageLog(criteria);
        await logDirectlyService.WriteLog(criteria.MatchId, message);

        var result = await HandleSettle(currentTickets, currentMatchInfo, request);

        return result;
    }

    private async Task<Result> HandleSettle(List<TicketSettleInfo> currentTickets, MatchStatus matchStatus, VirtualBetRadarRequest request)
    {
        var criteria = mapper.Map<VirtualBetRadarRequest, VirtualBetRadarCriteria>(request);
        var settleResult = await calculateService.Execute(criteria);

        await HandleAfterSettle(request, settleResult, matchStatus);

        var matchInfo = new MatchInfo
        {
            KickOffTime = matchStatus.KickOffTime,
            LeagueName = matchStatus.LeagueName,
            LeagueNameCs = matchStatus.LeagueNameCs,
            LeagueNameCh = matchStatus.LeagueNameCh,
            MatchId = matchStatus.MatchId,
            LeagueGroupId = matchStatus.LeagueGroupId,
        };
        await announcementService.Handle(currentTickets, matchInfo, request.AdminId);

        return settleResult;
    }

    private static string GenerateMessageLog(VirtualBetRadarCriteria criteria)
        => $@"NAP: VS BetRadar - Calculate {criteria.Action} for matchId: {criteria.MatchId}
              Scores: FT=[{criteria.Fth}-{criteria.Fta}]; HT=[{criteria.Hth}-{criteria.Hta}]
              Resource Results: {criteria.ResourceResult}";

    private async Task HandleAfterSettle(VirtualBetRadarRequest request, Result settleResult, MatchStatus matchStatus)
    {
        var logCriteria = CreateWriteLogCriteria(request, settleResult);
        await repository.ExecuteAsync(logCriteria);

        await SendMail(logCriteria, settleResult);

        await SendNotify(request, matchStatus);
    }

    private async Task SendMail(WriteLogCriteria logCriteria, Result settleResult)
    {
        if (!settleResult.IsSuccessful)
        {
            var sendMailCriteria = mapper.Map<WriteLogCriteria, SendMailCriteria>(logCriteria);
            sendMailCriteria.Message = logCriteria.Remark;
            await repository.ExecuteAsync(sendMailCriteria);
        }
    }

    private async Task SendNotify(VirtualBetRadarRequest request, MatchStatus matchStatus)
    {
        if (IsReSettleOrRefund(request.Action, matchStatus))
        {
            var criteria = new SendNotifyBetRadarCriteria
            {
                MatchId = request.MatchId,
                NotifyAction = ActionIdValue(request.Action)
            };

            await repository.ExecuteAsync(criteria);
        }
    }

    private static WriteLogCriteria CreateWriteLogCriteria(VirtualBetRadarRequest request, Result settleResult)
    {
        var actionId = ActionIdValue(request.Action);

        var criteria = new WriteLogCriteria(request.MatchId, actionId, request.AdminId, request.Ip)
        {
            FinalAwayScore = request.Fta,
            FinalHomeScore = request.Fth,
            HtAwayScore = request.Hta,
            HtHomeScore = request.Hth,
            Remark = $"VS BetRadar - Calculate ({request.Action}) {(settleResult.ErrorCode > 0 ? settleResult.ErrorMessage : string.Empty)}" //check
        };
        return criteria;
    }

    private static int ActionIdValue(string actionName) => string.Equals(actionName, ActionName.Calculate, StringComparison.OrdinalIgnoreCase)
        ? ActionId.VirtualBetRadarCalc
        : ActionId.VirtualBetRadarRefund;

    private async Task UpdateMatchInfo(VirtualBetRadarRequest request)
    {
        var criteria = mapper.Map<VirtualBetRadarRequest, BetRadarUpdateMatchCriteria>(request);
        await repository.ExecuteAsync(criteria);
    }

    private static bool IsReSettleOrRefund(string action, MatchStatus matchStatus)
    => string.Equals(action, ActionName.Refund, StringComparison.OrdinalIgnoreCase)
       || matchStatus != null && (matchStatus.Calculate || matchStatus.CalRefund);

    private async Task<MatchStatus> GetMatchStatus(int matchId)
    {
        var criteria = new GetMatchStatusCriteria { MatchId = matchId };
        var matchStatus = await repository.GetAsync<MatchStatus>(criteria);
        matchStatus.MatchId = matchId;

        return matchStatus;
    }
}