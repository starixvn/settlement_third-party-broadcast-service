namespace ThirdPartyBroadcast.Consumer.Services.Products;

using Criteria.VirtualSoccer;

public interface VirtualSoccerService
{
    Task<Result> Settle(VirtualSoccerRequest request);
}

public class VirtualSoccerServiceImp : VirtualSoccerService
{
    private readonly ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService;

    private readonly IDynamicRepository repository;

    private readonly IMapper mapper;

    private readonly CalculateService calculateService;

    private readonly LogDirectlyService logDirectlyService;

    public VirtualSoccerServiceImp(ScaleOutSettleThirdPartyService scaleOutSettleThirdPartyService,
        IDynamicRepository repository,
        IMapper mapper,
        CalculateService calculateService,
        LogDirectlyService logDirectlyService)
    {
        this.scaleOutSettleThirdPartyService = scaleOutSettleThirdPartyService;
        this.repository = repository;
        this.mapper = mapper;
        this.calculateService = calculateService;
        this.logDirectlyService = logDirectlyService;
    }

    public async Task<Result> Settle(VirtualSoccerRequest request)
    {
        await UpdateMatchInfo(request);
        var criteria = mapper.Map<VirtualSoccerRequest, VirtualSoccerCriteria>(request);
        var message = GenerateMessageLog(criteria);
        await logDirectlyService.WriteLog(criteria.MatchId, message);

        return await calculateService.Execute(criteria);
    }

    private static string GenerateMessageLog(VirtualSoccerCriteria criteria)
        => $"NAP: Calculate {criteria.Action} for matchId: {criteria.MatchId} score: {criteria.Fth}-{criteria.Fta}";

    private async Task UpdateMatchInfo(VirtualSoccerRequest request)
    {
        var updateInfoRequest = mapper.Map<VirtualSoccerRequest, UpdateMatchInfo3RdRequest>(request);
        var criteria = mapper.Map<VirtualSoccerRequest, VrSoccerUpdateMatchCriteria>(request);

        if (request.Action.ToLowerInvariant().Equals(ActionType.Calculate))
        {
            updateInfoRequest.FTAway = request.Fta;
            updateInfoRequest.FTHome = request.Fth;
            criteria.Command = "C";
            updateInfoRequest.Command = $"{SpName.VirtualSports}-CAL";
        }
        else
        {
            criteria.Command = "F";
            updateInfoRequest.Command = $"{SpName.VirtualSports}-REFUND";
        }

        await scaleOutSettleThirdPartyService.UpdateMatchInfo3rd(updateInfoRequest);
        await repository.ExecuteAsync(criteria);
    }
}