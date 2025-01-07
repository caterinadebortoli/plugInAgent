using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

using SemanticKernelAgent.AgentCore.Services;
using SemanticKernelAgent.AgentTypes.Conversation;
using Microsoft.SemanticKernel.Embeddings;

namespace SemanticKernelAgent.AgentCore.Plugins;
public class UploadPlugin
{
    private readonly Kernel _kernel;
    private ConversationData _conversationData;
    private ITurnContext<IMessageActivity> _turnContext;

    public UploadPlugin(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext, Kernel kernel)
    {
        _conversationData = conversationData;
        _turnContext = turnContext;
        _kernel = kernel;
    }


    [KernelFunction, Description("Search for relevant information in the uploaded documents. Only use this when the user refers to documents they uploaded. Do not use or ask follow up questions about this function if the user did not specifically mention a document")]
    public async Task<string> SearchUploads(
        [Description("The exact name of the document to be searched.")] string docName,
        [Description("The text to search by similarity.")] string query
    )
    {
        await _turnContext.SendActivityAsync($"Searching document {docName} for \"{query}\"...");
        var embedding = await _kernel.GetRequiredService<ITextEmbeddingGenerationService>().GenerateEmbeddingsAsync(new List<string> { query });
        //var embedding = await _embeddingClient.GenerateEmbeddingsAsync(new List<string> { query });
        var vector = embedding.First().ToArray();
        var similarities = new List<float>();
        var attachment = _conversationData.Attachments.Find(x => x.Name == docName);
        foreach (AttachmentPage page in attachment.Pages)
        {
            float similarity = 0;
            for (int i = 0; i < page.Vector.Count(); i++)
            {
                similarity += page.Vector[i] * vector[i];
            }
            similarities.Add(similarity);
        }
        var maxIndex = similarities.IndexOf(similarities.Max());
        return _conversationData.Attachments.First().Pages[maxIndex].Content;
    }

}