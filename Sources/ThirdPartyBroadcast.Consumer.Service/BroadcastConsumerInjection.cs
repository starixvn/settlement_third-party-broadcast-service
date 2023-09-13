using Fanex.Caching;
using Fanex.Caching.Redis;
using Fanex.Data;
using Fanex.Data.SqlServer;
using Fanex.Logging;
using Fanex.Logging.Sentry.RavenClient;
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

namespace ThirdPartyBroadcast.Consumer.Service;

public static class BroadcastConsumerInjection
{
    public static void InitializeServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        service.AddSingleton<ICacheService, CacheService>();
        service.AddSingleton<BroadcastConsumerController, BroadcastConsumerControllerImp>();
        service.AddSingleton<ExchangesService, ExchangesServiceImp>();
        service.AddSingleton<FlagService, FlagServiceImp>();
        service.AddSingleton<IMaintenanceService, MaintenanceServiceImp>();

        service.AddSingleton(new UMClientConfiguration().Initialize(configuration));
        service.AddSingleton<IUMCacheService, UMCacheService>();
        service.AddSingleton<IUMRestClient, UMRestClient>();
        service.AddSingleton<ONEbook.UM.Services.IMaintenanceService, ONEbook.UM.Services.MaintenanceService>();
        service.AddConfigRedisCache(configuration);
        service.StartUmSetting(configuration);
        service.StartDataBaseSetting(configuration);
        service.StartLogSetting(configuration);
    }

    private static IServiceCollection AddConfigRedisCache(this IServiceCollection service, IConfiguration configuration)
    {
        var redisConfig = configuration.GetSection("AuthSettings:RedisConfig").Value;
        var applicationName = configuration.GetSection("AuthSettings:ChannelPrefix").Value;
        service.AddSingleton<ICache>(new RedisCache(new RedisCacheOptions
        {
            ConnectionString = redisConfig,
            ApplicationName = applicationName,
        }));
        service.AddSingleton<ICacheService>(new CacheService());

        return service;
    }

    private static IServiceCollection StartUmSetting(this IServiceCollection service, IConfiguration configuration)
    {
        var umConfig = new UMClientConfiguration().Initialize(configuration);

        UMClientManager
            .UseConfig(umConfig)
            .UseCacheService(new UMCacheService(new CacheService()))
            .UseRestClient(new UMRestClient(umConfig));

        return service;
    }

    private static IServiceCollection StartDataBaseSetting(this IServiceCollection service, IConfiguration configuration)
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

        return service;
    }

    private static IServiceCollection StartLogSetting(this IServiceCollection service, IConfiguration configuration)
    {
        var logEngine = new List<ILogEngine>();

        var sentryEngineOptions = new SentryEngineOptions
        {
            Dsn = new Dsn(configuration.GetSection("SentryToken").Value)
        };
        logEngine.Add(new SentryLogging(sentryEngineOptions));

        LogManager.SetDefaultLogCategory(configuration.GetSection("SiteCategory").Value).Use(logEngine);

        return service;
    }
}