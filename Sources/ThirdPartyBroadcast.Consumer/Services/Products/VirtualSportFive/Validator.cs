namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualSportFive;

public interface IValidator
{
    Result Validate(ResourceItemModel htResult, ResourceItemModel ftResult, MatchInfo matchInfo);

    IValidator SetNext(IValidator next);
}

public static class ValidatorFactory
{
    public static IValidator CreateValidatorChain()
    {
        var correctScoreValidator = new CorrectScoreValidator();
        var matchScoreValidator = new MatchScoreValidator();
        var missingResultValidator = new MissingResultValidator();

        correctScoreValidator.SetNext(matchScoreValidator)
                              .SetNext(missingResultValidator);

        return correctScoreValidator;
    }
}