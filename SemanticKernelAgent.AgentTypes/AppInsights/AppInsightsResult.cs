using System.Collections.Generic;

namespace SemanticKernelAgent.AgentTypes.AppInsights;

    public class AppInsightsResult
{
    public int StatusCode { get; set; }
    public string? ResultMessage { get; set; }
    public List<string>? ResponseMessages { get; set; } // Added missing semicolon
}

