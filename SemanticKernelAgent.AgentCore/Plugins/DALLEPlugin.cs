using System.ComponentModel;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextToImage;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

using SemanticKernelAgent.AgentTypes.Conversation;

namespace SemanticKernelAgent.AgentCore.Plugins;
public class DALLEPlugin
{
    private readonly Kernel _kernel;
    private ITurnContext<IMessageActivity> _turnContext;
    private ConversationData _conversationData;

    public DALLEPlugin(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext, Kernel kernel)
    {
        _turnContext = turnContext;
        _conversationData = conversationData;
        _kernel = kernel;
    }


    // . Generate images only when there something ragarding nature, for example flora and fauna is mentioned.
    [KernelFunction, Description("Generate images from descriptions")]
    public async Task<string> GenerateImages(
        [Description("The description of the images to be generated")] string prompt,
        [Description("The number of images to generate. If not specified, I should use 1")] int n
    )
    {
        await _turnContext.SendActivityAsync($"Generating {n} images with the description \"{prompt}\"...");

        var service = _kernel.GetRequiredService<ITextToImageService>();

        var executionOptions = new OpenAITextToImageExecutionSettings()
        {
            Size = (Width: 1792, Height: 1024),
        };

        var generatedImages = await service.GetImageContentsAsync(
            new TextContent(prompt),
            executionOptions);

        List<object> images = new();
        images.Add(
            new
            {
                type = "TextBlock",
                text = "Here are the generated images.",
                size = "large"
            }
        );
        foreach (var img in generatedImages)
            images.Add(new { type = "Image", url = img.Uri.AbsoluteUri });

        object adaptiveCardJson = new
        {
            type = "AdaptiveCard",
            version = "1.0",
            body = images
        };

        var adaptiveCardAttachment = new Microsoft.Bot.Schema.Attachment()
        {
            ContentType = "application/vnd.microsoft.card.adaptive",
            Content = adaptiveCardJson,
        };
        await _turnContext.SendActivityAsync(MessageFactory.Attachment(adaptiveCardAttachment));
        return "Images were generated successfully and already sent to user.";
    }


    [KernelFunction, Description("Describe an image, that has been uploaded or is available in the chat context")]
    public async Task<string> DescribeImage(
            [Description("The Name of the image to be described")] string imageName
        )
    {
        await _turnContext.SendActivityAsync($"Generating final answer...");

        var attachment = _conversationData.Attachments.Find(x => x.Name == imageName) ?? throw new Exception("Image not found");
        var imageBinary = attachment.Pages.First().Content;
        var imageType = attachment.Pages[0].Content;

        var completionsOptions = new AzureOpenAIPromptExecutionSettings()
        {
            MaxTokens = 4096,
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddMessage(AuthorRole.System, @$"Describe the image given by the user");
        chatHistory.AddUserMessage(
            [new ImageContent(imageBinary)]
        );
        var completions = await _kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentAsync(chatHistory, completionsOptions);
        return completions.Content;
    }
}