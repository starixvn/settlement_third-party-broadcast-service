using AutoMapper;
using Fanex.Caching;
using Fanex.Caching.Redis;
using Fanex.Data.Repository;
using Fanex.Logging;
using Fanex.Logging.Extensions;
using Fanex.Logging.Extensions.AspNetCore;
using Fanex.Logging.Extensions.Database;
using Fanex.Logging.Sentry.RavenClient;
using Fanex.MailDeliver.MailClient;
using Fanex.MailDelivery.Client;
using Fanex.RemotingRef;
using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.ScaleOut;
using Fanex.Settlement.ScaleOut.Services;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ONEbook.UM;
using ONEbook.UM.AspNetCore;
using ONEbook.UM.Configurations;
using ONEbook.UM.Helpers;
using ONEbook.UM.Transport;
using Settlement.Business.Services;
using SharpRaven;
using StackExchange.Redis;
using ThirdPartyBroadcast.Consumer.Services;
using ThirdPartyBroadcast.Consumer.Services.Consumer;
using ThirdPartyBroadcast.Consumer.Services.Products;
using ThirdPartyBroadcast.Consumer.Services.Products.BetBuilder;
using ThirdPartyBroadcast.Consumer.Services.Products.HappyFive;
using ThirdPartyBroadcast.Consumer.Services.Products.VirtualSportFive;

namespace ThirdPartyBroadcast.Consumer.Service;

public static class BroadcastConsumerInjection
{
    public static void InitializeServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        service.AddSingleton<XmlConvertService, XmlConvertServiceImp>();
        service.AddTransient<ScaleOutSettleThirdPartyService, ScaleOutSettleThirdPartyServiceImp>();
        service.AddTransient<IDynamicRepository, DynamicRepository>();
        service.AddTransient<CalculateService, CalculateServiceImp>();
        service.AddTransient<LogDirectlyService, LogDirectlyServiceImp>();
        service.AddTransient<GeneralService, GeneralServiceImp>();
        service.AddTransient<ScaleOutSettleService, ScaleOutSettleServiceImp>();
        service.AddSingleton<ICacheService, CacheService>();
        service.AddSingleton<GenerateConsumerService, GenerateConsumerServiceImp>();
        service.AddSingleton<BitCoinService, BitCoinServiceImp>();
        service.AddSingleton<GroupOneService, GroupOneServiceImp>();
        service.AddSingleton<GroupTwoService, GroupTwoServiceImp>();
        service.AddSingleton<OutrightService, OutrightServiceImp>();
        service.AddSingleton<HappyFiveService, HappyFiveServiceImp>();
        service.AddSingleton<NumberGameService, NumberGameServiceImp>();
        service.AddSingleton<BetBuilderService, BetBuilderServiceImp>();
        service.AddSingleton<BetBuilderGenerateService, BetBuilderGenerateServiceImp>();
        service.AddSingleton<FastMarketService, FastMarketServiceImp>();
        service.AddSingleton<VirtualBetRadarService, VirtualBetRadarServiceImp>();
        service.AddSingleton<VirtualSportsService, VirtualSportsServiceImp>();
        service.AddSingleton<LeapGamingService, LeapGamingServiceImp>();
        service.AddSingleton<Product3rdService, Product3rdServiceImp>();
        service.AddSingleton<SendMailService, SendMailServiceImp>();
        service.AddSingleton<PinGoalService, PinGoalServiceImp>();
        service.AddTransient<AnnouncementService, AnnouncementServiceImp>();
        service.AddTransient<MessageAnnouncementService, MessageAnnouncementServiceImp>();
        service.AddTransient<RecipientMessageService, RecipientMessageServiceImp>();
        service.AddTransient<TicketInfoService, TicketInfoServiceImp>();
        service.AddTransient<Services.MatchService, Services.MatchServiceImp>();
        service.AddSingleton<IMaintenanceService, MaintenanceServiceImp>();
        service.AddSingleton(new UMClientConfiguration().Initialize(configuration));
        service.AddSingleton<IUMCacheService, UMCacheService>();
        service.AddSingleton<IUMRestClient, UMRestClient>();
        service.AddSingleton<ONEbook.UM.Services.IMaintenanceService, ONEbook.UM.Services.MaintenanceService>();
        service.AddConfigRedisCache(configuration);
        StartUmSetting(configuration);
        StartLogSetting(configuration);
        service.ConfigureAutoMapper();
        service.ConfigureMassTransit(configuration);

