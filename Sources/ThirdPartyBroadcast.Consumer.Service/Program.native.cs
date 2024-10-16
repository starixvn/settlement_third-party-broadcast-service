using Fanex.Data.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyBroadcast.Consumer.Models;
using ThirdPartyBroadcast.Consumer.Service;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFanexSql(builder.Configuration);

builder.Services.InitializeServices(builder.Configuration);
builder.Services.InitializeBusinessServices();

builder.Services.Configure<AppSettingsKey>(builder.Configuration);

var app = builder.Build();

app.Run(async context =>
{
    await context.Response.WriteAsync("OK");
});

await app.RunAsync();