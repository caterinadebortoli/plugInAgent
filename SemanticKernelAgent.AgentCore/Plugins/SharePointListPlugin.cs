using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Azure.AI.OpenAI;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Extensions.Configuration;
using System.Configuration;
using Azure;

using SemanticKernelAgent.AgentCore.Services;
using SemanticKernelAgent.AgentTypes.Conversation;

namespace SemanticKernelAgent.AgentCore.Plugins;
public class SharePointListPlugin
{

    private ITurnContext<IMessageActivity> _turnContext;

    private readonly GraphClient _graphClient;

    

    public SharePointListPlugin(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext, GraphClient graphClient)
    {

        _turnContext = turnContext;
        _graphClient = graphClient;

    }

    [KernelFunction, Description("Get Items from SharePoint list by given List name. Executes when User asks for items in Sharepoint, or gives Sharepoint / Power Platform relevant List names.")]
    public async Task<string> GenerateFinalResponse(
        [Description("list name")] string SharePointListName,
        [Description("Top N Items to be retrieves, defaults to Null")] int? TopNItems
    )
    {
        await _turnContext.SendActivityAsync($"Retrieving Sharepoint List items for {SharePointListName} and item count {TopNItems}");
        string response;
        try {
            var site = await _graphClient.GetSite();
            response = site.Name;
        }
        catch(Exception ex) {
            response = "Wait for Caro;";
        }
       

        
        if (!string.IsNullOrEmpty(response))
        {
            return response;
        }
        else
        {
            return "";
        }
    }


}