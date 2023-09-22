using AutoMapper;
using Fanex.Caching;
using Fanex.Caching.Redis;
using Fanex.Data;
using Fanex.Data.Repository;
using Fanex.Data.SqlServer;
using Fanex.Logging;
using Fanex.Logging.Sentry.RavenClient;
using Fanex.Settlement.Converter.Services;
using Fanex.Settlement.ScaleOut;
using Fanex.Settlement.ScaleOut.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ONEbook.UM;
using ONEbook.UM.AspNetCore;
using ONEbook.UM.Configurations;
using ONEbook.UM.Helpers;
using ONEbook.UM.Transport;
using SharpRaven;
using ThirdPartyBroadcast.Consumer.Services;
using ThirdPartyBroadcast.Consumer.Services.Consumer;
using ThirdPartyBroadcast.Consumer.Services.Products;

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
        service.AddSingleton<BroadcastConsumerController, BroadcastConsumerControllerImp>();
        service.AddSingleton<ExchangesService, ExchangesServiceImp>();
        service.AddSingleton<FlagService, FlagServiceImp>();
        service.AddSingleton<GenerateConsumerService, GenerateConsumerServiceImp>();
        service.AddSingleton<BitCoinService, BitCoinServiceImp>();
        service.AddSingleton<GroupOneService, GroupOneServiceImp>();
        service.AddSingleton<OutrightService, OutrightServiceImp>();
        service.AddSingleton<HappyFiveService, HappyFiveServiceImp>();
        service.AddSingleton<NumberGameService, NumberGameServiceImp>();
        service.AddSingleton<BetBuilderService, BetBuilderServiceImp>();
        service.AddSingleton<FastMarketService, FastMarketServiceImp>();

        service.AddSingleton<IMaintenanceService, MaintenanceServiceImp>();
        service.AddSingleton(new UMClientConfiguration().Initialize(configuration));
        service.AddSingleton<IUMCacheService, UMCacheService>();
        service.AddSingleton<IUMRestClient, UMRestClient>();
        service.AddSingleton<ONEbook.UM.Services.IMaintenanceService, ONEbook.UM.Services.MaintenanceService>();
        service.AddConfigRedisCache(configuration);
        StartUmSetting(configuration);
        StartDataBaseSetting(configuration);
        StartLogSetting(configuration);
        service.ConfigureAutoMapper();
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

    private static void StartDataBaseSetting(IConfiguration configuration)
    {
        var connections = configuration.GetSection("ConnectionStrings")
            .GetChildren()
            .AsEnumerable()
            .ToDictionary(
                x => x.GetValue<string>("Name"),
                y => new ConnectionConfiguration(y.GetValue<string>("Name"), y.GetValue<string>("ConnectionString"), y.GetValue<string>("ProviderName"))
            );

        DbSettingProviderManager
            .StartNewSession()
            .Use(connections)
            .WithSqlServer()
            .UseDefaultDbSettingProvider(resourcePath: configuration.GetSection("StoredProcedurePath").Value)
            .Run();
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
    }
}