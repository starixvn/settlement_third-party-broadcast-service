namespace ThirdPartyBroadcast.Consumer;

using Criteria;
using Criteria.BetBuilder;
using Criteria.BitCoin;
using Criteria.LeapGaming;
using Criteria.VirtualBetRadar;
using Criteria.VirtualSports;
using Requests;

public class ThirdPartyBroadcastMappingProfile : Profile
{
    public ThirdPartyBroadcastMappingProfile()
    {
        CreateMap<NumberGameRequest, NumberGameCriteria>();
        CreateMap<FastMarketRequest, FastMarketCriteria>();
        CreateMap<PinGoalRequest, UpdateMatchInfoRequest>();
        CreateMap<PinGoalRequest, PinGoalCriteria>();
        CreateMap<BitCoinRequest, BitCoinUpdateMatchCriteria>();
        CreateMap<BitCoinRequest, BitCoinCriteria>();
        CreateMap<FastMarketCriteria, EventTicketFastMarketRequest>();
        CreateMap<VirtualBetRadarRequest, BetRadarUpdateMatchCriteria>();
        CreateMap<VirtualBetRadarRequest, VirtualBetRadarCriteria>();
        CreateMap<VirtualSoccerRequest, VirtualSportsCriteria>();
        CreateMap<VirtualSoccerRequest, VirtualSportsUpdateMatchCriteria>();
        CreateMap<BetBuilderRequest, BetBuilderCriteria>();
        CreateMap<OutrightRequest, OutrightCriteria>();
        CreateMap<WriteLogCriteria, SendMailCriteria>().ForMember(dest => dest.AdminId, opt => opt.MapFrom(src => src.UserId));
        CreateMap<VirtualSoccerRequest, UpdateMatchInfo3RdRequest>().ForMember(dest => dest.Creator, opt => opt.MapFrom(src => src.AdminId));
        CreateMap<VirtualBetRadarRequest, BetRadarValidationCriteria>();
        CreateMap<LeapGamingRequest, LeapGamingCriteria>();
        CreateMap<LeapGamingRequest, UpdateMatchInfoRequest>();
        CreateMap<LeapGamingRequest, UpdateMatchCriteria>();
    }
}