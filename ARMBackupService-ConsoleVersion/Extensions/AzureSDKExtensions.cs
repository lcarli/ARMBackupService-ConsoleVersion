using Microsoft.Azure.Management.ResourceManager.Fluent;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ARMBackupService_ConsoleVersion.Extensions
{
   public static class AzureSDKExtensions
   {
      public static async Task<string> ExportTemplate(ResourceManagementClient resourceManagementClient, string subscriptionId, string resourceGroupName)
      {
         var baseURL = $"https://management.azure.com/subscriptions/{subscriptionId}/resourcegroups/{resourceGroupName}/exportTemplate?api-version=2021-04-01";
         var body = new
         {
            resources = new string[] { "*" },
            options = "IncludeParameterDefaultValue,IncludeComments"
         };
         var _result = await customPOST(resourceManagementClient, baseURL, body);
         return _result;
      }

      public static async Task<string> ExportAutomationRunbook(ResourceManagementClient resourceManagementClient, string subscriptionId, string resourceGroupName, string automationAccountName, string runbookName)
      {
         var baseURL = $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Automation/automationAccounts/{automationAccountName}/runbooks/{runbookName}/content?api-version=2015-10-31";
         var _result = await customGET(resourceManagementClient, baseURL);
         return _result;
      }

      public static async Task<string> ExportLogicAppWorkflow(ResourceManagementClient resourceManagementClient, string subscriptionId, string resourceGroupName, string workflowName)
      {
         var baseURL = $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Logic/workflows/{workflowName}?api-version=2016-06-01";
         var _result = await customGET(resourceManagementClient, baseURL);
         return _result;
      }

      private static async Task<string> customGET(ResourceManagementClient resourceManagementClient, string url)
      {
         var _httpRequest = new HttpRequestMessage();
         HttpResponseMessage _httpResponse = null;
         _httpRequest.Method = new HttpMethod("GET");
         _httpRequest.RequestUri = new Uri(url);
         _httpRequest.Headers.TryAddWithoutValidation("x-ms-client-request-id", Guid.NewGuid().ToString());
         _httpRequest.Headers.TryAddWithoutValidation("accept-language", resourceManagementClient.AcceptLanguage);
         CancellationToken cancellationToken = default(CancellationToken);
         cancellationToken.ThrowIfCancellationRequested();
         await resourceManagementClient.Credentials.ProcessHttpRequestAsync(_httpRequest, cancellationToken).ConfigureAwait(false);
         _httpResponse = await resourceManagementClient.HttpClient.SendAsync(_httpRequest, cancellationToken).ConfigureAwait(false);
         return await _httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
      }

      private static async Task<string> customPOST(ResourceManagementClient resourceManagementClient, string url, object body)
      {
         var _httpRequest = new HttpRequestMessage();
         HttpResponseMessage _httpResponse = null;
         _httpRequest.Method = new HttpMethod("POST");
         _httpRequest.RequestUri = new Uri(url);
         _httpRequest.Headers.TryAddWithoutValidation("x-ms-client-request-id", Guid.NewGuid().ToString());
         _httpRequest.Headers.TryAddWithoutValidation("accept-language", resourceManagementClient.AcceptLanguage);
         if (body != null)
         {
            _httpRequest.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
         }
         CancellationToken cancellationToken = default(CancellationToken);
         cancellationToken.ThrowIfCancellationRequested();
         await resourceManagementClient.Credentials.ProcessHttpRequestAsync(_httpRequest, cancellationToken).ConfigureAwait(false);
         _httpResponse = await resourceManagementClient.HttpClient.SendAsync(_httpRequest, cancellationToken).ConfigureAwait(false);
         if (_httpResponse.StatusCode == System.Net.HttpStatusCode.Accepted)
         {
            Thread.Sleep(TimeSpan.FromSeconds(20));
            var retry = await customGET(resourceManagementClient, _httpResponse.Headers.Location.OriginalString);
            if (string.IsNullOrWhiteSpace(retry))
            {
               Thread.Sleep(TimeSpan.FromSeconds(10));
               retry = await customGET(resourceManagementClient, _httpResponse.Headers.Location.OriginalString);
            }
            return retry;
         }
         else
         {
            return await _httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
         }
         
      }
   }
}
