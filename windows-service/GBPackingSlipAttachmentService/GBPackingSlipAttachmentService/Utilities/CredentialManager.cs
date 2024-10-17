using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using CredentialManagement;

namespace GBPackingSlipAttachmentService.Utilities
{
    public class CredentialManager : ICredentialManager
    {
        public NetworkCredential GetCredentials(string target)
        {
            var cm = new Credential { Target = target };
            if (cm.Load())
            {
                return new NetworkCredential(cm.Username, cm.Password);
            }
            return null;
        }
    }
}
