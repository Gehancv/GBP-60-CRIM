using System;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using GBPackingSlipAttachmentService.Services;
using GBPackingSlipAttachmentService.Utilities;
using Microsoft.Extensions.Configuration;

namespace GBPackingSlipAttachmentService
{
    public partial class GBWindowsService : ServiceBase
    {
        private readonly ILogging _logger;
        private readonly IIFSMediaAttachmentService _mediaAttachmentService;
        private readonly IConfiguration _configuration;
        private string _downloadsFolder;
        private string _retryFolder;
        private TimeSpan _retryInterval;
        private CancellationTokenSource _cancellationTokenSource;
        private Thread _retryThread;
        private FileSystemWatcher _fileSystemWatcher;
        public GBWindowsService(IIFSMediaAttachmentService mediaAttachmentService, IConfiguration configuration, ILogging logger)
        {
            InitializeComponent();
            _mediaAttachmentService = mediaAttachmentService;
            _configuration = configuration;
            _logger = logger;
            _downloadsFolder = _configuration["Windows:POPackingSlipFolder"];
            _retryFolder = _configuration["Windows:POPackingSlipRetryFolder"];
            _retryInterval = TimeSpan.FromMinutes(Convert.ToDouble(_configuration["Windows:RetryIntervalMinutes"]));
            _cancellationTokenSource = new CancellationTokenSource();
        }

        protected override void OnStart(string[] args)
        {
            _logger.LogInformation("Service Starting...");
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;  // 100 seconds
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            try
            {
                CreateDirectories();
                StartFileWatcher();

                _retryThread = new Thread(async () => await RetryFailedUploads(_cancellationTokenSource.Token));
                _retryThread.Start();

                // Report service as running after the work is done
                serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
                SetServiceStatus(this.ServiceHandle, ref serviceStatus);

                _logger.LogInformation("Service Started Successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Service failed to start", ex);
                throw;
            }
        }


        protected override void OnStop()
        {
            _logger.LogInformation("Service Stopping...");


            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;  // 100 seconds
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            _fileSystemWatcher.Dispose();

            _cancellationTokenSource?.Cancel();
            if (_retryThread != null && _retryThread.IsAlive) 
            {
                _logger.LogInformation("Stopping the Retry Thread.");
                _retryThread.Join();
            }

            // Report service as stopped
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            _logger.LogInformation("Service Stopped.");

        }

        private void CreateDirectories() 
        {
            if (!Directory.Exists(_downloadsFolder))
            {
                Directory.CreateDirectory(_downloadsFolder);
            }

            if (!Directory.Exists(_retryFolder))
            {
                Directory.CreateDirectory(_retryFolder);
            }
        }

        private void StartFileWatcher()
        {
            _logger.LogInformation("Watching for Packing Slips...");
            _fileSystemWatcher = new FileSystemWatcher
            {
                Path = _downloadsFolder,
                Filter = "*.png",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            _fileSystemWatcher.Created += OnFileCreator;
        }

        private void OnFileCreator(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation($"WindowsService - OnFileCreator => New Packing Slip detected: {e.FullPath}");
            try
            {
                _logger.LogInformation($"WindowsService - OnFileCreator => Started uploading Packing Slip: {e.Name}");
                _mediaAttachmentService.UploadMediaObject(e.FullPath);
                //File.Delete(e.FullPath);
                _logger.LogInformation($"WindowsService - OnFileCreator => Finished uploading Packing Slip: {e.Name}");

            }
            catch (Exception ex)
            {
                _logger.LogError($"WindowsService - OnFileCreator => Failed uploading Packing Slip: {e.Name}", ex);
                MoveToRetryFolder(e.FullPath);
            }
        }

        private void MoveToRetryFolder(string filePath) 
        {
            var retryFilePath = Path.Combine(_retryFolder, Path.GetFileName(filePath));
            try
            {
                _logger.LogInformation($"GBWindowsService - MoveToRetryFolder => Moving file : {filePath} into {retryFilePath}");
                File.Move(filePath, retryFilePath);
            }
            catch (Exception ex) 
            {
                _logger.LogError($"GBWindowsService - MoveToRetryFolder => Error Moving file : {filePath} into {retryFilePath}", ex);
            }
        }

        private async Task RetryFailedUploads(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation($"GBWindowsService - MoveToRetryFolder => Checking for failed uploads in {_retryFolder}");
                    var filesToRetry = Directory.GetFiles(_retryFolder, "*.png");
                    foreach (var file in filesToRetry)
                    {
                        _logger.LogInformation($"GBWindowsService - RetryFailedUploads => Started uploading Packing Slip: {file}");
                        _mediaAttachmentService.UploadMediaObject(file);
                        //File.Delete(file);
                        _logger.LogInformation($"GBWindowsService - RetryFailedUploads => Finished uploading Packing Slip: {file}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"GBWindowsService - RetryFailedUploads => Failed uploading packing slips in {_retryFolder}", ex);
                }

                await Task.Delay(_retryInterval, token);
            }

        }

        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);
    }
}
