param containerVersion string
param integrationResourceGroupName string
param containerAppEnvironmentName string
param containerRegistryName string
param serviceBusName string
param location string

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2023-04-01-preview' existing = {
  name: containerAppEnvironmentName
  scope: resourceGroup(integrationResourceGroupName)
}
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2022-12-01' existing = {
  name: containerRegistryName
  scope: resourceGroup(integrationResourceGroupName)
}
resource hitsStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: uniqueString(resourceGroup().name)
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  resource hitsTableStorageService 'tableServices' existing = {
    name: 'default'
    resource sourceTable 'tables' existing = {
      name: 'hitsforcalculation'
    }
    resource tenMinutesTable 'tables' existing = {
      name: 'hitsbytenminutes'
    }
    resource totalTable 'tables' existing = {
      name: 'hitstotal'
    }
  }
}

resource hitsProcessorJob 'Microsoft.App/jobs@2023-05-01' = {
  name: 'tinylnk-jobs-totals-processor'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    environmentId: containerAppEnvironment.id
    configuration: {
      secrets: [
        {
          name: 'container-registry-secret'
          value: containerRegistry.listCredentials().passwords[0].value
        }
      ]
      replicaTimeout: 60
      replicaRetryLimit: 1
      triggerType: 'Schedule'
      scheduleTriggerConfig: {
        parallelism: 1
        replicaCompletionCount: 1
        cronExpression: '*/10 * * * *'
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          username: containerRegistry.name
          passwordSecretRef: 'container-registry-secret'
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${containerRegistry.properties.loginServer}/tinylnk-jobs-hitstotalprocessor:${containerVersion}'
          name: 'hits-total-calc-processor'
          env: [
            {
              name: 'StorageAccountName'
              value: hitsStorageAccount.name
            }
            {
              name: 'StorageSourceTableName'
              value: hitsStorageAccount::hitsTableStorageService::sourceTable.name
            }
            {
              name: 'StorageTenMinutesTableName'
              value: hitsStorageAccount::hitsTableStorageService::tenMinutesTable.name
            }
            {
              name: 'StorageTotalTableName'
              value: hitsStorageAccount::hitsTableStorageService::totalTable.name
            }
            {
              name: 'ServiceBusName'
              value: serviceBusName
            }
          ]
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
    }
  }
}

resource storageTableDataContributorRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  name: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
}
module storageTableDataContributorRoleAssignment 'roleAssignment.bicep' = {
  name: 'storageTableDataContributorRoleAssignment'
  params: {
    principalId: hitsProcessorJob.identity.principalId
    roleDefinitionId: storageTableDataContributorRoleDefinition.id
    principalType: 'ServicePrincipal'
  }
}

resource serviceBusDataSenderRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  name: '69a216fc-b8fb-44d8-bc22-1f3c2cd27a39'
}
module serviceBusDataSenderRoleAssignment 'roleAssignment.bicep' = {
  name: 'serviceBusDataSenderRoleAssignment'
  scope: resourceGroup(integrationResourceGroupName)
  params: {
    principalId: hitsProcessorJob.identity.principalId
    roleDefinitionId: serviceBusDataSenderRoleDefinition.id
    principalType: 'ServicePrincipal'
  }
}
