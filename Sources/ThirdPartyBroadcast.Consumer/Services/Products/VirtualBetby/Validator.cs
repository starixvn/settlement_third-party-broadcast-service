namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

public interface IValidator
{
    Result Validate(VirtualBetbyRequest request, IEnumerable<ResourceItemModel> currentResources);

    IValidator SetNext(IValidator next);
}

public static class ValidatorFactory
{
    public static IValidator CreateValidatorChain()
    {
        var scoreService = new ScoreServiceImp();
        var halfTimeScoreValidator = new HalfTimeScoreValidator(scoreService);
        var fullTimeScoreValidator = new FullTimeScoreValidator(scoreService);

        halfTimeScoreValidator.SetNext(fullTimeScoreValidator);

        return halfTimeScoreValidator;
    }
}