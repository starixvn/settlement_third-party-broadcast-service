using Fanex.Data.SqlServer;
using Fanex.Logging.Sentry.RavenClient;
using Fanex.SimpleMailDelivery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ONEbook.UM.DependencyInjection;
using ThirdPartyBroadcast.Consumer.Models;
using ThirdPartyBroadcast.Consumer.Service;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddOneBookUmForService(configuration);
services.AddFanexSql(configuration);
services.AddFanexLogSentry(configuration);
services.AddRedisCacheConfig(configuration);
services.AddMailDelivery(configuration);
services.AddAutoMapperProfile();
services.AddMassTransit(configuration);
services.AddInternalServices(configuration);
services.AddBusinessServices();
services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.WriteIndented = true;
});

services.Configure<AppSettingsKey>(configuration);

var app = builder.Build();

app.Run(async context =>
{
    await context.Response.WriteAsync("OK");
});

await app.RunAsync();