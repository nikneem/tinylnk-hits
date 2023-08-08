targetScope = 'subscription'

param location string = deployment().location
param containerVersion string

var systemName = 'tinylnk-hits'
var locationAbbreviation = 'we'

var resourceGroupName = '${systemName}-${locationAbbreviation}'

resource targetResourceGroup 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: resourceGroupName
  location: location
}

module hitsProcessorModule 'hits-processor-resources.bicep' = {
  name: 'hits-processor-module'
  scope: targetResourceGroup
}
