using ARMBackupService_ConsoleVersion.Extensions;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ARMBackupService_ConsoleVersion.Models;
using System.Linq;

namespace ARMBackupService_ConsoleVersion.Helpers
{
	public class ARMTemplateHelper
	{
		private ISubscription subscription;
		private ResourceGroupInner resourceGroup;
		private ResourceManagementClient restClient;
		private ILogger log;
		public ARMFixedObject fixedObject;

		public ARMTemplateHelper()
		{

		}

		public async Task<ARMFixedObject> CheckAndFix(string template, ResourceManagementClient _restClient, ISubscription _subscription, ResourceGroupInner _resourceGroup, string tenantId, Dictionary<string, string> files)
		{
			subscription = _subscription;
			resourceGroup = _resourceGroup;
			restClient = _restClient;
			fixedObject = new ARMFixedObject();
			fixedObject.files = files;
			fixedObject.template = await template.FixAKV(fixedObject, restClient, subscription, resourceGroup)
			   .FixStorage(fixedObject, restClient, subscription, resourceGroup)
			   .FixCertificates(fixedObject, restClient, subscription, resourceGroup)
			   .FixIdentity(fixedObject, restClient, subscription, resourceGroup)
			   .FixActionGroups(fixedObject, restClient, subscription, resourceGroup)
			   .FixAutomation(fixedObject, restClient, subscription, resourceGroup, tenantId).GetAwaiter().GetResult()
			   .FixUniqueParameters(fixedObject, restClient, subscription, resourceGroup)
			   .FixLogicApps(fixedObject, restClient, subscription, resourceGroup); //Logic Apps must be the last because he ignores the template parameter.
			return fixedObject;
		}
	}

	public static class StringExtensions
	{
		public static string FixSQL(this string template, ARMFixedObject fixedObject, ResourceManagementClient restClient, ISubscription subscription, ResourceGroupInner resourceGroup)
		{
			//log.LogInformation("Start Search SQL");
			JObject parsedTemplate = JObject.Parse(template);
			JObject mainObject = (JObject)parsedTemplate["template"];
			JArray resources = (JArray)mainObject["resources"];
			JObject parameters = (JObject)mainObject["parameters"];
			List<string> parametersToRemove = new List<string>();
			List<JToken> itemsToRemove = new List<JToken>();
			string admName = string.Empty;
			foreach (var item in parameters)
			{
				if (item.Key.StartsWith("vulnerabilityAssessments"))
				{
					parametersToRemove.Add(item.Key);
				}
			}
			foreach (var p in parametersToRemove)
			{
				parameters.Property(p).Remove();
			}
			foreach (var item in resources)
			{
				var type = (string)item["type"];
				if (type == "Microsoft.Sql/servers")
				{
					admName = item["properties"]["administratorLogin"].ToString();
					item["properties"]["administratorLogin"] = "[parameters('sqlServerAdministrator')]";
					(item["properties"] as JObject).AddAfterSelf(new JProperty("administratorLoginPassword", "[parameters('sqlServerAdministratorPwd')]"));
					//Add parameters
					var login = new
					{
						defaultValue = admName,
						type = "String"
					};
					parameters.Add("sqlServerAdministrator", JObject.FromObject(login));
					var password = new
					{
						defaultValue = "",
						type = "String"
					};
					parameters.Add("sqlServerAdministratorPwd", JObject.FromObject(password));
				}
				if (type == "Microsoft.Sql/servers/vulnerabilityAssessments")
				{
					var containerPath = item["properties"]["storageContainerPath"];
					if (containerPath != null || containerPath.Count() > 0)
					{
						itemsToRemove.Add(item);
						fixedObject.readme.Append($" - Ajouter le VULNERABILITY ASSESSMENT pour le SQL Server dans l'ongle SECURITY > ADVANCED DATA SECURITY");
						fixedObject.readme.Append(Environment.NewLine);
					}
				}
				if (type == "Microsoft.Sql/servers/keys")
				{
					if (item["name"].ToString().Contains("ServiceManaged"))
					{
						itemsToRemove.Add(item);
					}
				}
			}
			foreach (var i in itemsToRemove)
			{
				resources.Remove(i);
			}
			return parsedTemplate.ToString();
		}


