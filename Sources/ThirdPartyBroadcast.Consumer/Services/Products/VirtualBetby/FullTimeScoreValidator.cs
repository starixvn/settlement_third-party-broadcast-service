namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

public class FullTimeScoreValidator(ScoreService scoreService) : IValidator
{
    private IValidator validator;

    public IValidator SetNext(IValidator next)
    {
        validator = next;
        return validator;
    }

    public Result Validate(VirtualBetbyRequest request, IEnumerable<ResourceItemModel> currentResources)
    {
        var ftResult = request.Resources.Exists(r => r.Id == ResourceId.FullTimeResourceId);

        if (ftResult)
        {
            return ValidateFtConsistency(request, currentResources);
        }

        return validator?.Validate(request, currentResources) ?? new Result();
    }

    private Result ValidateFtConsistency(VirtualBetbyRequest request, IEnumerable<ResourceItemModel> currentResources)
    {
        var resources = scoreService.GetMarkedList(request.Resources, currentResources);

        request.Resources = resources;

        if (!scoreService.CheckForAllQuarters(resources))
        {
            const string message = "Cannot Calculate FT due to all quarters have not been calculated or selected to calculate.";
            return new Result(1, message);
        }
        var fullTime = resources.Find(r => r.Id == ResourceId.FullTimeResourceId);

        if (fullTime != null && fullTime.Home.Equals(fullTime.Away))
        {
            var message = $"Cannot calculate FT({fullTime.Home} - {fullTime.Away}) due to FT should not be draw.";
            return new Result(1, message);
        }

        var quarters = resources.Where(r => r.Id != ResourceId.FullTimeResourceId
                                            && r.Id != ResourceId.HalfTimeResourceId);

        if (!scoreService.IsScoreEqualToQuarterScores(quarters, fullTime))
        {
            var message = scoreService.GetScoreMessage(resources, string.Empty);
            return new Result(1, $"Inconsistent value {message}.");
        }

        return new Result();
    }
}