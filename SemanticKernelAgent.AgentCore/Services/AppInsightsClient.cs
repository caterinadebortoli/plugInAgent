using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using System;
using Azure;
using System.Drawing;
using SemanticKernelAgent.AgentTypes.AppInsights;
using Azure.Core;
using Microsoft.Graph.Models;
using Microsoft.Bot.Configuration;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System.ClientModel.Primitives;
namespace SemanticKernelAgent.AgentCore.Services;

public class AppInsightsClient
{

    private readonly string _apiKey;



    public AppInsightsClient(IConfiguration configuration)
    {
  
        _apiKey=configuration.GetValue<string>("ApiKey");

    }
    public async Task<string> CreateQuery(string tableName, int? TopNItems, bool HasSeverityLevel, int? SeverityLevel)
    {
        if(TopNItems==null && HasSeverityLevel==false){
           return $"{tableName} | project timestamp, message"; 
        } 
        else if(TopNItems!=null && HasSeverityLevel==false){
         return $"{tableName} | top {TopNItems} by timestamp | project timestamp, message";
        }
        else if(TopNItems!=null && HasSeverityLevel==true){
           return $"{tableName} | top {TopNItems} by severityLevel | project timestamp, message"; 
        }
        else {
            return $"{tableName} | where severityLevel == {SeverityLevel} | project timestamp, message";
        }
    }

    public async Task<AppInsightsResult> ExecuteQuery(string query)
        {
        
        AppInsightsResult appInsightsResult = new AppInsightsResult();
        appInsightsResult.ResponseMessages = new List<ResponseMessage>();


    
        try{
        
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-api-key",_apiKey);
            
        var requestBody = new
        {
            query = query
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        
            
            var response= await client.PostAsync(requestUri:"https://api.applicationinsights.io/v1/apps/3569fc86-6318-4348-930d-1e6eb67002c9/query",content);
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseData);

            if (json != null)
            {
                foreach (var table in json["tables"])
                {
                    var rows = table["rows"];
                    if (rows.Count() > 0)
                    {
                        
                        foreach (JToken row in rows)
                        {   
                            ResponseMessage responseMessage = new ResponseMessage(Convert.ToString(row[0]),Convert.ToString(row[1]));
                    
                            appInsightsResult.ResponseMessages.Add(responseMessage);
                        }
                    }
                else{

                    appInsightsResult.StatusCode=200;
                    appInsightsResult.ResultMessage="Queries successfully executed, no information found for this topic";
                    return appInsightsResult;
                }
            }
            appInsightsResult.StatusCode=200;
            appInsightsResult.ResultMessage="Queries successfully executed";
            return appInsightsResult;
        }
        else{

            appInsightsResult.StatusCode=400;
            appInsightsResult.ResultMessage="Queries successfully executed, no information found";
            return appInsightsResult;
        }
        }
        catch(Exception ex){

            appInsightsResult.StatusCode=500;
            appInsightsResult.ResultMessage=ex.Message;
            return appInsightsResult;
        }

        }

}