		public static string FixStorage(this string template, ARMFixedObject fixedObject, ResourceManagementClient restClient, ISubscription subscription, ResourceGroupInner resourceGroup)
		{
			//log.LogInformation("Start Search storage");
			JObject parsedTemplate = JObject.Parse(template);
			JObject mainObject = (JObject)parsedTemplate["template"];
			JArray resources = (JArray)mainObject["resources"];
			foreach (var item in resources)
			{
				var type = (string)item["type"];
				if (type == "Microsoft.Storage/storageAccounts")
				{
					//Check if there is VNET Rules
					var VnetRules = item["properties"]["networkAcls"]["virtualNetworkRules"];
					if (VnetRules != null && VnetRules.Count() > 0)
					{
						//GET Subnet name for this storage (just one subnet?)
						var sid = VnetRules[0]["id"];
						var subnetName = ((string)sid).Substring(((string)sid).LastIndexOf("/"));
						//GET list of subnets
						var subnetList = resources.Where(r => (string)r["type"] == "Microsoft.Network/virtualNetworks/subnets");
						//search for the correct subnet. Using foreach is more clear to understand the process then LINQ
						foreach (var s in subnetList)
						{
							var sName = ((string)s["name"]).Substring(((string)s["name"]).LastIndexOf("/"));
							if (sName == subnetName)
							{
								string[] dependency = new string[] { $"[resourceId('Microsoft.Network/virtualNetworks/subnets', {s["name"]})]" };
								(item as JObject).Add("dependsOn", JArray.FromObject(dependency));
							}
						}
					}
				}
			}
			return parsedTemplate.ToString();
		}

		public static async Task<string> FixLogicApps(this string template, ARMFixedObject fixedObject, ResourceManagementClient restClient, ISubscription subscription, ResourceGroupInner resourceGroup)
		{
			//log.LogInformation("Start Search Logic Apps");
			Dictionary<string, string> workflowsName = new Dictionary<string, string>();
			JObject parsedTemplate = JObject.Parse(template);
			JObject mainObject = (JObject)parsedTemplate["template"];
			JObject parameters = (JObject)mainObject["parameters"];
			//List<string> parametersToRemove = new List<string>();
			foreach (var item in parameters)
			{
				if (item.Key.StartsWith("workflows_"))
				{
					JObject value = (JObject)item.Value;
					workflowsName.Add(item.Key, (string)value["defaultValue"]);
					//parametersToRemove.Add(item.Key);
				}
			}
			//foreach (var p in parametersToRemove)
			//{
			//   parameters.Property(p).Remove();
			//}
			JArray resources = (JArray)mainObject["resources"];
			List<JToken> itemsToRemove = new List<JToken>();
			Dictionary<JToken, JToken> listOfTemplateItem = new Dictionary<JToken, JToken>();
			foreach (var item in resources)
			{
				var type = (string)item["type"];
				if (type == "Microsoft.Logic/workflows")
				{
					//Export Workflow Definition if its not there ***NOT CONTAINS SCHEMA***
					var definition = item["properties"]["definition"];
					if (!definition.ToString().Contains("$schema"))
					{
						string originalName = (string)item["name"];
						string substringedName = originalName.Substring(originalName.IndexOf("workflow")).Replace("')]", "");
						string name = workflowsName.Where(w => w.Key == substringedName).FirstOrDefault().Value;
						var workflowDefinition = await AzureSDKExtensions.ExportLogicAppWorkflow(restClient, subscription.SubscriptionId, resourceGroup.Name, name);
						var workflowObject = JObject.Parse(workflowDefinition);
						var workflowObjectProperties = workflowObject["properties"];
						(workflowObjectProperties as JObject).Property("provisioningState").Remove();
						(workflowObjectProperties as JObject).Property("createdTime").Remove();
						(workflowObjectProperties as JObject).Property("changedTime").Remove();
						(workflowObjectProperties as JObject).Property("accessEndpoint").Remove();
						(workflowObjectProperties as JObject).Property("version").Remove();
						//Replace the template with the correct Workflow Definition
						item["properties"] = workflowObjectProperties;
					}
				}
				if (type == "Microsoft.Web/connections")
				{
					var connectorType = ((string)item["properties"]["api"]["id"]).Split('/').Last();
					//Create connector templates
					//var conf = JObject.Parse(await GetConfig("LogicAppsConnectorTemplates.json"));
					//JArray templates = (JArray)conf["models"];
					//foreach (var model in templates)
					//{
					//   if ((string)model["azureType"] == connectorType)
					//   {
					//      listOfTemplateItem.Add(model, item);
					//   }
					//}
				}
			}
			foreach (var TemplateItem in listOfTemplateItem)
			{
				if (TemplateItem.Key != null)
				{
					(TemplateItem.Key as JObject).Property("azureType").Remove();
					//Organize Parameters
					//Change parameters variable Name and DisplayName for the name of the connection with the original one
					TemplateItem.Key["name"] = TemplateItem.Key["properties"]["displayName"] = TemplateItem.Value["name"];
					resources.Add(TemplateItem.Key);
					itemsToRemove.Add(TemplateItem.Value);
					//foreach parameter values that shis connector needs, adds to the scope of ARM Template
					var parameterValues = (JObject)TemplateItem.Key["properties"]["parameterValues"];
					if (parameterValues != null)
					{
						foreach (var parameterValue in parameterValues)
						{
							var value = ((string)parameterValue.Value).Replace("[parameters('", "").Replace("')]", "");
							var obj = new
							{
								defaultValue = value,
								type = "String"
							};
							parameters.Add(value, JObject.FromObject(obj));
						}
					}
				}
			}
			foreach (var i in itemsToRemove)
			{
				resources.Remove(i);
			}
			if (workflowsName.Count > 0)
			{
				fixedObject.readme.Append($" - Un/plusieurs Logic Apps a/ont été trouvé. Nous avons modifier les workflows : {String.Join(", ", workflowsName.Keys.ToArray())}. " +
				   $"Vous pouvez faire attention aux connectors qu'ils ont besoin. Pour les SQL Connectors, si jamais vous trouvez de problemes, vous pouvez voir le connector en utilisent le code view" +
				   $"pour en voir plus de détails.");
				fixedObject.readme.Append(Environment.NewLine);
			}
			return parsedTemplate["template"].ToString();
		}

