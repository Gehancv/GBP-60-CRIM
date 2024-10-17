using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBPackingSlipAttachmentService.Utilities
{
    public interface ILogging
    {
        void LogInformation(string message);
        void LogError(string message, Exception ex = null);    
    }
}
