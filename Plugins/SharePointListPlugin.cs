using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.BotBuilderSamples;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Azure.AI.OpenAI;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System;

namespace Plugins;
public class SharePointListPlugin
{

    private ITurnContext<IMessageActivity> _turnContext;

    public SharePointListPlugin(ConversationData conversationData, ITurnContext<IMessageActivity> turnContext)
    {

        _turnContext = turnContext;
    }

    [KernelFunction, Description("Get Items from SharePoint list by given List name. Executes when User asks for items in Sharepoint, or gives Sharepoint / Power Platform relevant List names.")]
    public async Task<string> GenerateFinalResponse(
        [Description("list name")] string SharePointListName,
        [Description("Top N Items to be retrieves, defaults to Null")] int? TopNItems
    )
    {
        await _turnContext.SendActivityAsync($"Retrieving Sharepoint List items for {SharePointListName} and item count {TopNItems}");

        //https://planbcloud.sharepoint.com/sites/planb-power/Lists/Planung/Weekly.aspx
        //https://{site_url}/_api/web/lists/GetByTitle('Test')/items

        var accessToken = "eyJ0eXAiOiJKV1QiLCJub25jZSI6ImhraFhQSWFqN0JjSmE5VVE4aThDZzZMOXgzemFEeXhiekowN1U5dnRkblEiLCJhbGciOiJSUzI1NiIsIng1dCI6InoxcnNZSEhKOS04bWdndDRIc1p1OEJLa0JQdyIsImtpZCI6InoxcnNZSEhKOS04bWdndDRIc1p1OEJLa0JQdyJ9.eyJhdWQiOiIwMDAwMDAwMy0wMDAwLTAwMDAtYzAwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC8wNzk1N2ZkNy03N2U1LTQ1OTctYmUyMS00ZmNiYzg3ZTQ1MWEvIiwiaWF0IjoxNzM1ODI4OTM4LCJuYmYiOjE3MzU4Mjg5MzgsImV4cCI6MTczNTgzMzUzNywiYWNjdCI6MCwiYWNyIjoiMSIsImFjcnMiOlsidXJuOnVzZXI6cmVnaXN0ZXJzZWN1cml0eWluZm8iXSwiYWlvIjoiQVZRQXEvOFlBQUFBM2dvaGN4SDNQbnIyT0pRaE1Qcy9PMEFhMTFtdFoyeEtOSk9GTW0vVlM4YnZEMUlBUVZoYzNZaTVQRDNQU2R3WkpkL2p5eEE2OW14M3U5UzNCV3ZPcmF6MElsSG1HRFFscHpna1liSlJMOG89IiwiYW1yIjpbInB3ZCIsInJzYSIsIm1mYSJdLCJhcHBfZGlzcGxheW5hbWUiOiJHcmFwaCBFeHBsb3JlciIsImFwcGlkIjoiZGU4YmM4YjUtZDlmOS00OGIxLWE4YWQtYjc0OGRhNzI1MDY0IiwiYXBwaWRhY3IiOiIwIiwiZGV2aWNlaWQiOiI5ZWM3ZjA2ZS0yMDc3LTRiY2ItYjNiMC05ZjBjN2IyN2QwZTUiLCJmYW1pbHlfbmFtZSI6IkZlcnJhbm8iLCJnaXZlbl9uYW1lIjoiRmxvcmlhbiIsImlkdHlwIjoidXNlciIsImlwYWRkciI6IjYyLjE1Mi4xODAuMjEyIiwibmFtZSI6IkZsb3JpYW4gRmVycmFubyIsIm9pZCI6IjE3MGE5NmI5LTY5MDYtNDY2OC04NDdhLWRiMDAyZDgxMTg0ZiIsInBsYXRmIjoiNSIsInB1aWQiOiIxMDAzMjAwMjk0MTNFMTcwIiwicmgiOiIxLkFWOEExMy1WQi1WM2wwVy1JVV9MeUg1RkdnTUFBQUFBQUFBQXdBQUFBQUFBQUFBUEFXWmZBQS4iLCJzY3AiOiJEZXZpY2VNYW5hZ2VtZW50QXBwcy5SZWFkLkFsbCBEZXZpY2VNYW5hZ2VtZW50QXBwcy5SZWFkV3JpdGUuQWxsIERldmljZU1hbmFnZW1lbnRDb25maWd1cmF0aW9uLlJlYWQuQWxsIERldmljZU1hbmFnZW1lbnRDb25maWd1cmF0aW9uLlJlYWRXcml0ZS5BbGwgRGlyZWN0b3J5LlJlYWQuQWxsIEdyb3VwLlJlYWQuQWxsIEdyb3VwLlJlYWRXcml0ZS5BbGwgb3BlbmlkIFByZXNlbmNlLlJlYWQgUHJlc2VuY2UuUmVhZC5BbGwgcHJvZmlsZSBVc2VyLlJlYWQgVXNlci5SZWFkLkFsbCBVc2VyLlJlYWRCYXNpYy5BbGwgV2luZG93c1VwZGF0ZXMuUmVhZFdyaXRlLkFsbCBlbWFpbCIsInNpZ25pbl9zdGF0ZSI6WyJpbmtub3dubnR3ayJdLCJzdWIiOiJLSzVLQV81ckk5RllnaENCenR6X2dzNW5Ld0Mxc1ZaUGxsNGVOaUoycVZzIiwidGVuYW50X3JlZ2lvbl9zY29wZSI6IkVVIiwidGlkIjoiMDc5NTdmZDctNzdlNS00NTk3LWJlMjEtNGZjYmM4N2U0NTFhIiwidW5pcXVlX25hbWUiOiJGbG9yaWFuLkZlcnJhbm9AcGxhbi1iLWdtYmguY29tIiwidXBuIjoiRmxvcmlhbi5GZXJyYW5vQHBsYW4tYi1nbWJoLmNvbSIsInV0aSI6IjVndUltMkJGa2ttQzBhbFdRcUFzQVEiLCJ2ZXIiOiIxLjAiLCJ3aWRzIjpbImI3OWZiZjRkLTNlZjktNDY4OS04MTQzLTc2YjE5NGU4NTUwOSJdLCJ4bXNfY2MiOlsiQ1AxIl0sInhtc19mdGQiOiIwTDBhdlZlODlIaDBXakdrb0p4Z0w0Z1BvQy1HbVRVaWVwYXoxRTFmTjNVIiwieG1zX2lkcmVsIjoiMSAyMCIsInhtc19zc20iOiIxIiwieG1zX3N0Ijp7InN1YiI6IjVpSERGMHpUUFJJY0Vkak5Dd1FjNHNaYXJPUVA1VFFkLVBrVmF2WEEwS2MifSwieG1zX3RjZHQiOjEzOTk5MTk5MzAsInhtc190ZGJyIjoiRVUifQ.MgXssGe25dBPzsOyjkH1goOzs7-ZHc0LqB8lPqEYwHsU3b_WaZFkZKdNuQyZuwgFiF2dXWxkDPWqmOdAmL5Cem6FWkYIkt2rwTap_s_eNWeWUyyim_UWeHsPwhFj1YVzinIatsOtdOAXL7tNDZT2fxP1dickVigTkxn-YsEgL356G-oBqgcYxW0TE2O3mjm-voMe3rHlMp2Tj3zC3gomc1xDP8GGt1rKxKkqN2H-VGtM1s0Y5OG9NFx-IzM6YNwgID0W8XKZkTdMbvZknh4T1C3sndwpCRLD5XclspNXJg0wKohDeapKO8Nk-fz_GgFIh0GZeIDUadz1DVgDJfoDJw";

        Uri uri = new Uri("https://planbcloud.sharepoint.com/sites/planb-power");
        var endpointUrl = string.Format("{0}/_api/web/lists/getbytitle('{1}')/items", uri, SharePointListName);

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose"); // Specify JSON format

        var response = httpClient.GetAsync(endpointUrl).Result;
        if (response.IsSuccessStatusCode)
        {
            var result = response.Content.ReadAsStringAsync().Result;
            return result;
        }
        else
        {
            return "";
        }
    }


}