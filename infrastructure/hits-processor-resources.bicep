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

resource hitsProcessorJob 'Microsoft.App/jobs@2023-05-01' = {
  name: 'hitsProcessorJob'
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
          passwordSecretRef: containerRegistry.listCredentials().passwords[0].value
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
              value: serviceBus.listKeys().primaryConnectionString
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

//     template: {
//       containers: [
//         {
//           image: 'pollstarintprodneuacr.azurecr.io/pollstar-votes-job:1.0.17'
//           name: jobs_process_incoming_votes_name_param
//           env: [
//             {
//               name: 'ServiceBusConnection'
//               value: 'Endpoint=sb://pollstar-int-prod-neu-bus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=8/2hWDcGMiDrr8+FuRE5UZTO3a7Q9tBHm+tOGe1oZ6A='
//             }
//             {
//               name: 'StorageAccountConnection'
//               value: 'DefaultEndpointsProtocol=https;AccountName=mfd6x6jycr5pq;AccountKey=l40iDuzji/carbGNFrRGzf8Sr8kdEmA5GihWYN4reSB8boAnGUd+IhlaVA3FlkmyWuAqhwkqB7Zj+AStl4DMow==;EndpointSuffix=core.windows.net'
//             }
//           ]
//           resources: {
//             cpu: '0.5'
//             memory: '1Gi'
//           }
//         }
//       ]
//     }

// resource jobs_process_incoming_votes_name 'Microsoft.App/jobs@2023-04-01-preview' = {
//   name: jobs_process_incoming_votes_name_param
//   location: 'North Europe'
//   identity: {
//     type: 'None'
//   }
//   properties: {
//     environmentId: managedEnvironments_pollstar_int_prod_neu_env_externalid
//     configuration: {
//       secrets: [
//         {
//           name: 'servicebus-connection-string'
//         }
//         {
//           name: 'storage-account-connection-string'
//         }
//         {
//           name: 'pollstarintprodneuacrazurecrio-pollstarintprodneuacr'
//         }
//       ]
//       triggerType: 'Event'
//       replicaTimeout: 60
//       replicaRetryLimit: 1
//       eventTriggerConfig: {
//         replicaCompletionCount: 1
//         parallelism: 1
//         scale: {
//           minExecutions: 0
//           maxExecutions: 10
//           pollingInterval: 30
//           rules: [
//             {
//               name: 'azure-servicebus-queue-rule'
//               type: 'azure-servicebus'
//               metadata: {}
//               auth: [
//                 {
//                   secretRef: 'servicebus-connection-string'
//                   triggerParameter: 'connection'
//                 }
//               ]
//             }
//           ]
//         }
//       }
//       registries: [
//         {
//           server: 'pollstarintprodneuacr.azurecr.io'
//           username: 'pollstarintprodneuacr'
//           passwordSecretRef: 'pollstarintprodneuacrazurecrio-pollstarintprodneuacr'
//         }
//       ]
//     }
//     template: {
//       containers: [
//         {
//           image: 'pollstarintprodneuacr.azurecr.io/pollstar-votes-job:1.0.17'
//           name: jobs_process_incoming_votes_name_param
//           env: [
//             {
//               name: 'ServiceBusConnection'
//               value: 'Endpoint=sb://pollstar-int-prod-neu-bus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=8/2hWDcGMiDrr8+FuRE5UZTO3a7Q9tBHm+tOGe1oZ6A='
//             }
//             {
//               name: 'StorageAccountConnection'
//               value: 'DefaultEndpointsProtocol=https;AccountName=mfd6x6jycr5pq;AccountKey=l40iDuzji/carbGNFrRGzf8Sr8kdEmA5GihWYN4reSB8boAnGUd+IhlaVA3FlkmyWuAqhwkqB7Zj+AStl4DMow==;EndpointSuffix=core.windows.net'
//             }
//           ]
//           resources: {
//             cpu: '0.5'
//             memory: '1Gi'
//           }
//         }
//       ]
//     }
//   }
// }
