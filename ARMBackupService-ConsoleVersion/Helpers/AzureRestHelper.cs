using ARMBackupService_ConsoleVersion.Extensions;
using ARMBackupService_ConsoleVersion.Models;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace ARMBackupService_ConsoleVersion.Helpers
{
    public class AzureRestHelper
    {
        public GeneralConfiguration config;

        public AzureRestHelper(string clientId, string clientSecret, string tenantId) 
        {
            config = new GeneralConfiguration(clientId, clientSecret, tenantId);
        }


        //public async Task<Dictionary<string, string>> GetAllInfos(IAzure _azure, RestClient _restClient)
        //{
        //    var files = new Dictionary<string, string>();
        //    foreach (var subscription in await _azure.Subscriptions.ListAsync())
        //    {
        //        Console.WriteLine($"Start subscription: {subscription.DisplayName}");
        //        ResourceManagementClient resourceManagementClient = new ResourceManagementClient(_restClient);
        //        resourceManagementClient.SubscriptionId = subscription.SubscriptionId;
        //        foreach (var resourceGroup in await resourceManagementClient.ResourceGroups.ListAsync())
        //        {
        //            Console.WriteLine($"Getting Template: Subscription: {subscription.DisplayName} - Resource Group: {resourceGroup.Name}");
        //            //Get Template
        //            var template = await AzureSDKExtensions.ExportTemplate(resourceManagementClient, subscription.SubscriptionId, resourceGroup.Name);
        //            if (string.IsNullOrWhiteSpace(template))
        //            {
        //                int counter = 1;
        //                while (string.IsNullOrWhiteSpace(template))
        //                {
        //                    if (counter == 12)
        //                    {
        //                        Console.WriteLine($"Subscription {subscription.DisplayName} with Resource Group {resourceGroup.Name} doesn't exported the ARM Template.");
        //                        break;
        //                    }
        //                    Thread.Sleep(500);
        //                    template = await AzureSDKExtensions.ExportTemplate(resourceManagementClient, subscription.SubscriptionId, resourceGroup.Name);
        //                    counter++;
        //                }
        //            }
        //            Console.WriteLine($"Template of Resource Group: {resourceGroup.Name} succefully exported");
        //            if (!string.IsNullOrWhiteSpace(template))
        //            {
        //                //Indent json from Azure
        //                var toIndented = JsonConvert.DeserializeObject(template);
        //                var template_indented = JsonConvert.SerializeObject(toIndented, Formatting.Indented);

        //                var time = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        //                //await SaveIntoStorage(template, $"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}/{time}.json");
        //                await SaveIntoStorageGIT(template, $"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}.json");
        //                files.Add($"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}.json", template_indented);
        //                var templateHelper = new ARMTemplateHelper();
        //                ARMFixedObject fixedObject = await templateHelper.CheckAndFix(template, resourceManagementClient, subscription, resourceGroup, log, _restClient.Credentials.TenantId, files);
        //                //await SaveIntoStorage(fixedObject.template, $"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}/{time}_fixed.json");
        //                await SaveIntoStorageGIT(fixedObject.template, $"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}_fixed.json");
        //                files.Add($"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}_fixed.json", fixedObject.template);
        //                if (!string.IsNullOrWhiteSpace(fixedObject.readme.ToString()))
        //                {
        //                    //Enconding string for Readme
        //                    var readmeInBytes = Encoding.UTF8.GetBytes(fixedObject.readme.ToString());
        //                    var readmeEncoded = Encoding.UTF8.GetString(readmeInBytes);

        //                    //await SaveIntoStorage(readmeEncoded, $"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}/{time}_README.txt");
        //                    await SaveIntoStorageGIT(readmeEncoded, $"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}_README.txt");
        //                    files.Add($"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}_README.txt", readmeEncoded);
        //                    fixedObject.readme.Clear();
        //                }
        //            }
        //            Console.WriteLine($"Template of Resource Group: {resourceGroup.Name} succefully add to list to save after into Azure Repos");
        //        }
        //    }
        //    return files;
        //}

        //public async Task<Dictionary<string, string>> GetAllInfosBySubscription(ISubscription subscription, RestClient _restClient)
        //{
        //    var files = new Dictionary<string, string>();
        //    Console.WriteLine($"Start subscription: {subscription.DisplayName}");
        //    ResourceManagementClient resourceManagementClient = new ResourceManagementClient(_restClient);
        //    resourceManagementClient.SubscriptionId = subscription.SubscriptionId;
        //    foreach (var resourceGroup in await resourceManagementClient.ResourceGroups.ListAsync())
        //    {
        //        Console.WriteLine($"Getting Template: Subscription: {subscription.DisplayName} - Resource Group: {resourceGroup.Name}");
        //        //Get Template
        //        var template = await AzureSDKExtensions.ExportTemplate(resourceManagementClient, subscription.SubscriptionId, resourceGroup.Name);
        //        if (string.IsNullOrWhiteSpace(template))
        //        {
        //            int counter = 1;
        //            while (string.IsNullOrWhiteSpace(template))
        //            {
        //                if (counter == 12)
        //                {
        //                    Console.WriteLine($"Subscription {subscription.DisplayName} with Resource Group {resourceGroup.Name} doesn't exported the ARM Template.");
        //                    break;
        //                }
        //                Thread.Sleep(500);
        //                template = await AzureSDKExtensions.ExportTemplate(resourceManagementClient, subscription.SubscriptionId, resourceGroup.Name);
        //                counter++;
        //            }
        //        }
        //        Console.WriteLine($"Template of Resource Group: {resourceGroup.Name} succefully exported");
        //        if (!string.IsNullOrWhiteSpace(template))
        //        {
        //            //Indent json from Azure
        //            var toIndented = JsonConvert.DeserializeObject(template);
        //            var template_indented = JsonConvert.SerializeObject(toIndented, Formatting.Indented);

        //            var time = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        //            //await SaveIntoStorage(template, $"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}/{time}.json");
        //            await SaveIntoStorageGIT(template, $"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}.json");
        //            files.Add($"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}.json", template_indented);
        //            var templateHelper = new ARMTemplateHelper();
        //            ARMFixedObject fixedObject = await templateHelper.CheckAndFix(template, resourceManagementClient, subscription, resourceGroup, log, _restClient.Credentials.TenantId, files);
        //            //await SaveIntoStorage(fixedObject.template, $"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}/{time}_fixed.json");
        //            await SaveIntoStorageGIT(fixedObject.template, $"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}_fixed.json");
        //            files.Add($"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}_fixed.json", fixedObject.template);
        //            if (!string.IsNullOrWhiteSpace(fixedObject.readme.ToString()))
        //            {
        //                //Enconding string for Readme
        //                var readmeInBytes = Encoding.UTF8.GetBytes(fixedObject.readme.ToString());
        //                var readmeEncoded = Encoding.UTF8.GetString(readmeInBytes);

        //                //await SaveIntoStorage(readmeEncoded, $"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}/{time}_README.txt");
        //                await SaveIntoStorageGIT(readmeEncoded, $"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}_README.txt");
        //                files.Add($"{_restClient.Credentials.TenantId}-{subscription.DisplayName}/{resourceGroup.Name}_README.txt", readmeEncoded);
        //                fixedObject.readme.Clear();
        //            }
        //        }
        //        Console.WriteLine($"Template of Resource Group: {resourceGroup.Name} succefully add to list to save after into Azure Repos");
        //    }
        //    return files;
        //}

        public async Task<Dictionary<string, string>> GetAllInfosByResourceGroup(ISubscription subscription, CustomContext customContext)
        {
            var files = new Dictionary<string, string>();
            Console.WriteLine($"Getting Template: Subscription: {subscription.DisplayName} - Resource Group: {customContext.resourceGroup.Name}");
            //Get Template
            var template = await AzureSDKExtensions.ExportTemplate(customContext.resourceManagementClient, subscription.SubscriptionId, customContext.resourceGroup.Name);
            if (string.IsNullOrWhiteSpace(template))
            {
                int counter = 1;
                while (string.IsNullOrWhiteSpace(template))
                {
                    if (counter == 12)
                    {
                        Console.WriteLine($"Subscription {subscription.DisplayName} with Resource Group {customContext.resourceGroup.Name} doesn't exported the ARM Template.");
                        break;
                    }
                    Thread.Sleep(500);
                    template = await AzureSDKExtensions.ExportTemplate(customContext.resourceManagementClient, subscription.SubscriptionId, customContext.resourceGroup.Name);
                    counter++;
                }
            }
            Console.WriteLine($"Template of Resource Group: {customContext.resourceGroup.Name} succefully exported");
            if (!string.IsNullOrWhiteSpace(template))
            {
                //Indent json from Azure
                var toIndented = JsonConvert.DeserializeObject(template);
                var template_indented = JsonConvert.SerializeObject(toIndented, Formatting.Indented);

                var time = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                files.Add($"{customContext.configs.restClient.Credentials.TenantId}/{subscription.SubscriptionId}/{customContext.resourceGroup.Name}.json", template_indented);
                var templateHelper = new ARMTemplateHelper();
                ARMFixedObject fixedObject = await templateHelper.CheckAndFix(template, customContext.resourceManagementClient, subscription, customContext.resourceGroup, customContext.configs.restClient.Credentials.TenantId, files);
                files.Add($"{customContext.configs.restClient.Credentials.TenantId}/{subscription.SubscriptionId}/{customContext.resourceGroup.Name}_fixed.json", fixedObject.template);
                if (!string.IsNullOrWhiteSpace(fixedObject.readme.ToString()))
                {
                    //Enconding string for Readme
                    var readmeInBytes = Encoding.UTF8.GetBytes(fixedObject.readme.ToString());
                    var readmeEncoded = Encoding.UTF8.GetString(readmeInBytes);

                    files.Add($"{customContext.configs.restClient.Credentials.TenantId}/{subscription.SubscriptionId}/{customContext.resourceGroup.Name}_README.txt", readmeEncoded);
                    fixedObject.readme.Clear();
                }
            }
            Console.WriteLine($"Template of Resource Group: {customContext.resourceGroup.Name} succefully add to list to save after into Azure Repos");
            return files;
        }

    }
}
