using Fanex.SiteDbConnectionManager.Client.Constants;
using Fanex.SiteDbConnectionManager.Client.Models;
using Fanex.SiteDbConnectionManager.Client.Services;
using Microsoft.Extensions.Configuration;
using Settlement.Business.Core.Requests;
using BusinessModels = Settlement.Business.Core.Models;

namespace ThirdPartyBroadcast.Consumer.Services.Products.HappyFive;

public interface SettleSiteHandler
{
    Task<BusinessModels.Result> SettleByServer(HappyFiveRequest request, List<BusinessModels.Resources> resources,
        SiteDb site);
}

public class SettleSiteHandlerImp(
    SiteDbClientService siteDbClientService,
    ProcessService processService,
    SiteDbPendingCommandService pendingCommandService,
    IConfiguration configuration) : SettleSiteHandler
{
    public async Task<BusinessModels.Result> SettleByServer(HappyFiveRequest request, List<BusinessModels.Resources> resources, SiteDb site)
    {
        var isActive = await siteDbClientService.IsSiteDbActiveAsync(site.SiteDbGroupId);
        var pendingCommand = await pendingCommandService.CheckingPendingCommand(request.MatchId, site.SiteDbGroupId);
        if (isActive && pendingCommand.ErrorCode == 0)
        {
            return await ProcessActiveSite(request, site, resources);
        }
        return await ProcessInactiveSite(request, resources, site);
    }

    private async Task<BusinessModels.Result> ProcessActiveSite(HappyFiveRequest request, SiteDb site, List<BusinessModels.Resources> resources)
    {
        return await processService.Settle(request, site.SiteName, resources);
    }

    private async Task<BusinessModels.Result> ProcessInactiveSite(HappyFiveRequest request, List<BusinessModels.Resources> resources, SiteDb site)
    {
        var command = CreateSiteDbPendingCommand(request, resources, site);
        var result = await pendingCommandService.SavePendingCommand(command);
        return new BusinessModels.Result
        {
            ErrCode = result.ErrorCode,
            ErrMessage = result.ErrorMessage
        };
    }

    private SiteDbPendingCommand CreateSiteDbPendingCommand(HappyFiveRequest request, List<BusinessModels.Resources> resources, SiteDb site)
    {
        var parameter = CreateBatchTicketRequest(request, site.SiteName, resources);
        return new SiteDbPendingCommand
        {
            MatchId = request.MatchId,
            ProductType = ProductType.HappyFive,
            Action = request.Action,
            SpName = "NAP_Admin_Calculate_OtherSports",
            Parameter = parameter.ToJson(),
            AdminId = request.AdminId,
            IP = request.Ip,
            SystemName = SystemName.Broadcast,
            StatusId = CommandStatus.Pending,
            SiteDBGroupId = site.SiteDbGroupId,
        };
    }

    private BatchTicketRequest CreateBatchTicketRequest(HappyFiveRequest request, string site, List<BusinessModels.Resources> resources)
    {
        return new BatchTicketRequest
        {
            MatchId = request.MatchId,
            SportType = request.SportType,
            Action = request.Action,
            ActionId = request.ActionId,
            Resources = resources,
            DbServer = site,
            LastCustId = 0,
            LastTransId = 0,
            BatchSize = Convert.ToInt32(configuration.GetSection("MaxBatchSize").Value),
            BatchTotal = Convert.ToInt32(configuration.GetSection("MaxWorker").Value)
        };
    }
}