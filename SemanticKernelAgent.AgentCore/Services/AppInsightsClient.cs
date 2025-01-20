using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using System;
using Azure;
using System.Drawing;
using SemanticKernelAgent.AgentTypes.AppInsights;
namespace SemanticKernelAgent.AgentCore.Services;

public class AppInsightsClient
{
    private readonly string _workspaceId;
    private readonly string _tenantId;
    private readonly string _clientId;
    private readonly string _clientSecret;



    public AppInsightsClient(IConfiguration configuration)
    {
        _workspaceId = configuration.GetValue<string>("WORKSPACE_ID");
        _tenantId = configuration.GetValue<string>("MicrosoftAppTenantId");
        _clientId = configuration.GetValue<string>("ClientAppId");
        _clientSecret=configuration.GetValue<string>("ClientSecret");
   

    }
    public async Task<string> CreateQuery(string tableName, int? TopNItems, bool HasSeverityLevel, int? SeverityLevel)
    {
        if(TopNItems==null && HasSeverityLevel==false){
           return $"{tableName} | project TimeGenerated, Message"; 
        } 
        else if(TopNItems!=null && HasSeverityLevel==false){
         return $"{tableName} | top {TopNItems} by TimeGenerated | project TimeGenerated, Message";
        }
        else if(TopNItems!=null && HasSeverityLevel==true){
           return $"{tableName} | top {TopNItems} by SeverityLevel | project TimeGenerated, Message"; 
        }
        else {
            return $"{tableName} | where SeverityLevel == {SeverityLevel} | project TimeGenerated, Message";
        }
    }

    public async Task<AppInsightsResult> ExecuteQuery(string query)
        {
        
                var options = new ClientSecretCredentialOptions
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
        };
    
        // https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
        var clientSecretCredential = new ClientSecretCredential(_tenantId, _clientId, _clientSecret,options);

        //_graphClient = new GraphServiceClient(clientSecretCredential, scopes);
        //ClientSecretCredential credential = new ClientSecretCredential(_tenantId,_clientId,_clientSecret);
        
        AppInsightsResult appInsightsResult = new AppInsightsResult();
        appInsightsResult.ResponseMessages = new List<string>();
        var client = new LogsQueryClient(clientSecretCredential);

        try{
        Response<LogsQueryResult>? response=await client.QueryWorkspaceAsync(
            workspaceId: _workspaceId,
            query: query,
            timeRange: new QueryTimeRange(TimeSpan.FromHours(1))
        );


        if(response!=null)
        {
            foreach(var table in response.Value.AllTables){
                if(table.Rows.Count>0){
                    
                foreach(var row in table.Rows){
                    
                    appInsightsResult.ResponseMessages.Add($"{row["TimeGenerated"]}: {row["Message"]}");
                }
                }
                else{

                    appInsightsResult.StatusCode=200;
                    appInsightsResult.ResultMessage="Queries successfully executed, no information found for this topic";
                    return appInsightsResult;
                }
            }
            appInsightsResult.StatusCode=200;
            appInsightsResult.ResultMessage="Queries successfully executed";
            return appInsightsResult;
        }
        else{

            appInsightsResult.StatusCode=400;
            appInsightsResult.ResultMessage="Queries successfully executed, no information found";
            return appInsightsResult;
        }
        }
        catch(Exception ex){

            appInsightsResult.StatusCode=500;
            appInsightsResult.ResultMessage=ex.Message;
            return appInsightsResult;
        }

        }

}
