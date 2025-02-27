using AutoMapper;
using Fanex.Caching;
using Fanex.Caching.Redis;
using Fanex.Data.Repository;
using Fanex.RemotingRef;
using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.ScaleOut;
using Fanex.Settlement.ScaleOut.Services;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ThirdPartyBroadcast.Consumer.Services;
using ThirdPartyBroadcast.Consumer.Services.Consumer;
using ThirdPartyBroadcast.Consumer.Services.Products;
using ThirdPartyBroadcast.Consumer.Services.Products.BetBuilder;
using ThirdPartyBroadcast.Consumer.Services.Products.HappyFive;
using ThirdPartyBroadcast.Consumer.Services.Products.VirtualBetby;
using ThirdPartyBroadcast.Consumer.Services.Products.VirtualRacing;
using ThirdPartyBroadcast.Consumer.Services.Products.VirtualSportFive;

namespace ThirdPartyBroadcast.Consumer.Service;

using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

public static class BroadcastConsumerInjection
{
    public static void AddInternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<XmlConvertService, XmlConvertServiceImp>();
        services.AddTransient<ScaleOutSettleThirdPartyService, ScaleOutSettleThirdPartyServiceImp>();
        services.AddTransient<IDynamicRepository, DynamicRepository>();
        services.AddTransient<CalculateService, CalculateServiceImp>();
        services.AddTransient<LogDirectlyService, LogDirectlyServiceImp>();
        services.AddTransient<GeneralService, GeneralServiceImp>();
        services.AddTransient<ScaleOutSettleService, ScaleOutSettleServiceImp>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddTransient<GenerateConsumerService, GenerateConsumerServiceImp>();
        services.AddTransient<BitCoinService, BitCoinServiceImp>();
        services.AddTransient<GroupOneService, GroupOneServiceImp>();
        services.AddTransient<GroupTwoService, GroupTwoServiceImp>();
        services.AddTransient<GroupThreeService, GroupThreeServiceImp>();
        services.AddTransient<VirtualRacingService, VirtualRacingServiceImp>();
        services.AddTransient<OutrightService, OutrightServiceImp>();
        services.AddTransient<NumberGameService, NumberGameServiceImp>();
        services.AddTransient<BetBuilderService, BetBuilderServiceImp>();
        services.AddTransient<BetBuilderGenerateService, BetBuilderGenerateServiceImp>();
        services.AddTransient<FastMarketService, FastMarketServiceImp>();
        services.AddTransient<VirtualBetRadarService, VirtualBetRadarServiceImp>();
        services.AddTransient<VirtualSportsService, VirtualSportsServiceImp>();
        services.AddTransient<LeapGamingService, LeapGamingServiceImp>();
        services.AddTransient<Product3rdService, Product3rdServiceImp>();
        services.AddTransient<SendMailService, SendMailServiceImp>();
        services.AddTransient<PinGoalService, PinGoalServiceImp>();
        services.AddTransient<VirtualBetbyService, VirtualBetbyServiceImp>();
        services.AddTransient<VirtualBetbyGenerateService, VirtualBetbyGenerateServiceImp>();
        services.AddTransient<GenerateMatchInfoService, GenerateMatchInfoServiceImp>();
        services.AddTransient<ScoreService, ScoreServiceImp>();
        services.AddTransient<ResultDetailService, ResultDetailServiceImp>();

        services.AddTransient<AnnouncementService, AnnouncementServiceImp>();
        services.AddTransient<MessageAnnouncementService, MessageAnnouncementServiceImp>();
        services.AddTransient<RecipientMessageService, RecipientMessageServiceImp>();
        services.AddTransient<TicketInfoService, TicketInfoServiceImp>();
        services.AddTransient<Services.MatchService, Services.MatchServiceImp>();

        var remotingRefUrl = configuration.GetValue("Fanex.RemotingRef.Service.Url", string.Empty);
        RefDataTypeContainer
            .ConnectTo(new Uri(remotingRefUrl))
            .FromApplication("Settlement");
        services.AddSingleton<IRemotingRefService, RemotingRefService>();
        services.AddTransient<RefDataService, RefDataServiceImp>();
    }

    public static void AddBusinessByProductsServices(this IServiceCollection services)
    {
        services.AddTransient<HappyFiveService, HappyFiveServiceImp>();
        services.AddTransient<Services.Products.HappyFive.ProcessService, Services.Products.HappyFive.ProcessServiceImp>();
        services.AddTransient<Services.Products.HappyFive.SettleSitesService, Services.Products.HappyFive.SettleSitesServiceImp>();

        services.AddTransient<VirtualRacingService, VirtualRacingServiceImp>();
        services.AddTransient<RacersValidatorService, RacersValidatorServiceImp>();
        services.AddTransient<Services.Products.VirtualRacing.ProcessService, Services.Products.VirtualRacing.ProcessServiceImp>();
        services.AddTransient<Services.Products.VirtualRacing.SettleSitesService, Services.Products.VirtualRacing.SettleSitesServiceImp>();
    }

    public static void AddAutoMapperProfile(this IServiceCollection service)
    {
        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(typeof(ScaleOutMappingProfile));
            mc.AddProfile(typeof(ThirdPartyBroadcastMappingProfile));
        });

        var mapper = mapperConfig.CreateMapper();
        service.AddSingleton(mapper);
    }

    public static void AddRedisCacheConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConfig = configuration.GetSection("Fanex.Caching.Redis:RedisConfig").Value;
        var applicationName = configuration.GetSection("Fanex.Caching.Redis:ChannelPrefix").Value;
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect($"{redisConfig},ChannelPrefix={applicationName}"));
        services.AddSingleton<ICache>(new RedisCache(new RedisCacheOptions
        {
            ConnectionString = redisConfig,
            ApplicationName = applicationName
        }));
    }

    public static void AddMassTransit(this IServiceCollection service, IConfiguration configuration)
    {
        var host = configuration.GetValue("RabbitMQ:Hosting", string.Empty);
        var virtualHost = configuration.GetValue("RabbitMQ:VirtualHost", string.Empty);
        var username = configuration.GetValue("RabbitMQ:Username", string.Empty);
        var password = configuration.GetValue("RabbitMQ:Password", string.Empty);
        service.AddMassTransit(busConfigurator =>
        {
            busConfigurator.AddConsumer<ProcessingConsumerService, MyConsumerDefinition>();
            busConfigurator.UsingRabbitMq((context, busFactoryConfigurator) =>
            {
                busFactoryConfigurator.Host(host, virtualHost, h =>
                {
                    h.Username(username);
                    h.Password(password);
                });
                busFactoryConfigurator.ConfigureEndpoints(context);
            });
        });
    }
}