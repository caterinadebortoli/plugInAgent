param app_name string
param app_service_plan string 
param tier string
param resourceGroupName string
param resourceGroupLocation string
param identityName string
param botName string
param botLocation string
param botSku string

targetScope = 'subscription'

resource newRG 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: resourceGroupLocation
}

module identityModule 'identity.bicep' = {
  name: 'identityModule'
  scope: newRG
  params: {
    identityName: identityName
    location: resourceGroupLocation
  }
}

module appServicePlanModule 'appServicePlan.bicep' = {
  name: 'appServicePlanModule'
  scope: newRG
  params: {
    app_service_plan: app_service_plan
    tier: tier
    location: resourceGroupLocation
  }
}

module appServiceModule 'appService.bicep' = {
  name: 'appServiceModule'
  scope: newRG
  params: {
    app_name: app_name
    location: resourceGroupLocation
    serverFarmId: appServicePlanModule.outputs.serverFarmId
  }
}

module workspace 'workspace.bicep'={
  name: 'workspaceModule'
  scope: newRG
}

module appInsights 'appInsights.bicep'={
  name: 'appInsightsModule'
  scope:newRG
  params:{workspaceId: workspace.outputs.workspaceId}
}
module botModule 'bot.bicep' = {
  name: 'botModule'
  scope: newRG
  params: {
    botName: botName
    botLocation: botLocation
    botSku: botSku
    tenantId: identityModule.outputs.tenantId
    clientId: identityModule.outputs.clientId
    identityId: identityModule.outputs.identityId
    appServiceEndpoint: 'https://${app_name}.azurewebsites.net/api/messages'
    developerAppInsightsApplicationId:appInsights.outputs.appInsightsApplicationId
    developerAppInsightKey:appInsights.outputs.appInsightsKey
  }
}

module communicationServiceModule 'communicationService.bicep'={
  	name:'communicationServiceModule'
    scope:newRG
}
