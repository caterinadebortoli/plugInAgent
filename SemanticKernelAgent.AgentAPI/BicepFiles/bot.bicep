param botName string
param botLocation string
param botSku string
param tenantId string
param clientId string
param identityId string
param appServiceEndpoint string
param developerAppInsightKey string
param developerAppInsightsApplicationId string

resource bot 'Microsoft.BotService/botServices@2023-09-15-preview' = {
  name: botName
  location: botLocation
  properties: {
    tenantId: '07957fd7-77e5-4597-be21-4fcbc87e451a'
    msaAppType:'UserAssignedMSI'
    displayName: botName
    endpoint: appServiceEndpoint
    msaAppId: clientId
    msaAppMSIResourceId: identityId
    msaAppTenantId: tenantId
    developerAppInsightKey: developerAppInsightKey
    developerAppInsightsApplicationId: developerAppInsightsApplicationId
    isStreamingSupported: false
    publicNetworkAccess: 'Enabled'
    schemaTransformationVersion:'1.3'
    
  }
  
  sku: {
    name: botSku
  }
  kind:'azurebot'
  
}
