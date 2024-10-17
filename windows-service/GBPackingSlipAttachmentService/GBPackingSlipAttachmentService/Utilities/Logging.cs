using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBPackingSlipAttachmentService.Utilities
{
    public class Logging : ILogging
    {
        private readonly EventLog _eventLog;
        private readonly IConfiguration _configuration;

        public Logging(IConfiguration configuration)
        {
            _configuration = configuration;
            _eventLog = new EventLog();
            if (!EventLog.SourceExists(_configuration["Windows:EventLogSource"]))
            {
                EventLog.CreateEventSource(_configuration["Windows:EventLogSource"], _configuration["Windows:EventLog"]);
            }
            _eventLog.Source = _configuration["Windows:EventLogSource"];
            _eventLog.Log = "";
        }

        public void LogError(string message, Exception ex = null)
        {
            var errorMsg = ex != null ? $"{message}: {ex.Message}" : message;
            _eventLog.WriteEntry(errorMsg, EventLogEntryType.Error);
        }

        public void LogInformation(string message)
        {
            _eventLog.WriteEntry(message, EventLogEntryType.Information);
        }
    }
}
