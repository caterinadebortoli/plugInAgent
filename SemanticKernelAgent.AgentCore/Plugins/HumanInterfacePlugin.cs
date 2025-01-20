using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.Collections.Generic;

using SemanticKernelAgent.AgentCore.Services;
using SemanticKernelAgent.AgentTypes.Conversation;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.Graph.Models;


namespace SemanticKernelAgent.AgentCore.Plugins;
public class HumanInterfacePlugin
{
    private readonly Kernel _kernel;
    private ITurnContext<IMessageActivity> _turnContext;

    public HumanInterfacePlugin(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext, Kernel kernel)
    {
        _turnContext = turnContext;
        _kernel = kernel;
    }



    [KernelFunction, Description("Generate a human-readable final answer based on the results of a plan. Always run this as a final step of any plan to respond to the user.")]
    public async Task<string> GenerateFinalResponse(
        [Description("Plan results")] string planResults,
        [Description("User's goal")] string goal
    )
    {

        await _turnContext.SendActivityAsync($"Generating final answer...");
        var completionsOptions = new AzureOpenAIPromptExecutionSettings()
        {
            MaxTokens = 12000,
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddMessage(AuthorRole.System, @$"The information below was obtained by connecting to external systems. Please use it to formulate a response to the user.
                [PLAN RESULTS]:
                {planResults}");
        chatHistory.AddMessage(AuthorRole.User, goal);


        var completions = await _kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentAsync(chatHistory, completionsOptions);

        return completions.Content;
    }


}