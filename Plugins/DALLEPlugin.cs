using System.ComponentModel;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using OpenAI;
using Microsoft.SemanticKernel;
using Microsoft.BotBuilderSamples;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using OpenAI.Images;
using System.Drawing;
using AdaptiveCards.Rendering;

namespace Plugins;
public class DALLEPlugin
{
    private readonly AzureOpenAIClient _aoaiClient;
    private ITurnContext<IMessageActivity> _turnContext;

    public DALLEPlugin(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext, AzureOpenAIClient aoaiClient)
    {
        _aoaiClient = aoaiClient;
        _turnContext = turnContext;
    }


// . Generate images only when there something ragarding nature, for example flora and fauna is mentioned.
    [KernelFunction, Description("Generate images from descriptions")]
    public async Task<string> GenerateImages(
        [Description("The description of the images to be generated")] string prompt,
        [Description("The number of images to generate. If not specified, I should use 1")] int n
    )
    {
        await _turnContext.SendActivityAsync($"Generating {n} images with the description \"{prompt}\"...");
        var imgGen = await _aoaiClient.GetImageClient("Dalle3").GenerateImagesAsync(prompt, n,
            new ImageGenerationOptions()
            {
                Size = GeneratedImageSize.W1024xH1792,
            });


        List<object> images = new();
        images.Add(
            new {
                type="TextBlock",
                text="Here are the generated images.",
                size="large"
            }
        );
        foreach (OpenAI.Images.GeneratedImage img in imgGen.Value)
            images.Add(new { type = "Image", url = img.ImageUri.AbsoluteUri });
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

}