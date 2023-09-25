using Fanex.Data.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThirdPartyBroadcast.Consumer.Models;
using ThirdPartyBroadcast.Consumer.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.InitializeServices(builder.Configuration);

builder.Services.AddFanexSql(builder.Configuration);

builder.Services.Configure<AppSettingsKey>(builder.Configuration);

builder.Services.AddHostedService<BroadcastConsumerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/",
        async context => { await context.Response.WriteAsync("ThirdPartyBroadcast.Consumer.Service"); });
});
#if DEBUG
app.Run();
#else
await app.RunAsync("http://0.0.0.0:5000");
#endif