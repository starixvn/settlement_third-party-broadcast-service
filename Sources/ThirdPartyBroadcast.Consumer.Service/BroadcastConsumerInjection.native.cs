using AutoMapper;
using Fanex.Caching;
using Fanex.Caching.Redis;
using Fanex.Data.Repository;
using Fanex.MailDeliver.MailClient;
using Fanex.MailDelivery.Client;
using Fanex.RemotingRef;
using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.ScaleOut;
using Fanex.Settlement.ScaleOut.Services;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Settlement.Business.Services;
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
    public static void InitializeServices(this IServiceCollection services, IConfiguration configuration)
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
        services.AddSingleton<GenerateConsumerService, GenerateConsumerServiceImp>();
        services.AddSingleton<BitCoinService, BitCoinServiceImp>();
        services.AddSingleton<GroupOneService, GroupOneServiceImp>();
        services.AddSingleton<GroupTwoService, GroupTwoServiceImp>();
        services.AddSingleton<GroupThreeService, GroupThreeServiceImp>();
        services.AddSingleton<VirtualRacingService, VirtualRacingServiceImp>();
        services.AddSingleton<OutrightService, OutrightServiceImp>();
        services.AddSingleton<NumberGameService, NumberGameServiceImp>();
        services.AddSingleton<BetBuilderService, BetBuilderServiceImp>();
        services.AddSingleton<BetBuilderGenerateService, BetBuilderGenerateServiceImp>();
        services.AddSingleton<FastMarketService, FastMarketServiceImp>();
        services.AddSingleton<VirtualBetRadarService, VirtualBetRadarServiceImp>();
        services.AddSingleton<VirtualSportsService, VirtualSportsServiceImp>();
        services.AddSingleton<LeapGamingService, LeapGamingServiceImp>();
        services.AddSingleton<Product3rdService, Product3rdServiceImp>();
        services.AddSingleton<SendMailService, SendMailServiceImp>();
        services.AddSingleton<PinGoalService, PinGoalServiceImp>();
        services.AddSingleton<VirtualBetbyService, VirtualBetbyServiceImp>();
        services.AddSingleton<VirtualBetbyGenerateService, VirtualBetbyGenerateServiceImp>();
        services.AddSingleton<GenerateMatchInfoService, GenerateMatchInfoServiceImp>();
        services.AddSingleton<ScoreService, ScoreServiceImp>();
        services.AddSingleton<ResultDetailService, ResultDetailServiceImp>();

        services.AddTransient<AnnouncementService, AnnouncementServiceImp>();
        services.AddTransient<MessageAnnouncementService, MessageAnnouncementServiceImp>();
        services.AddTransient<RecipientMessageService, RecipientMessageServiceImp>();
        services.AddTransient<TicketInfoService, TicketInfoServiceImp>();
        services.AddTransient<Services.MatchService, Services.MatchServiceImp>();

        var remotingRefUrl = configuration.GetValue("Fanex.RemotingRef.Service.Url", string.Empty) ?? string.Empty;
        RefDataTypeContainer
            .ConnectTo(new Uri(remotingRefUrl))
            .FromApplication("Settlement");
        services.AddSingleton<IRemotingRefService, RemotingRefService>();
        services.AddTransient<RefDataService, RefDataServiceImp>();

        AppSettings.AppMailId = configuration.GetSection("MailServer:Id").Value;
        AppSettings.AppMailAddress = configuration.GetSection("MailServer:Address").Value;
        MailClientHandler.StartAsync();
    }

    private static void InitializeBusinessByProductsServices(this IServiceCollection services)
    {
        services.AddSingleton<HappyFiveService, HappyFiveServiceImp>();
        services.AddSingleton<Services.Products.HappyFive.ProcessService, Services.Products.HappyFive.ProcessServiceImp>();
        services.AddSingleton<Services.Products.HappyFive.SettleSitesService, Services.Products.HappyFive.SettleSitesServiceImp>();

        services.AddSingleton<VirtualRacingService, VirtualRacingServiceImp>();
        services.AddSingleton<RacersValidatorService, RacersValidatorServiceImp>();
        services.AddSingleton<Services.Products.VirtualRacing.ProcessService, Services.Products.VirtualRacing.ProcessServiceImp>();
        services.AddSingleton<Services.Products.VirtualRacing.SettleSitesService, Services.Products.VirtualRacing.SettleSitesServiceImp>();
    }

    public static void InitializeBusinessServices(this IServiceCollection services)
    {
        services.AddSingleton<Settlement.Business.Services.Products.HappyFiveProcessService, Settlement.Business.Services.Products.HappyFiveProcessServiceImp>();
        services.AddSingleton<Settlement.Business.Services.Products.RacingProcessService, Settlement.Business.Services.Products.RacingProcessServiceImp>();
        services.AddSingleton<Settlement.Business.Services.MatchService, Settlement.Business.Services.MatchServiceImp>();

        services.AddSingleton<SettleService, SettleServiceImp>();
        services.AddSingleton<TicketValueService, TicketValueServiceImp>();
        services.AddSingleton<GenerateSettleTicketService, GenerateSettleTicketServiceImp>();

        services.AddSingleton<CustomerService, CustomerServiceImp>();
        services.AddSingleton<ModdsService, ModdsServiceImp>();
        services.AddSingleton<ModdsValueService, ModdsValueServiceImp>();
        services.AddSingleton<PrepareTicketService, PrepareTicketServiceImp>();
        services.AddSingleton<HistoryData, HistoryDataImp>();
        services.AddSingleton<TicketService, TicketServiceImp>();
        services.AddSingleton<WinLimitService, WinLimitServiceImp>();
        services.AddSingleton<WinLostService, WinLostServiceImp>();
        services.AddSingleton<WinLostByModdsService, WinLostByModdsServiceImp>();
        services.AddSingleton<CommissionService, CommissionServiceImp>();

        services.InitializeBusinessByProductsServices();

        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.WriteIndented = true;
        });
    }

    public static void ConfigureAutoMapper(this IServiceCollection service)
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

    public static void ConfigureMassTransit(this IServiceCollection service, IConfiguration configuration)
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
                    h.Username(username ?? string.Empty);
                    h.Password(password ?? string.Empty);
                });
                busFactoryConfigurator.ConfigureEndpoints(context);
            });
        });
    }
}