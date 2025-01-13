using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Neo4j.Driver;
using Neo4j.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory;

namespace SemanticKernelAgent.AgentCore.Services;

public class Neo4jGraphClient
{

    private readonly IDriver _driver;

    public Neo4jGraphClient(IConfiguration configuration)
    {
        _driver=GraphDatabase.Driver(configuration.GetValue<string>("NEO4J_URI"), AuthTokens.Basic(configuration.GetValue<string>("NEO4J_USER"),configuration.GetValue<string>("NEO4J_PASSWORD")));
    }

    public async Task<string> CreateQuery(string? NodeType, int? TopNItems){
        if(NodeType!=null && TopNItems==null)
        {
            return $"MATCH(a:{NodeType}) RETURN a";
        }
        else if(NodeType==null && TopNItems!=null)
        {
            return $"MATCH(a) ORDER BY a.creation_date DESC RETURN a LIMIT {TopNItems}";
        }
        else if(NodeType!=null && TopNItems!=null)
        {
            return $"MATCH(a:{NodeType}) ORDER BY a.creation_date DESC RETURN a LIMIT {TopNItems}";
        }
        else
        {
            return "MATCH(a) RETURN a";
        }
    }

    public async Task<Object> ExecuteQuery(string query){
        
        await using var session=_driver.AsyncSession();
        var result = await session.RunAsync(query);
        var record=result.ToListAsync();
        await session.CloseAsync();
        return record.As<string>();
    }


}
