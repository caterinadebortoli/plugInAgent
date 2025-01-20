param app_service_plan string
param tier string
param location string

resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: app_service_plan
  location: location
  sku: {
    name: tier
    tier: 'Basic'
  }
  kind: 'linux'

}

output serverFarmId string = appServicePlan.id
