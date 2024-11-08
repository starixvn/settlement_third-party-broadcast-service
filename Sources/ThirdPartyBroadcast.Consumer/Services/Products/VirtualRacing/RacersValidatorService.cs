using Settlement.Business.Core.Models;
using Result = ThirdPartyBroadcast.Consumer.Models.Result;

namespace ThirdPartyBroadcast.Consumer.Services.Products.VirtualRacing;

public interface RacersValidatorService
{
    Result ValidateBeforeSettle(VirtualRacingRequest request, IEnumerable<Outright> outright);

    IEnumerable<Outright> PlacingResult(int sportType, byte racers, IEnumerable<Outright> outright);
}

public class RacersValidatorServiceImp : RacersValidatorService
{
    public Result ValidateBeforeSettle(VirtualRacingRequest request, IEnumerable<Outright> outright)
    {
        if (string.Equals(request.Action, ActionName.Refund, StringComparison.OrdinalIgnoreCase))
        {
            return new Result();
        }

        if (!IsValidRacers(request.SportType, request.Racers))
        {
            return new Result
            {
                ErrorCode = 1,
                ErrorMessage = $"Invalid number of Racers Setting({request.Racers})."
            };
        }

        return ValidateRacers(request.Racers, outright.ToList());
    }

    private static Result ValidateRacers(byte racers, IReadOnlyCollection<Outright> outright)
    {
        var resultPlacing = outright.Where(x => x.Placing == 0);
        if (racers == resultPlacing.Count())
        {
            return new Result
            {
                ErrorCode = 2,
                ErrorMessage = "Insufficient results."
            };
        }

        var numberRacers = outright.Count;
        if (numberRacers != racers)
        {
            return new Result
            {
                ErrorCode = 3,
                ErrorMessage = $"Inconsistent between Racers Setting({racers}) & racer results({numberRacers}) received."
            };
        }

        return new Result();
    }

    private static bool IsValidRacers(int sportType, byte racers)
    {
        var dogRaces = sportType == SportType.VirtualDogs && racers is 6 or 8;
        var horsesRaces = sportType == SportType.VirtualHorses && racers is 8 or 10 or 12 or 14;

        return dogRaces || horsesRaces;
    }

    public IEnumerable<Outright> PlacingResult(int sportType, byte racers, IEnumerable<Outright> outright)
    {
        int[] validPlacings = sportType == SportType.VirtualDogs && racers == 6
            ? [1, 2]
            : [1, 2, 3];

        return outright.Where(x => validPlacings.Contains(x.Placing));
    }
}