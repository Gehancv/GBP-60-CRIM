using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBPackingSlipAttachmentService.Services
{
    public interface IIFSMediaAttachmentService
    {
        string GetEditableLuName();
        string GetLuNameMediaAware(string luName);
        string GetMediaFileExtList();
        string GetMediaItemSetEtag(string itemId);
        string CreateAndConnectMedia(string luName, string receiptSequence);
        void UploadMediaObject(string filePath);
    }
}
