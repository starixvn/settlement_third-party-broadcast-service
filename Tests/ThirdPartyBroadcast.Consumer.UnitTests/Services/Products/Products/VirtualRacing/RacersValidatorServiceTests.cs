using Settlement.Business.Core.Models;
using ThirdPartyBroadcast.Consumer.Services.Products.VirtualRacing;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.Products.VirtualRacing;

public class RacersValidatorServiceTests
{
    private readonly RacersValidatorServiceImp service = new();

    private static IEnumerable<Outright> CreateOutright(params int[] OrIds)
    {
        return OrIds.Select(i => new Outright { Orid = i, Placing = 0 });
    }

    [Fact]
    public void ValidateBeforeSettle_IsRefund_ReturnTrue()
    {
        var request = new VirtualRacingRequest
        {
            Action = ActionName.Refund
        };

        var result = service.ValidateBeforeSettle(request, []);

        Assert.True(result.IsSuccessful);
    }

    [Theory]
    [InlineData(SportType.VirtualDogs, 7)]
    [InlineData(SportType.VirtualHorses, 9)]
    public void ValidateBeforeSettle_IsCalcAndWrongNumberRacers_ReturnRightErrorMessage(short sportType, byte racers)
    {
        var request = new VirtualRacingRequest
        {
            Action = ActionName.Calculate,
            SportType = sportType,
            Racers = racers
        };

        var result = service.ValidateBeforeSettle(request, []);
        var expected = $"Invalid number of Racers Setting({request.Racers}).";
        Assert.Equal(expected, result.ErrorMessage);
    }

    [Fact]
    public void ValidateBeforeSettle_IsCalcAndVirtualDogsAllPlacingZero_ReturnRightErrorMessage()
    {
        var request = new VirtualRacingRequest
        {
            Action = ActionName.Calculate,
            SportType = SportType.VirtualDogs,
            Racers = 6
        };

        var outright = CreateOutright(1, 2, 3, 4, 5, 6);

        var result = service.ValidateBeforeSettle(request, outright);
        const string expected = "Insufficient results.";
        Assert.Equal(expected, result.ErrorMessage);
    }

    [Fact]
    public void ValidateBeforeSettle_IsCalcAndVirtualDogs_ReturnRightErrorMessage()
    {
        var request = new VirtualRacingRequest
        {
            Action = ActionName.Calculate,
            SportType = SportType.VirtualDogs,
            Racers = 8
        };

        var outright = CreateOutright(1, 2, 3, 4, 5, 6).ToList();
        outright[0].Placing = 1;

        var result = service.ValidateBeforeSettle(request, outright);
        const string expected = "Inconsistent between Racers Setting(8) & racer results(6) received.";
        Assert.Equal(expected, result.ErrorMessage);
    }

    [Fact]
    public void ValidateBeforeSettle_IsCalcAndVirtualHorsesAllPlacingZero_ReturnRightErrorMessage()
    {
        var request = new VirtualRacingRequest
        {
            Action = ActionName.Calculate,
            SportType = SportType.VirtualHorses,
            Racers = 8
        };

        var outright = CreateOutright(1, 2, 3, 4, 5, 6, 7, 8);

        var result = service.ValidateBeforeSettle(request, outright);
        const string expected = "Insufficient results.";
        Assert.Equal(expected, result.ErrorMessage);
    }

    [Fact]
    public void ValidateBeforeSettle_IsCalcAndVirtualHorses_ReturnRightErrorMessage()
    {
        var request = new VirtualRacingRequest
        {
            Action = ActionName.Calculate,
            SportType = SportType.VirtualHorses,
            Racers = 10
        };

        var outright = CreateOutright(1, 2, 3, 4, 5, 6, 7, 8).ToList();

        outright[0].Placing = 1;

        var result = service.ValidateBeforeSettle(request, outright);
        const string expected = "Inconsistent between Racers Setting(10) & racer results(8) received.";
        Assert.Equal(expected, result.ErrorMessage);
    }

    [Fact]
    public void ValidateBeforeSettle_IsCalcAndPassedAllValidate_ReturnTrue()
    {
        var request = new VirtualRacingRequest
        {
            Action = ActionName.Calculate,
            SportType = SportType.VirtualHorses,
            Racers = 8
        };

        var outright = CreateOutright(1, 2, 3, 4, 5, 6, 7, 8).ToList();
        outright[0].Placing = 1;

        var result = service.ValidateBeforeSettle(request, outright);
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void PlacingResult_VirtualDogsAndRacersSix_ReturnRightPlacings()
    {
        var outright = CreateOutright(1, 2, 3, 4, 5, 6).ToList();
        outright[0].Placing = 1;
        outright[5].Placing = 1;

        var result = service.PlacingResult(SportType.VirtualDogs, 6, outright);
        var expected = new List<Outright>
        {
            new()
            {
                Orid = 1,
                Placing = 1,
                RacerId = 0
            },
            new()
            {
                Orid = 6,
                Placing = 1,
                RacerId = 0
            }
        };
        AssertExtension.AreEqual(expected, result);
    }

    [Fact]
    public void PlacingResult_VirtualHorses_ReturnRightPlacings()
    {
        var outright = CreateOutright(1, 2, 3, 4, 5, 6, 7, 8).ToList();
        outright[1].Placing = 1;
        outright[4].Placing = 1;
        outright[7].Placing = 1;

        var result = service.PlacingResult(SportType.VirtualHorses, 8, outright);
        var expected = new List<Outright>
        {
            new()
            {
                Orid = 2,
                Placing = 1,
                RacerId = 0
            },
            new()
            {
                Orid = 5,
                Placing = 1,
                RacerId = 0
            },
            new()
            {
                Orid = 8,
                Placing = 1,
                RacerId = 0
            }
        };
        AssertExtension.AreEqual(expected, result);
    }
}