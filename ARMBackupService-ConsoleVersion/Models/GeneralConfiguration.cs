using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace ARMBackupService_ConsoleVersion.Models
{
   public class GeneralConfiguration
   {
      public RestClient restClient { get; set; }
      public AzureCredentials azureCredentials { get; set; }
      public IAzure azure { get; set; }

      public GeneralConfiguration(string clientId, string clientSecret, string tenantId)
      {
            //azureCredentials = SdkContext.AzureCredentialsFactory.FromFile(path);
            clientId = "";
            clientSecret = "";
            tenantId = "";
            azureCredentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientSecret, tenantId, environment: AzureEnvironment.AzureGlobalCloud);
            restClient = RestClient
                                .Configure()
                                .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                                .WithCredentials(azureCredentials)
                                .Build();
         azure = Microsoft.Azure.Management.Fluent.Azure.Authenticate(azureCredentials).WithDefaultSubscription();
      }
   }
}
