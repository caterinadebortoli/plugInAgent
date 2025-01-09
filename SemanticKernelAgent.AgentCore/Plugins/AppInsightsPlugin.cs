using System.ComponentModel;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Services;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using SemanticKernelAgent.AgentTypes.Conversation;
using Microsoft.Bot.Configuration;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using System;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Microsoft.Graph.Models;

namespace SemanticKernelAgent.AgentCore.Plugins;

public class AppInsightsPlugin

{
    private readonly Kernel _kernel;
    private ITurnContext<IMessageActivity> _turnContext;
    private ConversationData _conversationData;

    private readonly string _workspaceId;

    public AppInsightsPlugin(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext, Kernel kernel, string workspaceId)
    {
        _conversationData=conversationData;
        _turnContext=turnContext;
        _kernel=kernel;
        _workspaceId=workspaceId;
    }

    [KernelFunction, Description("Generate App Insights' query")]
    public async Task<string> CreateQuery(
        [Description("The name of the table to be retrieved, which can be AppTraces or AppExceptions")] string tableName,
        [Description("Top N Items to be retrieved, defaults to Null")] int? TopNItems,
        [Description("Whether the severity level is requested")] bool HasSeverityLevel,
        [Description("The severity level requested, defaults to Null.")] int? SeverityLevel

    ){
        await _turnContext.SendActivityAsync("Generating query...");
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

    [KernelFunction, Description("Execute App Insights' query")]
    public async Task<Object?> ExecuteAppInsightsQuery(
        [Description("The query to be executed")] string query
    )
        {
        var credential= new AzureCliCredential();
        
        var client = new LogsQueryClient(credential);
        

        List<string> values = new List<string>();

        try{
        
        await _turnContext.SendActivityAsync($"Executing query...");
        Response<LogsQueryResult>? response=await client.QueryWorkspaceAsync(
            workspaceId: _workspaceId,
            query: query,
            timeRange: new QueryTimeRange(TimeSpan.FromHours(1))
        );

        if(response!=null){
        foreach(var table in response.Value.AllTables){
            foreach(var row in table.Rows){
                values.Add($"{row["TimeGenerated"]}: {row["Message"]}");
            }
        }
        return values;
        }
        else{
            await _turnContext.SendActivityAsync($"No Traces");
            return "no response";
        }
        }
        catch(Exception ex){
            await _turnContext.SendActivityAsync($"Exception: {ex}");
            return "Exception";
        }

        }

}
