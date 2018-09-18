using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageUploadAzureBlob.BlobStorageConfig
{
    public class BlobStorageSetting
    {
        public string ConnectionStringAzureStorageBlob { get; set; }
        public string ContainerReferenceName { get; set; }
        public BlobContainerPublicAccessType BlobContainerPublicAccessTypeEnum { get; set; }

        public BlobStorageSetting(
            string connectionStringAzureStorageBlob,
            string containerReferenceName = "imageblobs",
            BlobContainerPublicAccessType blobContainerPublicAccessTypeEnum = BlobContainerPublicAccessType.Blob
            )
        {
            this.ConnectionStringAzureStorageBlob = connectionStringAzureStorageBlob;
            //Set some default settings
            this.ContainerReferenceName = containerReferenceName;
            this.BlobContainerPublicAccessTypeEnum = blobContainerPublicAccessTypeEnum;
        }

    }


}
