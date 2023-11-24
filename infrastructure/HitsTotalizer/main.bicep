targetScope = 'subscription'

param containerVersion string
param integrationResourceGroupName string
param containerAppEnvironmentName string
param location string = deployment().location

param integrationEnvironment object

var systemName = 'tinylnk-hits'
var locationAbbreviation = 'ne'

var resourceGroupName = '${systemName}-${locationAbbreviation}'

resource targetResourceGroup 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: resourceGroupName
  location: location
}

module hitsProcessorModule 'hits-total-processor-resources.bicep' = {
  name: 'hits-processor-module'
  scope: targetResourceGroup
  params: {
    containerVersion: containerVersion
    location: location
    integrationEnvironment: integrationEnvironment
    integrationResourceGroupName: integrationResourceGroupName
    containerAppEnvironmentName: containerAppEnvironmentName
  }
}
