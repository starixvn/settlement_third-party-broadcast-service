namespace ThirdPartyBroadcast.Consumer;

using Criteria.BetBuilder;
using Criteria.BitCoin;
using Criteria.HappyFive;
using Criteria.NumberGame;
using Criteria.OutRight;
using Requests;

public class ThirdPartyBroadcastMappingProfile : Profile
{
    public ThirdPartyBroadcastMappingProfile()
    {
        CreateMap<NumberGameRequest, NumberGameCriteria>();
        //CreateMap<AutoFastMarketRequest, FastMarketCriteria>();
        CreateMap<HappyFiveRequest, HappyFiveCriteria>();
        //CreateMap<AutoPinGoalRequest, UpdateMatchInfoRequest>();
        //CreateMap<AutoPinGoalRequest, PinGoalCriteria>();
        CreateMap<BitCoinRequest, BitCoinUpdateMatchCriteria>();
        CreateMap<BitCoinRequest, BitCoinCriteria>();
        //CreateMap<FastMarketCriteria, EventTicketFastMarketRequest>();
        //CreateMap<AutoVirtualBetRadarRequest, BetRadarUpdateMatchCriteria>();
        //CreateMap<AutoVirtualBetRadarRequest, VirtualBetRadarCriteria>();
        //CreateMap<AutoVirtualSoccerRequest, VirtualSoccerCriteria>();
        //CreateMap<AutoVirtualSoccerRequest, VrSoccerUpdateMatchCriteria>();
        CreateMap<BetBuilderRequest, UpdateMarketCriteria>();
        CreateMap<BetBuilderRequest, BetBuilderCriteria>();
        CreateMap<OutrightRequest, OutrightCriteria>();
        //CreateMap<WriteLogCriteria, SendMailCriteria>().ForMember(dest => dest.AdminId, opt => opt.MapFrom(src => src.UserId));
        //CreateMap<AutoVirtualSoccerRequest, UpdateMatchInfo3RdRequest>().ForMember(dest => dest.Creator, opt => opt.MapFrom(src => src.AdminId));
        //CreateMap<AutoVirtualBetRadarRequest, BetRadarValidationCriteria>();
        //CreateMap<LeapGamingRequest, LeapGamingCriteria>();
        //CreateMap<LeapGamingRequest, UpdateMatchInfoRequest>();
        //CreateMap<LeapGamingRequest, UpdateMatchCriteria>();
    }
}