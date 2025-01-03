using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.BotBuilderSamples;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.Collections.Generic;

namespace Plugins;
public class HumanInterfacePlugin
{
    private readonly AzureOpenAIClient _aoaiClient;
    private ITurnContext<IMessageActivity> _turnContext;

    public HumanInterfacePlugin(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext, AzureOpenAIClient aoaiClient)
    {
        _aoaiClient = aoaiClient;
        _turnContext = turnContext;
    }



    [KernelFunction, Description("Generate a human-readable final answer based on the results of a plan. Always run this as a final step of any plan to respond to the user.")]
    public async Task<string> GenerateFinalResponse(
        [Description("Plan results")] string planResults,
        [Description("User's goal")] string goal
    )
    {

        await _turnContext.SendActivityAsync($"Generating final answer...");
        var completionsOptions = new ChatCompletionOptions()
        {
            MaxOutputTokenCount = 12000,
        };

        List<ChatMessage> messages = new List<ChatMessage>();
        messages.Add(new SystemChatMessage(@$"The information below was obtained by connecting to external systems. Please use it to formulate a response to the user.
                [PLAN RESULTS]:
                {planResults}"));
        messages.Add(new UserChatMessage(goal));


        var completions = await _aoaiClient.GetChatClient("gpt-4").CompleteChatAsync(messages, completionsOptions);
        return completions.Value.Content.ToString();
    }


}