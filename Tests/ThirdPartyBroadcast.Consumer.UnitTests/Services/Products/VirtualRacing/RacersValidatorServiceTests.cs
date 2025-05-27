using Settlement.Business.Core.Models;
using ThirdPartyBroadcast.Consumer.Services.Products.VirtualRacing;
using SportTypes = Settlement.Business.Core.Constants.SportType;

namespace ThirdPartyBroadcast.Consumer.UnitTests.Services.Products.VirtualRacing;

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
    [InlineData(SportTypes.VirtualDogs, 7)]
    [InlineData(SportTypes.VirtualHorses, 9)]
    [InlineData(SportTypes.VirtualHarnessRacing, 9)]
    [InlineData(SportTypes.VirtualMotorRacing, 9)]
    [InlineData(SportTypes.VirtualSteepleChase, 9)]
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

    [Theory]
    [InlineData(SportTypes.VirtualDogs)]
    [InlineData(SportTypes.VirtualHarnessRacing)]
    [InlineData(SportTypes.VirtualMotorRacing)]
    [InlineData(SportTypes.VirtualSteepleChase)]
    public void ValidateBeforeSettle_IsCalcAndVirtualDogsAllPlacingZero_ReturnRightErrorMessage(short sportType)
    {
        var request = new VirtualRacingRequest
        {
            Action = ActionName.Calculate,
            SportType = sportType,
            Racers = 6
        };

        var outright = CreateOutright(1, 2, 3, 4, 5, 6);

        var result = service.ValidateBeforeSettle(request, outright);
        const string expected = "Insufficient results.";
        Assert.Equal(expected, result.ErrorMessage);
    }

    [Theory]
    [InlineData(SportTypes.VirtualDogs)]
    [InlineData(SportTypes.VirtualHarnessRacing)]
    [InlineData(SportTypes.VirtualMotorRacing)]
    [InlineData(SportTypes.VirtualSteepleChase)]
    public void ValidateBeforeSettle_IsCalcAndVirtualDogs_ReturnRightErrorMessage(short sportType)
    {
        var request = new VirtualRacingRequest
        {
            Action = ActionName.Calculate,
            SportType = sportType,
            Racers = 8
        };

        var outright = CreateOutright(1, 2, 3, 4, 5, 6).ToList();
        outright[0].Placing = 1;

        var result = service.ValidateBeforeSettle(request, outright);
        const string expected = "Inconsistent between Racers Setting(8) & racer results(6) received.";
        Assert.Equal(expected, result.ErrorMessage);
    }

    [Theory]
    [InlineData(SportTypes.VirtualHorses)]
    [InlineData(SportTypes.VirtualHarnessRacing)]
    [InlineData(SportTypes.VirtualMotorRacing)]
    [InlineData(SportTypes.VirtualSteepleChase)]
    public void ValidateBeforeSettle_IsCalcAndVirtualHorsesAllPlacingZero_ReturnRightErrorMessage(short sportType)
    {
        var request = new VirtualRacingRequest
        {
            Action = ActionName.Calculate,
            SportType = sportType,
            Racers = 8
        };

        var outright = CreateOutright(1, 2, 3, 4, 5, 6, 7, 8);

        var result = service.ValidateBeforeSettle(request, outright);
        const string expected = "Insufficient results.";
        Assert.Equal(expected, result.ErrorMessage);
    }

    [Theory]
    [InlineData(SportTypes.VirtualHorses)]
    [InlineData(SportTypes.VirtualHarnessRacing)]
    [InlineData(SportTypes.VirtualMotorRacing)]
    [InlineData(SportTypes.VirtualSteepleChase)]
    public void ValidateBeforeSettle_IsCalcAndVirtualHorses_ReturnRightErrorMessage(short sportType)
    {
        var request = new VirtualRacingRequest
        {
            Action = ActionName.Calculate,
            SportType = sportType,
            Racers = 10
        };

        var outright = CreateOutright(1, 2, 3, 4, 5, 6, 7, 8).ToList();

        outright[0].Placing = 1;

        var result = service.ValidateBeforeSettle(request, outright);
        const string expected = "Inconsistent between Racers Setting(10) & racer results(8) received.";
        Assert.Equal(expected, result.ErrorMessage);
    }

    [Theory]
    [InlineData(SportTypes.VirtualHorses)]
    [InlineData(SportTypes.VirtualHarnessRacing)]
    [InlineData(SportTypes.VirtualMotorRacing)]
    [InlineData(SportTypes.VirtualSteepleChase)]
    public void ValidateBeforeSettle_IsCalcAndPassedAllValidate_ReturnTrue(short sportType)
    {
        var request = new VirtualRacingRequest
        {
            Action = ActionName.Calculate,
            SportType = sportType,
            Racers = 8
        };

        var outright = CreateOutright(1, 2, 3, 4, 5, 6, 7, 8).ToList();
        outright[0].Placing = 1;

        var result = service.ValidateBeforeSettle(request, outright);
        Assert.True(result.IsSuccessful);
    }
}