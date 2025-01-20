param identityName string
param location string

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview' = {
  name: identityName
  location: location
}

output tenantId string = identity.properties.tenantId
output clientId string = identity.properties.clientId
output identityId string = identity.id
