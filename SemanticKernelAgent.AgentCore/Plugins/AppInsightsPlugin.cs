using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using SemanticKernelAgent.AgentCore.Services;
using SemanticKernelAgent.AgentTypes.AppInsights;
namespace SemanticKernelAgent.AgentCore.Plugins;

public class AppInsightsPlugin

{
    private ITurnContext<IMessageActivity> _turnContext;

    private readonly AppInsightsClient _client;

    public AppInsightsPlugin(ITurnContext<IMessageActivity> turnContext, AppInsightsClient client)
    {
        _turnContext=turnContext;
        _client=client;
    }

    [KernelFunction, Description("Generate App Insights' query; This function is called when Traces, Exceptions or other App Insijghts related informations is asked.")]
    public async Task<AppInsightsResult> CreateQuery(
        [Description("The name of the table to be retrieved, which can be traces or exceptions")] string tableName,
        [Description("Top N Items to be retrieved, defaults to Null")] int? TopNItems,
        [Description("Whether the severity level is requested")] bool HasSeverityLevel,
        [Description("The severity level requested, defaults to Null.")] int? SeverityLevel

    ){
 
        string q = await _client.CreateQuery(tableName,TopNItems,HasSeverityLevel,SeverityLevel);
        return await _client.ExecuteQuery(q);
    }


}
