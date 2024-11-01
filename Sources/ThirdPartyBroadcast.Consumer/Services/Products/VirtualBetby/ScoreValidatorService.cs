namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

public interface ScoreValidatorService
{
    Result Validate(VirtualBetbyRequest request, IEnumerable<ResourceItemModel> currentResources);
}

public class ScoreValidatorServiceImp(FullTimeScoreService fullTimeScoreService) : ScoreValidatorService
{
    public Result Validate(VirtualBetbyRequest request, IEnumerable<ResourceItemModel> currentResources)
    {
        var finalResources = currentResources
            .Concat(request.Resources)
            .GroupBy(item => item.Id)
            .Select(group => group.Last())
            .ToList();

        request.Resources = finalResources;
        var fullTime = finalResources.Find(r => r.Id == ResourceId.FullTimeResourceId);

        if (!fullTimeScoreService.CheckForAllQuarters(finalResources))
        {
            const string message = "Cannot Calculate FT due to all quarters have not been calculated or selected to calculate.";
            return GetResultMessage(1, message);
        }
        var quarters = finalResources.Where(r => r.Id != ResourceId.FullTimeResourceId
                                                 && r.Id != ResourceId.HalfTimeResourceId);

        if (!fullTimeScoreService.IsFullTimeEqualToQuarterScores(quarters, fullTime))
        {
            var message = fullTimeScoreService.GetInconsistentMessage(finalResources);
            return GetResultMessage(2, $"Inconsistent value {message}.");
        }

        if (fullTime != null && fullTime.Home.Equals(fullTime.Away))
        {
            var message = $"Cannot calculate FT({fullTime.Home} - {fullTime.Away}) due to FT should not be draw.";
            return GetResultMessage(3, message);
        }

        return new Result();
    }

    private static Result GetResultMessage(int errorCode, string errorMessage)
    {
        return new Result
        {
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}