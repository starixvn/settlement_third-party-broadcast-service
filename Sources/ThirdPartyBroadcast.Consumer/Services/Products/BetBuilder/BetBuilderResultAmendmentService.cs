using Fanex.Settlement.Converter.Services;

namespace ThirdPartyBroadcast.Consumer.Services.Products.BetBuilder;

public class BetBuilderResultAmendmentService
{
    private readonly LogDirectlyService logDirectlyService;
    private readonly MatchService matchService;
    private readonly XmlConvertService xmlConvertService;
    private readonly IPublishEndpoint publishEndpoint;
    public BetBuilderResultAmendmentService(
        LogDirectlyService logDirectlyService,
        MatchService matchService,
        XmlConvertService xmlConvertService,
        IPublishEndpoint publishEndpoint)
    {
        this.logDirectlyService = logDirectlyService;
        this.matchService = matchService;
        this.xmlConvertService = xmlConvertService;
        this.publishEndpoint = publishEndpoint;
    }
    public async Task Handle(BetBuilderRequest request)
    {
        var resources = await matchService.GetResultChanged(request.MatchId, request.Action, request.Resources);
    }
}
