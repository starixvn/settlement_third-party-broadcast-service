using ThirdPartyBroadcast.Consumer.Models.Happy5;

namespace ThirdPartyBroadcast.Consumer.Services.Products.Happy5;

public interface CommandService
{
    Task<Result> Execute(BatchBetTrans batch);
}

public class CommandServiceImp(
    IDynamicRepository repository, 
    BetTransService betTransService,
    ValidationService validationService) : CommandService
{
    public async Task<Result> Execute(BatchBetTrans batch)
    {
        var betTrans = await betTransService.GetTransId(batch);
        var maxPayoutSetting = validationService.GetMaxPayoutSetting()
        return new Result();
    }
}