using ThirdPartyBroadcast.Consumer.Extensions;

namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;

public class CorrectScoreValidator(ScoreService scoreService) : IValidator
{
    private IValidator validator;

    public IValidator SetNext(IValidator next)
    {
        validator = next;
        return validator;
    }

    public Result Validate(VirtualBetbyRequest request, IEnumerable<ResourceItemModel> currentResources)
    {
        var excludeOtResources = request.Resources.Where(r => r.Id != ResourceId.OverTime);
        foreach (var resource in excludeOtResources)
        {
            if (!resource.TryParseScore(out _))
            {
                return new Result
                {
                    ErrorCode = 1,
                    ErrorMessage = $"Invalid {scoreService.ResourceName(resource.Id)} score."
                };
            }
        }

        return validator?.Validate(request, currentResources) ?? new Result();
    }
}