using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using SemanticKernelAgent.AgentCore.Services;

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

    [KernelFunction, Description("Generate App Insights' query")]
    public async Task<string> CreateQuery(
        [Description("The name of the table to be retrieved, which can be AppTraces or AppExceptions")] string tableName,
        [Description("Top N Items to be retrieved, defaults to Null")] int? TopNItems,
        [Description("Whether the severity level is requested")] bool HasSeverityLevel,
        [Description("The severity level requested, defaults to Null.")] int? SeverityLevel

    ){
        await _turnContext.SendActivityAsync("Generating query...");
        return await _client.CreateQuery(tableName,TopNItems,HasSeverityLevel,SeverityLevel);
    }

    [KernelFunction, Description("Execute App Insights' query. It can be directly executed if the user give the query in the prompt.")]
    public async Task<Object?> ExecuteAppInsightsQuery(
        [Description("The query to be executed")] string query
    )
        {
            await _turnContext.SendActivityAsync("Executing query...");
            return await _client.ExecuteQuery(query);
        }

}
