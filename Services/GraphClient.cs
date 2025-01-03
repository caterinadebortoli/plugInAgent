using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Models;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Services
{
    public class GraphClient
    {
        private readonly string _tenantId;
        private readonly string _clientId;
        private readonly string _secret;

        private readonly GraphServiceClient _graphClient;

        public GraphClient(IConfiguration configuration) 
        {
            _tenantId = configuration.GetValue<string>("GRAPH_API_TENANT_ID");
            _clientId = configuration.GetValue<string>("GRAPH_API_CLIENT_ID");
            _secret = configuration.GetValue<string>("GRAPH_API_CLIENT_SECRET");
            var scopes = new[] { "https://graph.microsoft.com/.default" };

             // using Azure.Identity;
            var options = new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            };

            // https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var clientSecretCredential = new ClientSecretCredential(_tenantId, _clientId, _secret, options);

            _graphClient = new GraphServiceClient(clientSecretCredential, scopes);


        }
        public async Task<Site> GetSite()
        {
            var site = await _graphClient.Sites["{site-id}"].GetAsync();
            return site;
        }

    }
}