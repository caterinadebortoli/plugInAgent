using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using System;
using Azure;

namespace SemanticKernelAgent.AgentCore.Services;

public class AppInsightsClient
{
    private readonly string _workspaceId;

    public AppInsightsClient(IConfiguration configuration)
    {
        _workspaceId = configuration.GetValue<string>("WORKSPACE_ID");

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

    public async Task<Object> ExecuteQuery(string query)
        {
        var credential= new AzureCliCredential();
        
        var client = new LogsQueryClient(credential);
        
        List<string> values = new List<string>();

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
                    
                    values.Add($"{row["TimeGenerated"]}: {row["Message"]}");
                }
                }
                else{
                    return "no information found";
                }
            }
            return values;
        }
        else{
            return "no response";
        }
        }
        catch(Exception ex){
            return ex.Message;
        }

        }

}
