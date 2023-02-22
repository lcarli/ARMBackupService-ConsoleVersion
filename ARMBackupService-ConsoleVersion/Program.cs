using ARMBackupService_ConsoleVersion.Models;
using ARMBackupService_ConsoleVersion.Extensions;
using ARMBackupService_ConsoleVersion.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;



//INFOS:
var path = @"C:/Temp/";
var clientId = "{CLIENT_ID}";
var clientSecret = "{CLIENT_SECRET}";
var tenantId = "{TENANT_ID}";
bool getKVSecrets = false;




var helper = new AzureRestHelper(clientId, clientSecret, tenantId);

Console.WriteLine($"Running Get ARM Templates Orchestrator for Tenant {helper.config.azureCredentials.TenantId}");

try
{
    var listSubscriptions = helper.config.azure.Subscriptions.ListAsync().Result;
    foreach (var subscription in listSubscriptions)
    {
        var newContextModel = new CustomContext()
        {
            subscription = subscription,
            configs = helper.config
        };
        ResourceManagementClient resourceManagementClient = new ResourceManagementClient(newContextModel.configs.restClient);
        resourceManagementClient.SubscriptionId = subscription.SubscriptionId;
        var listRG = resourceManagementClient.ResourceGroups.ListAsync().Result;
        foreach (var rg in listRG)
        {
            newContextModel.resourceManagementClient = resourceManagementClient;
            newContextModel.resourceGroup = rg;

            //Temporaire way to do this
            Console.WriteLine($"Running Get ARM Templates for Tenant {newContextModel.configs.azureCredentials.TenantId}");
            try
            {
                var listFiles = await helper.GetAllInfosByResourceGroup(newContextModel.subscription, newContextModel);
                foreach (var file in listFiles)
                {
                    var _currentPath = $"{path}/{file.Key.Split("/")[0]}/{file.Key.Split("/")[1]}";
                    Directory.CreateDirectory(_currentPath);
                    File.WriteAllText($"{_currentPath}/{file.Key.Split("/")[2]}", file.Value);
                }
                Console.WriteLine($"Got All ARM Templates for {newContextModel.subscription.DisplayName}: Done");
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error during {newContextModel.subscription.DisplayName}: {ex.Message}");
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
}





Console.ReadKey();
