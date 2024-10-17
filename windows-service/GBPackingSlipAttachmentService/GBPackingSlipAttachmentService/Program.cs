using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using GBPackingSlipAttachmentService.Utilities;
using Microsoft.Extensions.Configuration;
using GBPackingSlipAttachmentService.Services;
using System.Diagnostics;

namespace GBPackingSlipAttachmentService
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            try
            {
                var serviceProvider = ConfigurationService();
                var windowsService = serviceProvider.GetService<GBWindowsService>();

                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                windowsService
                };
                ServiceBase.Run(ServicesToRun);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("GBPackSlipSource", ex.ToString(), EventLogEntryType.Error);
            }
        } 

        private static ServiceProvider ConfigurationService()
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

            serviceCollection.AddSingleton<IConfiguration>(config);
            serviceCollection.AddSingleton<GBWindowsService>();
            serviceCollection.AddSingleton<ILogging, Logging>();
            serviceCollection.AddSingleton<RestHelper>();
            serviceCollection.AddSingleton<IIFSMediaAttachmentService, IFSMediaAttachmentService>();
            serviceCollection.AddSingleton<IIFSAuthenticationService, IFSAuthenticationService>();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
