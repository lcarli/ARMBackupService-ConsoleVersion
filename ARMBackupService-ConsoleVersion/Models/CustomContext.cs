using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ARMBackupService_ConsoleVersion.Models
{
   public class CustomContext
   {
      public ISubscription subscription { get; set; }
      public GeneralConfiguration configs { get; set; }
      public ResourceManagementClient resourceManagementClient { get; set; }
      public ResourceGroupInner resourceGroup { get; set; }
   }
}
