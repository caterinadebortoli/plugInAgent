// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using SemanticKernelAgent.AgentTypes.Conversation;
using SemanticKernelAgent.AgentTypes.Enums;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;


namespace Microsoft.BotBuilderSamples
{
    public class DocumentUploadBot<T> : StateManagementBot<T> where T : Dialog
    {
        private readonly DocumentAnalysisClient _documentAnalysisClient;
        private readonly Kernel _kernel;

        public DocumentUploadBot(IConfiguration config, ConversationState conversationState, UserState userState, Kernel kernel, DocumentAnalysisClient documentAnalysisClient, T dialog) : base(config, conversationState, userState, dialog)
        {
            _documentAnalysisClient = documentAnalysisClient;
            _kernel = kernel;
        }

        public async Task HandleFileUploads(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
        {
            if (turnContext.Activity.Attachments.IsNullOrEmpty())
                return;

            // process PDFs for embedding
            await HandlePDFAttachment(conversationData, turnContext);
            // process Images for information extraction
            await HandleImageAttachment(conversationData, turnContext);


        }

        private async Task HandlePDFAttachment(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
        {
            // process PDFs for embedding
            var pdfAttachments = turnContext.Activity.Attachments.Where(x => x.ContentType == "application/pdf");
            if (pdfAttachments.IsNullOrEmpty())
                return;
            if (_documentAnalysisClient == null)
            {
                await turnContext.SendActivityAsync("Document upload not supported as no Document Intelligence endpoint was provided");
                return;
            }
            foreach (Bot.Schema.Attachment pdfAttachment in pdfAttachments)
            {
                await IngestAttachment(conversationData, turnContext, AttachmentType.PDF, pdfAttachment);
            }
        }

        private async Task HandleImageAttachment(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
        {
            // process images for Description exctraction
            var imageAttachments = turnContext.Activity.Attachments.Where(x => x.ContentType.StartsWith("image/"));
            if (imageAttachments.IsNullOrEmpty())
                return;
            foreach (Bot.Schema.Attachment imageAttachement in imageAttachments)
            {
                await IngestAttachment(conversationData, turnContext, AttachmentType.Image, imageAttachement);
            }
        }

        private async Task IngestAttachment(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext, AttachmentType attachmentType, Bot.Schema.Attachment attachmentInput)
        {
            if (attachmentType == AttachmentType.PDF)
            {

                Uri fileUri = new Uri(attachmentInput.ContentUrl);

                var httpClient = new HttpClient();
                var stream = await httpClient.GetStreamAsync(fileUri);

                var ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;

                var operation = await _documentAnalysisClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", ms);

                ms.Dispose();

                AnalyzeResult result = operation.Value;

                var attachment = new SemanticKernelAgent.AgentTypes.Conversation.Attachment();
                attachment.Name = attachmentInput.Name;
                foreach (DocumentPage page in result.Pages)
                {
                    var attachmentPage = new AttachmentPage();
                    attachmentPage.Content = "";
                    for (int i = 0; i < page.Lines.Count; i++)
                    {
                        DocumentLine line = page.Lines[i];
                        attachmentPage.Content += $"{line.Content}\n";
                    }
                    // Embed content
                    var embedding = await _kernel.GetRequiredService<ITextEmbeddingGenerationService>().GenerateEmbeddingsAsync(new List<string> { attachmentPage.Content });
                    attachmentPage.Vector = embedding.First().ToArray();
                    attachment.Pages.Add(attachmentPage);
                }
                conversationData.Attachments.Add(attachment);
                var replyText = $"File {attachmentInput.Name} uploaded successfully! {result.Pages.Count()} pages ingested.";
                conversationData.History.Add(new ConversationTurn { Role = "assistant", Message = replyText });
                await turnContext.SendActivityAsync(replyText);
            }
            else if (attachmentType == AttachmentType.Image)
            {

                // Get the file extension and determine the MIME type
                string extension = Path.GetExtension(attachmentInput.Name).ToLower();
                string mimeType = extension switch
                {
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".bmp" => "image/bmp",
                    ".webp" => "image/webp",
                    _ => "application/octet-stream", // Fallback MIME type
                };

                Uri fileUri = new Uri(attachmentInput.ContentUrl);

                var httpClient = new HttpClient();
                var stream = await httpClient.GetStreamAsync(fileUri);

                var ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;
                var imageArray = ms.ToArray();
                // Construct the Data URI
                string dataUri = $"data:{mimeType};base64,{Convert.ToBase64String(imageArray)}";
                var attachment = new SemanticKernelAgent.AgentTypes.Conversation.Attachment();
                attachment.Name = attachmentInput.Name;
                attachment.Pages.Add(new AttachmentPage { Content = dataUri });
                attachment.Pages.Add(new AttachmentPage { Content = attachmentInput.ContentType });
                conversationData.Attachments.Add(attachment);
                var replyText = $"Image {attachmentInput.Name} uploaded successfully!";
                conversationData.History.Add(new ConversationTurn { Role = "assistant", Message = replyText });
                await turnContext.SendActivityAsync(replyText);
            }
        }
    }
}