        RefDataTypeContainer
            .ConnectTo(new Uri(configuration.GetSection("Fanex.RemotingRef.Service.Url").Value))
            .FromApplication("Settlement");
        service.AddSingleton<IRemotingRefService, RemotingRefService>();
        service.AddTransient<RefDataService, RefDataServiceImp>();

        AppSettings.AppMailId = configuration.GetSection("MailServer:Id").Value;
        AppSettings.AppMailAddress = configuration.GetSection("MailServer:Address").Value;
        MailClientHandler.StartAsync();
    }

    public static void InitializeBusinessServices(this IServiceCollection services)
    {
        services.AddSingleton<Settlement.Business.Services.HappyFive.BatchTicketsService, Settlement.Business.Services.HappyFive.BatchTicketsServiceImp>();
        services.AddSingleton<Settlement.Business.Services.HappyFive.CommandService, Settlement.Business.Services.HappyFive.CommandServiceImp>();
        services.AddSingleton<Settlement.Business.Services.HappyFive.ProcessService, Settlement.Business.Services.HappyFive.ProcessServiceImp>();
        services.AddSingleton<Settlement.Business.Services.MatchService, Settlement.Business.Services.MatchServiceImp>();
        services.AddSingleton<CustomerService, CustomerServiceImp>();
        services.AddSingleton<ModdsService, ModdsServiceImp>();
        services.AddSingleton<ModdsValueService, ModdsValueServiceImp>();
        services.AddSingleton<PrepareTicketService, PrepareTicketServiceImp>();
        services.AddSingleton<InsertHistoryData, InsertHistoryDataImp>();
        services.AddSingleton<TicketService, TicketServiceImp>();
        services.AddSingleton<WinLimitService, WinLimitServiceImp>();
        services.AddSingleton<WinLostService, WinLostServiceImp>();
        services.AddSingleton<WinLostByModdsService, WinLostByModdsServiceImp>();
        services.AddSingleton<CommissionService, CommissionServiceImp>();
        services.AddSingleton<ProcessService, ProcessServiceImp>();
        services.AddSingleton<SettleSitesService, SettleSitesServiceImp>();

        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.WriteIndented = true;
        });
    }

    private static void ConfigureAutoMapper(this IServiceCollection service)
    {
        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(typeof(ScaleOutMappingProfile));
            mc.AddProfile(typeof(ThirdPartyBroadcastMappingProfile));
        });

        var mapper = mapperConfig.CreateMapper();
        service.AddSingleton(mapper);
    }

    private static void AddConfigRedisCache(this IServiceCollection service, IConfiguration configuration)
    {
        var redisConfig = configuration.GetSection("AuthSettings:RedisConfig").Value;
        var applicationName = configuration.GetSection("AuthSettings:ChannelPrefix").Value;
        service.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect($"{redisConfig},ChannelPrefix={applicationName}"));
        service.AddSingleton<ICache>(new RedisCache(new RedisCacheOptions
        {
            ConnectionString = redisConfig,
            ApplicationName = applicationName,
        }));
        service.AddSingleton<ICacheService>(new CacheService());
    }

    private static void StartUmSetting(IConfiguration configuration)
    {
        var umConfig = new UMClientConfiguration().Initialize(configuration);

        UMClientManager
            .UseConfig(umConfig)
            .UseCacheService(new UMCacheService(new CacheService()))
            .UseRestClient(new UMRestClient(umConfig));
    }

    private static void StartLogSetting(IConfiguration configuration)
    {
        var logEngine = new List<ILogEngine>();

        var sentryEngineOptions = new SentryEngineOptions
        {
            Dsn = new Dsn(configuration.GetSection("SentryToken").Value)
        };
        logEngine.Add(new SentryLogging(sentryEngineOptions));

        LogManager.SetDefaultLogCategory(configuration.GetSection("SiteCategory").Value).Use(logEngine);

        ExceptionHandler
            .UseConfig(new LoggingConfiguration())
            .UseGetDbExceptionInfo(DatabaseInfoGetters.GetDbExceptionInfo);
    }

    private static void ConfigureMassTransit(this IServiceCollection service, IConfiguration configuration)
    {
        var host = configuration.GetSection("RabbitMQ:Hosting").Value;
        var virtualHost = configuration.GetSection("RabbitMQ:VirtualHost").Value;
        var username = configuration.GetSection("RabbitMQ:Username").Value;
        var password = configuration.GetSection("RabbitMQ:Password").Value;
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