using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using SemanticKernelAgent.AgentCore.Services;

namespace SemanticKernelAgent.AgentCore.Plugins;

public class Neo4jGraphPlugIn

{
    private ITurnContext<IMessageActivity> _turnContext;

    private readonly Neo4jGraphClient _client;

    public Neo4jGraphPlugIn(ITurnContext<IMessageActivity> turnContext, Neo4jGraphClient client)
    {
        _turnContext=turnContext;
        _client=client;
    }

    [KernelFunction, Description("Generate Neo4j's query to retrieve risks' nodes")]
    public async Task<string> CreateQuery(
        [Description("The name of the Node to be retrieved, which can be FINANCIAL_RISK or Chunk. Defaults to Null")] string? NodeType,
        [Description("Top N Items to be retrieved, defaults to Null")] int? TopNItems
    ){
        await _turnContext.SendActivityAsync("Generating query...");
        return await _client.CreateQuery(NodeType,TopNItems);
    }

    [KernelFunction, Description("Execute Neo4j's query. It can be directly executed if the user give the query in the prompt.")]
    public async Task<Object?> ExecuteAppInsightsQuery(
        [Description("The query to be executed")] string query
    )
        {
            await _turnContext.SendActivityAsync("Executing query...");
            return await _client.ExecuteQuery(query);
        }

}