		public static async Task<string> FixAutomation(this string template, ARMFixedObject fixedObject, ResourceManagementClient restClient, ISubscription subscription, ResourceGroupInner resourceGroup, string tenantId)
		{
			//log.LogInformation("Start Search Automation");
			string automationAccountName = string.Empty;
			List<string> runbooksName = new List<string>();
			JObject parsedTemplate = JObject.Parse(template);
			JObject mainObject = (JObject)parsedTemplate["template"];
			JObject parameters = (JObject)mainObject["parameters"];
			List<string> parametersToRemove = new List<string>();
			foreach (var item in parameters)
			{
				if (item.Key.StartsWith("certificates_") && item.Key.EndsWith("base64Value"))
				{
					parametersToRemove.Add(item.Key);
				}

				if (item.Key.StartsWith("automationAccounts"))
				{
					JObject value = (JObject)item.Value;
					automationAccountName = (string)value["defaultValue"];
					parametersToRemove.Add(item.Key);
				}
			}
			foreach (var p in parametersToRemove)
			{
				parameters.Property(p).Remove();
			}
			JArray resources = (JArray)mainObject["resources"];
			List<JToken> itemsToRemove = new List<JToken>();
			foreach (var item in resources)
			{
				var type = (string)item["type"];
				if (type == "Microsoft.Automation/automationAccounts/runbooks")
				{
					var name = (string)item["name"];
					int index = name.IndexOf("/");
					var runbookName = name.Substring(index + 1).Replace("')]", "");
					runbooksName.Add(runbookName);
				}
				if (type.Contains("Microsoft.Automation"))
				{
					itemsToRemove.Add(item);
				}
			}
			foreach (var i in itemsToRemove)
			{
				resources.Remove(i);
			}
			//Export Runbooks
			foreach (var r in runbooksName)
			{
				var runbook = await AzureSDKExtensions.ExportAutomationRunbook(restClient, subscription.SubscriptionId, resourceGroup.Name, automationAccountName, r);
				fixedObject.files.Add($"{tenantId}-{subscription.DisplayName}/{resourceGroup.Name}/runbook-{r}.ps1", runbook);
			}
			if (runbooksName.Count > 0)
			{
				fixedObject.readme.Append($" - Ajouter un Automation Service. Les Runbooks {String.Join(", ", runbooksName.ToArray())} ont été sauvegardé et vous pouvez les télécharger directment.");
				fixedObject.readme.Append(Environment.NewLine);
			}
			return parsedTemplate.ToString();
		}

		public static string FixActionGroups(this string template, ARMFixedObject fixedObject, ResourceManagementClient restClient, ISubscription subscription, ResourceGroupInner resourceGroup)
		{
			//log.LogInformation("Start Search Action Groups");
			JObject parsedTemplate = JObject.Parse(template);
			JObject mainObject = (JObject)parsedTemplate["template"];
			JObject parameters = (JObject)mainObject["parameters"];
			foreach (var item in parameters)
			{
				if (item.Key.StartsWith("actiongroups_"))
				{
					JObject value = (JObject)item.Value;
					string valueAsString = (string)value["defaultValue"];
					if (valueAsString.Contains("providers"))
					{
						int indexToReplace = valueAsString.IndexOf("/providers");
						string currentValue = "[concat('/subscriptions/',subscription().id, '/resourceGroups/', resourceGroup().name,";
						string substringed = valueAsString.Substring(indexToReplace);
						string finalValue = $"{currentValue}'{substringed}')]";
						parameters[item.Key]["defaultValue"] = finalValue;
					}
				}
			}
			return parsedTemplate.ToString();
		}

