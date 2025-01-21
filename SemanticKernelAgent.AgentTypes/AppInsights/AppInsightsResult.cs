using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SemanticKernelAgent.AgentTypes.AppInsights;

    public class ResponseMessage {

        public string TimeStamp {get;set;}
        public string Message {get;set;}

        public ResponseMessage(string timestamp, string message){
            TimeStamp=timestamp;
            Message=message;
        }

    }
    public class AppInsightsResult
    {
    public int StatusCode { get; set; }
    public string? ResultMessage { get; set; }
    public List<ResponseMessage>? ResponseMessages { get; set; } // Added missing semicolon
    }


