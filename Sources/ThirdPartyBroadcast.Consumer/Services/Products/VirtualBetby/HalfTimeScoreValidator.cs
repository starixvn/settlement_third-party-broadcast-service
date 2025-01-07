namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

public class HalfTimeScoreValidator(ScoreService scoreService) : IValidator
{
    private IValidator validator;

    public IValidator SetNext(IValidator next)
    {
        validator = next;
        return validator;
    }

    public Result Validate(VirtualBetbyRequest request, IEnumerable<ResourceItemModel> currentResources)
    {
        var htResult = request.Resources.Exists(r => r.Id == ResourceId.HalfTimeResourceId);
        var result = new Result();
        if (htResult)
        {
            result = ValidateHtConsistency(request, currentResources);
        }

        return result.IsSuccessful ? validator?.Validate(request, currentResources) : result;
    }

    private Result ValidateHtConsistency(VirtualBetbyRequest request, IEnumerable<ResourceItemModel> currentResources)
    {
        var currentHalftimeResources = currentResources.Where(i => i.Id is ResourceId.Quarter1 or ResourceId.Quarter2);
        var htResources = scoreService.GetMarkedList(request.Resources, currentHalftimeResources);
        request.Resources = htResources;

        if (!scoreService.CheckForHalftimeQuarters(htResources))
        {
            return new Result(1, "Cannot Calculate HT due to related quarters have not been calculated or selected to calculate.");
        }

        var halftimeScore = htResources.Find(r => r.Id == ResourceId.HalfTimeResourceId);
        var quarters = htResources.Where(i => i.Id is ResourceId.Quarter1 or ResourceId.Quarter2).ToList();
        if (scoreService.IsScoreEqualToQuarterScores(quarters, halftimeScore))
        {
            return new Result();
        }

        quarters.Add(halftimeScore);
        var message = scoreService.GetScoreMessage(quarters, string.Empty);
        return new Result(1, $"Inconsistent value {message}.");
    }
}