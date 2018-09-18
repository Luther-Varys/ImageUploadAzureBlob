using ImageUploadAzureBlob.BlobStorageConfig;
using ImageUploadAzureBlob.Helpers;
using ImageUploadAzureBlob.ImageEditing;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageUploadAzureBlob
{
    public class BlobStorageAzureManager
    {
        public BlobStorageSetting BlobStorageSetting { get; set; }


        private IImageEditorManager _ImageEditorManager { get; set; }
        private CloudStorageAccount _StorageAccount { get; set; }
        private CloudBlobContainer _CloudBlobContainer { get; set; }
        private CloudBlobClient _CloudBlobClient { get; set; }


        public BlobStorageAzureManager(BlobStorageSetting blobStorageSetting, IImageEditorManager imageEditorManager)
        {
            this.BlobStorageSetting = blobStorageSetting;
            this._ImageEditorManager = imageEditorManager;

            // Check whether the connection string can be parsed.
            CloudStorageAccount storageAccount;
            if (CloudStorageAccount.TryParse(this.BlobStorageSetting.ConnectionStringAzureStorageBlob, out storageAccount))
                this._StorageAccount = storageAccount;
            else
            {
                string errmsg = $@"A connection string has not been defined in the system environment variables. 
Add a environment variable named 'storageconnectionstring' with your storage connection string as a value.";

                Console.WriteLine(errmsg);
                throw new Exception("ZR: CloudStorageAccount.TryParse did not work. " + errmsg);

            }

            this._CloudBlobClient = this._StorageAccount.CreateCloudBlobClient();

            // Create a container if not exist. 
            this._CloudBlobContainer = this._CloudBlobClient.GetContainerReference(BlobStorageSetting.ContainerReferenceName);
            this._CloudBlobContainer.CreateIfNotExists();


            // Set the permissions so the blobs are public. 
            BlobContainerPermissions permissions = new BlobContainerPermissions
            {
                PublicAccess = this.BlobStorageSetting.BlobContainerPublicAccessTypeEnum
            };
            this._CloudBlobContainer.SetPermissionsAsync(permissions).GetAwaiter().GetResult();

        }



        public async Task<List<CloudBlockBlob>> UploadImageByFileAsync(string imageFilePath, List<ImageEditParameter> listImageEditParameter, string blobDirectory = null)
        {
            System.Drawing.Image img = System.Drawing.Image.FromFile(imageFilePath);
            //https://stackoverflow.com/questions/1625170/problem-with-png-images-in-c-sharp
            //img = new Bitmap(img);
            List<CloudBlockBlob> listCloudBlockBlobSaved = await this.UploadImageByImageAsync(img, listImageEditParameter, blobDirectory);
            return listCloudBlockBlobSaved;
        }

        public async Task<List<CloudBlockBlob>> UploadImageByImageAsync(System.Drawing.Image img, List<ImageEditParameter> listImageEditParameter, string blobDirectory = null)
        {
            byte[] arr;
            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                arr = ms.ToArray();

            }

            List<CloudBlockBlob> listCloudBlockBlobSaved = await this.UploadImageByBytesAsync(arr, listImageEditParameter, blobDirectory);
            return listCloudBlockBlobSaved;
        }

        public async Task<List<CloudBlockBlob>> UploadImageByBytesAsync(byte[] imageBytes, List<ImageEditParameter> listImageEditParameter, string blobDirectory = null)
        {
            List<CloudBlockBlob> listCloudBlockBlobSaved = await this._ProcessUploadImageBytesAsync(imageBytes, listImageEditParameter, blobDirectory);
            return listCloudBlockBlobSaved;
        }






        private async Task<List<CloudBlockBlob>> _ProcessUploadImageBytesAsync(byte[] arr, List<ImageEditParameter> listImageEditParameter, string blobDirectory = null)
        {

            List<CloudBlockBlob> listCloudBlockBlobSaved = new List<CloudBlockBlob>();
            string imageguid = ProjBasicHelper.GenrateGuidString();
            string timeStamp = ProjBasicHelper.GetTimestamp(DateTime.Now);

            try
            {
                Console.WriteLine("Created container '{0}'", this._CloudBlobContainer.Name);
                Console.WriteLine();



                //List<byte[]> listArrImageEdit = new List<byte[]>();

                foreach (ImageEditParameter imageEditParameter in listImageEditParameter)
                {
                    byte[] arrImageEdited = this._ImageEditorManager.EditImage(arr, imageEditParameter);

                    //listArrImageEdit.Add(arrImageEdit);


                    // Get a reference to the blob address, then upload the file to the blob.
                    // Use the value of localFileName for the blob name.
                    string imageFileNameGenerated = this.GenerateImageFileName(imageEditParameter, timeStamp);

                    CloudBlockBlob cloudBlockBlob;
                    if (blobDirectory != null)
                    {
                        string imageFileNameNoExt = $@"";

                        CloudBlobDirectory directory = this._CloudBlobContainer.GetDirectoryReference(blobDirectory);
                        cloudBlockBlob = directory.GetBlockBlobReference($@"{imageFileNameGenerated}.jpg");
                    }
                    else
                    {
                        cloudBlockBlob = this._CloudBlobContainer.GetBlockBlobReference($@"{imageFileNameGenerated}.jpg");
                    }
                    await cloudBlockBlob.UploadFromByteArrayAsync(arrImageEdited, 0, arrImageEdited.Length);

                    listCloudBlockBlobSaved.Add(cloudBlockBlob);

                    //Add image meta data .
                    await this._SetMetadataForCloudBlockBlob(cloudBlockBlob, imageEditParameter, imageguid, timeStamp, blobDirectory);
                }







                // List the blobs in the container.
                Console.WriteLine("Listing blobs in container.");
                BlobContinuationToken blobContinuationToken = null;
                do
                {
                    var results = await _CloudBlobContainer.ListBlobsSegmentedAsync(null, blobContinuationToken);
                    // Get the value of the continuation token returned by the listing call.
                    blobContinuationToken = results.ContinuationToken;
                    foreach (IListBlobItem item in results.Results)
                    {
                        Console.WriteLine(item.Uri);
                    }
                } while (blobContinuationToken != null); // Loop while the continuation token is not null.


                return listCloudBlockBlobSaved;
            }
            catch (StorageException ex)
            {
                Console.WriteLine("Error returned from the service: {0}", ex.Message);
                throw new Exception("zr in _ProcessUploadImageBytesAsync() " + ex.Message);

            }
            finally
            {

            }



        }

        private async Task _SetMetadataForCloudBlockBlob(CloudBlockBlob cloudBlockBlob, ImageEditParameter imageEditParameter, string imageguid, string timeStamp, string blobDirectory)
        {

            foreach (KeyValuePair<string, string> pair in imageEditParameter.BlobNameDefinition)
            {
                //Will lower the case of the key string, so that it can be easily handled
                string keyToLower = "BlobNameDefinition_".ToLower() + pair.Key.ToLower();

                if (ProjBasicHelper.IsStringNullOrEmptyOrWhitespace(pair.Value))
                    throw new Exception($"ZR ImageEditParameter.BlobNameDefinition with key {pair.Key} has no value. Value cannot be null or empty string.");
                else
                    await this.SaveMetaDataAsync(cloudBlockBlob, keyToLower, pair.Value);
            }


            foreach (KeyValuePair<string, string> pair in imageEditParameter.ImageMetaData)
            {
                //Will lower the case of the key string, so that it can be easily handled
                string keyToLower = pair.Key.ToLower();

                if (ProjBasicHelper.IsStringNullOrEmptyOrWhitespace(pair.Value))
                    throw new Exception($"ZR ImageEditParameter.ImageMetaData with key {pair.Key} has no value. Value cannot be null or empty string.");
                else
                    await this.SaveMetaDataAsync(cloudBlockBlob, keyToLower, pair.Value);
            }



            await this.SaveMetaDataAsync(cloudBlockBlob, "imageguid", imageguid);
            await this.SaveMetaDataAsync(cloudBlockBlob, "timestamp", timeStamp);
            await this.SaveMetaDataAsync(cloudBlockBlob, "blobdirectory", blobDirectory);
        }


        private string GenerateImageFileName(ImageEditParameter imageEditParameter, string timeStamp)
        {
            string imageFileName = string.Empty;


            foreach (KeyValuePair<string, string> pair in imageEditParameter.BlobNameDefinition)
            {
                if (ProjBasicHelper.IsStringNullOrEmptyOrWhitespace(pair.Value))
                    imageFileName = imageFileName + "_";
                else
                    imageFileName = imageFileName + $"_{pair.Value}";
            }


            if (ProjBasicHelper.IsStringNullOrEmptyOrWhitespace(timeStamp))
            {
                throw new Exception("ZR in GenerateImageFileName(): timeStamp is null or emty string. Timestamp must have a value.");
            }

            //Set --> timeStamp
            imageFileName = imageFileName + $"_{timeStamp}";



            return imageFileName;
        }

        public async Task DeleteBlobAsync(string blobFilename, string directoryReference = null)
        {
            if (directoryReference != null)
            {
                //ex: "TestDirectory/"
                var dir = this._CloudBlobContainer.GetDirectoryReference(directoryReference);
                //ex: "MytestImageNameNoExt__201809162214379758.jpg"
                var blobRef = dir.GetBlockBlobReference(blobFilename);
                // Delete the blob.
                await blobRef.DeleteIfExistsAsync();
            }
            else
            {
                var blobRef = this._CloudBlobContainer.GetBlockBlobReference(blobFilename);
                // Delete the blob.
                await blobRef.DeleteIfExistsAsync();
            }



        }


        public async Task DeleteBlobFolderAsync(string directoryReference)
        {
            //CloudStorageAccount storageAccount = CloudStorageAccount.Parse("your storage account");
            //CloudBlobContainer container = storageAccount.CreateCloudBlobClient().GetContainerReference("pictures");
            foreach (IListBlobItem blob in this._CloudBlobContainer.GetDirectoryReference(directoryReference).ListBlobs(true))
            {
                if (blob.GetType() == typeof(CloudBlob) || blob.GetType().BaseType == typeof(CloudBlob))
                {
                    await ((CloudBlob)blob).DeleteIfExistsAsync();
                }
            }

        }



        public async Task SaveMetaDataAsync(string blobFilename, string directoryReference, string key, string value)
        {
            //ex: "TestDirectory/"
            CloudBlobDirectory dir = this._CloudBlobContainer.GetDirectoryReference(directoryReference);
            //ex: "MytestImageNameNoExt__201809162214379758.jpg"
            CloudBlockBlob blobRef = dir.GetBlockBlobReference(blobFilename);

            blobRef.FetchAttributes();
            if (blobRef.Metadata.ContainsKey(key))
            {
                blobRef.Metadata[key] = value;
            }
            else
                blobRef.Metadata.Add(key, value);
            await blobRef.SetMetadataAsync();
        }

        public async Task SaveMetaDataAsync(CloudBlockBlob blobRef, string key, string value)
        {

            blobRef.FetchAttributes();
            if (blobRef.Metadata.ContainsKey(key))
            {
                blobRef.Metadata[key] = value;
            }
            else
                blobRef.Metadata.Add(key, value);
            await blobRef.SetMetadataAsync();
        }

        public async Task SaveMetaDataAsync(CloudBlockBlob blobRef, Dictionary<string, string> metaDataKeyValuePairs)
        {
            blobRef.FetchAttributes();
            foreach (string key in metaDataKeyValuePairs.Keys.ToList())
            {

                if (blobRef.Metadata.ContainsKey(key))
                {
                    blobRef.Metadata[key] = metaDataKeyValuePairs[key];
                }
                else
                    blobRef.Metadata.Add(key, metaDataKeyValuePairs[key]);
            }

            await blobRef.SetMetadataAsync();
        }



        

        public async Task<CloudBlockBlob> RenameBlobFileNameAsync(string fileNameOld, string fileNameNew, string directoryReference = null)
        {
            //https://azure.microsoft.com/en-us/resources/samples/storage-blobs-dotnet-rename-blob/

            /// <summary>  
            /// 1. Copy the file and name it with a new name  
            /// 2. Delete the old file  
            /// </summary> 
            //StorageCredentials cred = new StorageCredentials("[Your?storage?account?name]", "[Your?storage?account?key]");
            //CloudBlobContainer container = new CloudBlobContainer(new Uri("http://[Your?storage?account?name].blob.core.windows.net/[Your container name] /"), cred);

            //string fileName = "OldFileName";
            //string newFileName = "NewFileName";
            //await container.CreateIfNotExistsAsync();


            if (directoryReference == null)
            {
                directoryReference = "";
            }

            //ex: "TestDirectory/"
            CloudBlobDirectory dir = this._CloudBlobContainer.GetDirectoryReference(directoryReference);
            //ex: "MytestImageNameNoExt__201809162214379758.jpg"
            CloudBlockBlob blob_New = dir.GetBlockBlobReference(fileNameNew);

            if (!await blob_New.ExistsAsync())
            {
                CloudBlockBlob blob_Old = this._CloudBlobContainer
                    .GetDirectoryReference(directoryReference)
                    .GetBlockBlobReference(fileNameOld);
                if (await blob_Old.ExistsAsync())
                {
                    await blob_New.StartCopyAsync(blob_Old);
                    await blob_Old.DeleteIfExistsAsync();
                }
                else
                {
                    throw new Exception($"ZR in : there is no blob with name:{fileNameOld} in directory:{directoryReference} . Thus changing the name to {fileNameNew} is not possible.");
                }
            }

            //The blob with this name aready exists
            return blob_New;






            //CloudBlockBlob blobCopy = this._CloudBlobContainer.GetBlockBlobReference(fileNameNew);
            //if (!await blobCopy.ExistsAsync())
            //{
            //    CloudBlockBlob blob = this._CloudBlobContainer.GetBlockBlobReference(fileNameOld);

            //    if (await blob.ExistsAsync())
            //    {
            //        await blobCopy.StartCopyAsync(blob);
            //        await blob.DeleteIfExistsAsync();
            //    }
            //}
        }




    }
}
