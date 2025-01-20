param app_name string
param location string
param serverFarmId string

resource appService 'Microsoft.Web/sites@2021-02-01' = {
  name: app_name
  location: location
  kind: 'app'
  properties: {
    serverFarmId: serverFarmId
    httpsOnly: true
  }

}
