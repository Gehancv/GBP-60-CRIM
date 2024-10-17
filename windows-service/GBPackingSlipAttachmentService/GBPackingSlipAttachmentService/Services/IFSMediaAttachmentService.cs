using GBPackingSlipAttachmentService.Utilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GBPackingSlipAttachmentService.Services
{
    public class IFSMediaAttachmentService : IIFSMediaAttachmentService
    {
        private readonly ILogging _logger;
        private readonly IConfiguration _configuration;
        private readonly RestHelper _restHelper;

        public IFSMediaAttachmentService(RestHelper restHelper, IConfiguration configuration, ILogging logger)
        {
            _configuration = configuration;
            _logger = logger;
            _restHelper = restHelper;
        }

        public string CreateAndConnectMedia(string luName, string receiptSequence)
        {
            var projection = "MediaLibraryAttachmentHandling.svc";
            var method = "CreateAndConnectMedia";

            var uri = _restHelper.CreateRequestUri($"{projection}/{method}");

            var content = new
            {
                LuName = luName,
                KeyRef = $"RECEIPT_SEQUENCE={receiptSequence}^",
                Name = receiptSequence,
                Description = (string) null
            };

            var jsonBody = JsonConvert.SerializeObject(content);

            var response = _restHelper.PostAsync(uri, jsonBody).Result;

            return response["ItemId"];
        }

        public string GetEditableLuName()
        {
            var projection = "MediaLibraryAttachmentHandling.svc";
            var method = $"GetEditableLuName(TargetLuName='{_configuration["Ifs:MediaInfo:TargetLuName"]}',Service='{_configuration["Ifs:MediaInfo:Service"]}')";

            var uri = _restHelper.CreateRequestUri($"{projection}/{method}");
            var response = _restHelper.GetAsync(uri).Result;
            return response["value"];
        }

        public string GetLuNameMediaAware(string luName)
        {
            var projection = "MediaLibraryAttachmentHandling.svc";
            var method = $"GetLuNameMediaAware(LuName='{luName}')";

            var uri = _restHelper.CreateRequestUri($"{projection}/{method}");
            var response = _restHelper.GetAsync(uri).Result;
            return response["value"];
        }

        public string GetMediaFileExtList()
        {
            var projection = "MediaLibraryAttachmentHandling.svc";
            var method = "GetMediaFileExtList()";

            var uri = _restHelper.CreateRequestUri($"{projection}/{method}");
            var response = _restHelper.GetAsync(uri).Result;
            return response["value"];
        }

        public string GetMediaItemSetEtag(string itemId)
        {
            var projection = "MediaLibraryAttachmentHandling.svc";
            var method = $"MediaItemSet(ItemId={itemId})?$select=MediaObject";

            var uri = _restHelper.CreateRequestUri($"{projection}/{method}");
            var response = _restHelper.GetAsync(uri).Result;
            return response["@odata.etag"];
        }
    
        public void UploadMediaObject(string filePath)
        {
            var luName = GetEditableLuName();
            var isEnabled = GetLuNameMediaAware(luName);

            if (isEnabled == "TRUE")
            {
                var validExtensions = GetMediaFileExtList();
                var fileExt = Path.GetExtension(filePath);
                var isFileExtExists = validExtensions.Contains(fileExt);

                if (isFileExtExists)
                {
                    var receiptSequence = Path.GetFileNameWithoutExtension(filePath);
                    var itemId = CreateAndConnectMedia(luName, receiptSequence);
                    var etag = GetMediaItemSetEtag(itemId);

                    var projection = "MediaLibraryAttachmentHandling.svc";
                    var method = $"MediaItemSet(ItemId={itemId})/MediaObject";

                    var uri = _restHelper.CreateRequestUri($"{projection}/{method}");
                    var response = _restHelper.PatchAsync(uri, etag, itemId, filePath).Result;
                    _logger.LogInformation($"IFSMediaAttachmentService - UploadMediaObject => Packing Slip attached to receipt {receiptSequence} successfully.");
                }
                else
                {
                    _logger.LogError($"IFSMediaAttachmentService - UploadMediaObject => {fileExt} is not a valid File Extensions type in IFS");
                }
            }
            else
            {
                _logger.LogError($"IFSMediaAttachmentService - UploadMediaObject => Media attachment is not allowed for {luName}");
            }

        }
    }
}
