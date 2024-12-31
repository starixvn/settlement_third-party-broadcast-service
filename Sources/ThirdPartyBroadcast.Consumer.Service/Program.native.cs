using Fanex.Data.SqlServer;
using Fanex.Logging.Sentry.RavenClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ONEbook.UM.DependencyInjection;
using ThirdPartyBroadcast.Consumer.Models;
using ThirdPartyBroadcast.Consumer.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOneBookUm(builder.Configuration);

builder.Services.AddFanexSql(builder.Configuration);

builder.Services.AddFanexLogSentry(builder.Configuration);

builder.Services.AddRedisCacheConfig(builder.Configuration);

builder.Services.ConfigureAutoMapper();

builder.Services.ConfigureMassTransit(builder.Configuration);

builder.Services.InitializeServices(builder.Configuration);

builder.Services.InitializeBusinessServices();

builder.Services.Configure<AppSettingsKey>(builder.Configuration);

var app = builder.Build();

app.Run(async context =>
{
    await context.Response.WriteAsync("OK");
});

await app.RunAsync();