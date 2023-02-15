using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARMBackupService_ConsoleVersion.Models
{
    public class ARMFixedObject
    {
        public string template { get; set; }
        public StringBuilder readme { get; set; }
        public Dictionary<string, string> files { get; set; }

        public ARMFixedObject()
        {
            readme = new StringBuilder();
        }
    }
}
