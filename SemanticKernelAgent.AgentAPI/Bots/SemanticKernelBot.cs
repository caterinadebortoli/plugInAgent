// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.AI.OpenAI;
using Azure.Storage.Blobs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Handlebars;

using SemanticKernelAgent.AgentCore.Plugins;
using SemanticKernelAgent.AgentCore.Services;
using SemanticKernelAgent.AgentTypes.Conversation;



namespace Microsoft.BotBuilderSamples
{
    public class SemanticKernelBot<T> : DocumentUploadBot<T> where T : Dialog
    {
        private readonly Kernel _kernel;
        private readonly DocumentAnalysisClient _documentAnalysisClient;
        private readonly string _welcomeMessage;
        private readonly List<string> _suggestedQuestions;
        private readonly bool _useStepwisePlanner;
        private readonly string _searchSemanticConfig;

        private readonly GraphClient _graphClient;

        private readonly string _workspaceId;
       

        public SemanticKernelBot(
            IConfiguration config,
            ConversationState conversationState,
            UserState userState,
            Kernel kernel,
            T dialog,
            DocumentAnalysisClient documentAnalysisClient = null,
            BlobServiceClient blobServiceClient = null,
            GraphClient graphClient = null) :
            base(config, conversationState, userState, kernel, documentAnalysisClient, dialog)
        {
            _welcomeMessage = config.GetValue<string>("PROMPT_WELCOME_MESSAGE");
            _systemMessage = config.GetValue<string>("PROMPT_SYSTEM_MESSAGE");
            _suggestedQuestions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(config.GetValue<string>("PROMPT_SUGGESTED_QUESTIONS"));
            _useStepwisePlanner = config.GetValue<bool>("USE_STEPWISE_PLANNER");
            _searchSemanticConfig = config.GetValue<string>("SEARCH_SEMANTIC_CONFIG");
            _documentAnalysisClient = documentAnalysisClient;
            _graphClient = graphClient;
            _workspaceId=config.GetValue<string>("WORKSPACE_ID");
            _kernel = kernel;

        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(new Activity()
            {
                Type = "message",
                Text = _welcomeMessage,
                SuggestedActions = new SuggestedActions()
                {
                    Actions = _suggestedQuestions
                        .Select(value => new CardAction(type: "postBack", value: value))
                        .ToList()
                }
            });
        }

        public override async Task<string> ProcessMessage(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
        {

            await turnContext.SendActivityAsync(new Activity(type: "typing"));

            await HandleFileUploads(conversationData, turnContext);
            if (turnContext.Activity.Text.IsNullOrEmpty())
                return "";

            RegisterPlugins(conversationData, turnContext);
            if (_useStepwisePlanner)
            {
                var plannerOptions = new FunctionCallingStepwisePlannerOptions
                {
                    MaxTokens = 128000,
                };

                var planner = new FunctionCallingStepwisePlanner(plannerOptions);
                string prompt = FormatConversationHistory(conversationData);
                var result = await planner.ExecuteAsync(_kernel, prompt);

                return result.FinalAnswer;
            }
            else
            {
                var plannerOptions = new HandlebarsPlannerOptions
                {

                };

                var planner = new HandlebarsPlanner(plannerOptions);
                string prompt = FormatConversationHistory(conversationData);
                var plan = await planner.CreatePlanAsync(_kernel, prompt);
                var result = await plan.InvokeAsync(_kernel, default);
                return result;
            }
        }

        private void RegisterPlugins(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
        {
            if (_documentAnalysisClient != null && !_kernel.Plugins.Select(x => x.Name).Contains("UploadPlugin")) _kernel.ImportPluginFromObject(new UploadPlugin(conversationData, turnContext, _kernel), "UploadPlugin");
            if (!_kernel.Plugins.Select(x => x.Name).Contains("DALLEPlugin")) _kernel.ImportPluginFromObject(new DALLEPlugin(conversationData, turnContext, _kernel), "DALLEPlugin");
            if (!_kernel.Plugins.Select(x=>x.Name).Contains("AppInsightsPlugin")) _kernel.ImportPluginFromObject(new AppInsightsPlugin(conversationData,turnContext,_kernel, _workspaceId), "AppInsightsPlugin");
            if (!_kernel.Plugins.Select(x => x.Name).Contains("SharePointPlugin")) _kernel.ImportPluginFromObject(new SharePointListPlugin(conversationData, turnContext, _graphClient), "SharePointPlugin");
            if (!_useStepwisePlanner && !_kernel.Plugins.Select(x => x.Name).Contains("HumanInterfacePlugin")) _kernel.ImportPluginFromObject(new HumanInterfacePlugin(conversationData, turnContext, _kernel), "HumanInterfacePlugin");
        }
    }
}
