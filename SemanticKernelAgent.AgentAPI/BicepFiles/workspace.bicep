resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  location: 'eastus'
  name: 'BotWorkspace'
  properties:{
    sku: {name:'PerGB2018'}
  }
}

output workspaceId string = workspace.id
