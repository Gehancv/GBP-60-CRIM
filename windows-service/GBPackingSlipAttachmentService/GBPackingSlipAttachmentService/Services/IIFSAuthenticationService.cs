using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBPackingSlipAttachmentService.Services
{
    public interface IIFSAuthenticationService
    {
        string GetBearerToken();
    }
}
