targetScope = 'subscription'

param containerVersion string
param integrationResourceGroupName string
param containerAppEnvironmentName string
param integrationEnvironment object

param location string = deployment().location

var systemName = 'tinylnk-hits'
var defaultResourceName = '${systemName}-ne'

resource targetResourceGroup 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: defaultResourceName
  location: location
}

module resourcesModule 'resources.bicep' = {
  name: 'resourcesModule'
  scope: targetResourceGroup
  params: {
    containerVersion: containerVersion
    location: location
    integrationResourceGroupName: integrationResourceGroupName
    containerAppEnvironmentName: containerAppEnvironmentName
    integrationEnvironment: integrationEnvironment
  }
}
