param workspaceId string

resource appInsights 'Microsoft.Insights/components@2020-02-02'={
  kind: 'web'
  name: 'BotAppInsightsPlanB'
  location: 'eastus'
  properties:{
    Application_Type: 'web'
    WorkspaceResourceId: workspaceId
  }
}

output appInsightsApplicationId string = appInsights.properties.AppId
output appInsightsKey string = appInsights.properties.InstrumentationKey

