using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GBPackingSlipAttachmentService.Utilities
{
    public interface ICredentialManager
    {
        //CredentialModel GetCredentials(string target);
        NetworkCredential GetCredentials(string target);
    }
}