		public static string FixCertificates(this string template, ARMFixedObject fixedObject, ResourceManagementClient restClient, ISubscription subscription, ResourceGroupInner resourceGroup)
		{
			//log.LogInformation("Start Search Certificates");
			JObject parsedTemplate = JObject.Parse(template);
			JObject mainObject = (JObject)parsedTemplate["template"];
			JObject parameters = (JObject)mainObject["parameters"];
			List<string> parametersToRemove = new List<string>();
			foreach (var item in parameters)
			{
				if (item.Key.StartsWith("certificates_") && !item.Key.EndsWith("base64Value")) //Not end with base64
				{
					parametersToRemove.Add(item.Key);
				}
			}
			foreach (var p in parametersToRemove)
			{
				parameters.Property(p).Remove();
			}
			JArray resources = (JArray)mainObject["resources"];
			List<JToken> itemsToRemove = new List<JToken>();
			foreach (var item in resources)
			{
				var type = (string)item["type"];
				if (type == "Microsoft.Web/certificates")
				{
					itemsToRemove.Add(item);
				}
			}
			foreach (var i in itemsToRemove)
			{
				resources.Remove(i);
			}
			if (itemsToRemove.Count > 0)
			{
				fixedObject.readme.Append($" - Le/La WebSite/Function avait un certificat. Il faut faire l'upload et ajouter le domain.");
				fixedObject.readme.Append(Environment.NewLine);
			}
			return parsedTemplate.ToString();
		}

		public static string FixIdentity(this string template, ARMFixedObject fixedObject, ResourceManagementClient restClient, ISubscription subscription, ResourceGroupInner resourceGroup)
		{
			//log.LogInformation("Start Search Identity");
			JObject parsedTemplate = JObject.Parse(template);
			JObject mainObject = (JObject)parsedTemplate["template"];
			JArray resources = (JArray)mainObject["resources"];
			foreach (var item in resources)
			{
				var type = (string)item["type"];
				if (type == "Microsoft.Web/sites" || type == "Microsoft.Web/sites/slots")
				{
					var a = item["identity"];
					if (item["identity"] != null)
					{
						fixedObject.readme.Append($" - Le/La WebSite/Function avait un Identité. L'indentité a été créé mais les droites doivent être ajouter.");
						fixedObject.readme.Append(Environment.NewLine);
						(item["identity"] as JObject).Property("principalId").Remove();
						(item["identity"] as JObject).Property("tenantId").Remove();
					}
				}
			}
			return parsedTemplate.ToString();
		}

		public static async Task<string> FixAKV(this string template, ARMFixedObject fixedObject, ResourceManagementClient restClient, ISubscription subscription, ResourceGroupInner resourceGroup)
		{
			//log.LogInformation("Start Search AKV");
			JObject parsedTemplate = JObject.Parse(template);
			JObject mainObject = (JObject)parsedTemplate["template"];
			JArray resources = (JArray)mainObject["resources"];
			foreach (var item in resources)
			{
				var type = (string)item["type"];
				if (type == "Microsoft.KeyVault/vaults/secrets")
				{
					var secretName = (string)item["name"];
					int index = secretName.IndexOf("/");
					var keyVaultName = (secretName.Split(',')[0]).Split('_')[1];
					var keyVaultsSecrets = await AzureSDKExtensions.ExportKeyVaultSecrets(restClient, subscription.SubscriptionId, resourceGroup.Name, keyVaultName, secretName.Substring(index + 1));
					fixedObject.readme.Append($" - Ajouter le valeur pour le secret {secretName.Substring(index + 1).Replace("')]", "")} - Valeur: {keyVaultsSecrets}");
					fixedObject.readme.Append(Environment.NewLine);
					(item["properties"] as JObject).Property("attributes").AddAfterSelf(new JProperty("value", "String"));
				}
			}

			return parsedTemplate.ToString();
		}

		public static string FixUniqueParameters(this string template, ARMFixedObject fixedObject, ResourceManagementClient restClient, ISubscription subscription, ResourceGroupInner resourceGroup)
		{
			//log.LogInformation("Start Cleaning Parameters never used");
			JObject parsedTemplate = JObject.Parse(template);
			JObject mainObject = (JObject)parsedTemplate["template"];
			JObject parameters = (JObject)mainObject["parameters"];
			List<string> parametersToRemove = new List<string>();
			foreach (var item in parameters)
			{
				if (CountStringOccurrences(parsedTemplate.ToString(), item.Key) == 1)
				{
					JObject value = (JObject)item.Value;
					parametersToRemove.Add(item.Key);
				}
			}
			foreach (var p in parametersToRemove)
			{
				parameters.Property(p).Remove();
			}
			return parsedTemplate.ToString();
		}

		public static int CountStringOccurrences(string text, string pattern)
		{
			// Loop through all instances of the string 'text'.
			int count = 0;
			int i = 0;
			while ((i = text.IndexOf(pattern, i)) != -1)
			{
				i += pattern.Length;
				count++;
			}
			return count;
		}

	}
}
