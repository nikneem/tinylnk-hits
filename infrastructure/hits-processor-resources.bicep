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
resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: serviceBusName
  scope: resourceGroup(integrationResourceGroupName)
}

var serviceBusEndpoint = '${serviceBus.id}/AuthorizationRules/RootManageSharedAccessKey'
var serviceBusConnectionString = listKeys(serviceBusEndpoint, serviceBus.apiVersion).primaryConnectionString

resource hitsStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: uniqueString(resourceGroup().name)
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  resource hitsTableStorageService 'tableServices' = {
    name: 'default'
    resource table 'tables' = {
      name: 'hits'
    }
  }
}

resource hitsProcessorJob 'Microsoft.App/jobs@2023-05-01' = {
  name: 'tinylnk-jobs-hits-processor'
  location: location
  properties: {
    environmentId: containerAppEnvironment.id
    configuration: {
      secrets: [
        {
          name: 'servicebus-connection-string'
          value: serviceBusConnectionString
        }
        {
          name: 'container-registry-secret'
          value: containerRegistry.listCredentials().passwords[0].value
        }
      ]
      replicaTimeout: 60
      replicaRetryLimit: 1
      triggerType: 'Event'
      eventTriggerConfig: {
        replicaCompletionCount: 1
        parallelism: 1
        scale: {
          minExecutions: 0
          maxExecutions: 10
          pollingInterval: 30
          rules: [
            {
              name: 'azure-servicebus-queue-rule'
              type: 'azure-servicebus'
              metadata: any({
                queueName: 'hits'
                connection: 'servicebus-connection-string'
              })
              auth: [
                {
                  secretRef: 'servicebus-connection-string'
                  triggerParameter: 'connection'
                }
              ]
            }
          ]
        }
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
          image: '${containerRegistry.properties.loginServer}/hits-processor:${containerVersion}'
          name: 'hits-processor'
          env: [
            {
              name: 'ServiceBusConnection'
              secretRef: 'servicebus-connection-string'
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
