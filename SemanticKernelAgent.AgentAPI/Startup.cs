// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using SemanticKernelAgent.AgentCore.Plugins;
using SemanticKernelAgent.AgentCore.Services;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Models;
using Services;

namespace Microsoft.BotBuilderSamples
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
            services.AddSingleton(configuration);
            var aiOptions= new ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions();
            
            services.AddApplicationInsightsTelemetry(aiOptions);

            DefaultAzureCredential azureCredentials;
            if (configuration.GetValue<string>("MicrosoftAppType") == "UserAssignedMSI")
                azureCredentials = new DefaultAzureCredential();
            else
                azureCredentials = new DefaultAzureCredential();
            services.AddHttpClient().AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.MaxDepth = HttpHelper.BotMessageSerializerSettings.MaxDepth;
            });

            // Create the Bot Framework Authentication to be used with the Bot Adapter.
            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the Bot Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            IStorage storage;
            if (configuration.GetValue<string>("COSMOS_API_ENDPOINT") != null)
            {
                var cosmosDbStorageOptions = new CosmosDbPartitionedStorageOptions()
                {
                    CosmosDbEndpoint = configuration.GetValue<string>("COSMOS_API_ENDPOINT"),
                    TokenCredential = azureCredentials,
                    DatabaseId = "SemanticKernelBot",
                    ContainerId = "Conversations"
                };
                storage = new CosmosDbPartitionedStorage(cosmosDbStorageOptions);
            }
            else
            {
                storage = new MemoryStorage();
            }


          

            // Add Graph Client Service Singleton
            services.AddSingleton<DirectLineService>();
            services.AddSingleton<GraphClient>();

            // Add App Insights Client Service Singleton
            services.AddSingleton<AppInsightsClient>();

            // Create the User state passing in the storage layer.
            var userState = new UserState(storage);
            services.AddSingleton(userState);

            // Create the Conversation state passing in the storage layer.
            var conversationState = new ConversationState(storage);
            services.AddSingleton(conversationState);

            if (!configuration.GetValue<string>("DOCINTEL_API_ENDPOINT").IsNullOrEmpty())
                services.AddSingleton(new DocumentAnalysisClient(new Uri(configuration.GetValue<string>("DOCINTEL_API_ENDPOINT")), new AzureKeyCredential(configuration.GetValue<string>("DOCINTEL_API_KEY"))));
            if (!configuration.GetValue<string>("BLOB_API_ENDPOINT").IsNullOrEmpty())
                if (!configuration.GetValue<string>("BLOB_API_KEY").IsNullOrEmpty())
                    services.AddSingleton(new BlobServiceClient(new Uri(configuration.GetValue<string>("BLOB_API_ENDPOINT")), new StorageSharedKeyCredential(configuration.GetValue<string>("BLOB_API_ENDPOINT").Split('/')[2].Split('.')[0], configuration.GetValue<string>("BLOB_API_KEY"))));
                else
                    services.AddSingleton(new BlobServiceClient(new Uri(configuration.GetValue<string>("BLOB_API_ENDPOINT")), azureCredentials));

            services.AddHttpClient();
            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            // services.AddSingleton<LoginDialog>();
            services.AddSingleton<LoginDialog>();
            services.AddTransient<IBot, SemanticKernelBot<LoginDialog>>();
            services.AddSwaggerGen();
            services.AddSingleton<Kernel>(services => {
                var kernel = Kernel.CreateBuilder()
                    .AddAzureOpenAIChatCompletion(
                        deploymentName: configuration.GetValue<string>("AOAI_GPT_MODEL"),
                        endpoint: configuration.GetValue<string>("AOAI_API_ENDPOINT"),
                        apiKey: configuration.GetValue<string>("AOAI_API_KEY")
                    )
                    //model name can be rmoved after appsettings is distributed
                    .AddAzureOpenAITextToImage(
                        configuration.GetValue<string>("AOAI_IMAGE_MODEL") ?? "Dalle3", 
                        configuration.GetValue<string>("AOAI_API_ENDPOINT"), 
                        configuration.GetValue<string>("AOAI_API_KEY")
                    )
                    .AddAzureOpenAITextEmbeddingGeneration(
                        configuration.GetValue<string>("AOAI_EMBEDDINGS_MODEL"), 
                        configuration.GetValue<string>("AOAI_API_ENDPOINT"), 
                        configuration.GetValue<string>("AOAI_API_KEY")
                    )
                    .Build();
                return kernel;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(options=>{
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                options.RoutePrefix = string.Empty;
            });
            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}
