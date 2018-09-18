using System;
using System.Collections.Generic;
using System.IO;
using ImageUploadAzureBlob.BlobStorageConfig;
using ImageUploadAzureBlob.ImageEditing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;

namespace ImageUploadAzureBlob.Test
{
    [TestClass]
    public class UnitTest1
    {

        public string ConnectionStringAzureStorageBlob { get; set; }
        public string BaseTestProjectPath { get; set; }

        /// <summary>
        /// The connection string settings are found in the myJsonAppConfig.json file.
        /// The json file is located in the root folder of the current test project.
        /// 
        /// The json (myJsonAppConfig.json) file content will be similar to this:
        /// {
        ///     "ConnectionStringAzureStorageBlob": "...connectionstring that is found in the azure portal...",
        ///     "TestKey01": "TestValue01"
        /// }
        /// 
        /// If the json file (myJsonAppConfig.json) is not visible, just create it manually since it could have been excluded from
        /// the git repository for security reason by .gitignore listing.
        /// 
        /// 
        /// </summary>
        public UnitTest1()
        {
            this.ConnectionStringAzureStorageBlob = this._GetValueFromMyJsonAppConfig("ConnectionStringAzureStorageBlob");
            this.BaseTestProjectPath = @"..\..\";
        }


        /// <summary>
        /// 1) Create BlobStorageAzureManager, this is the main object that will be used to manage the images/files in azure blob storage.
        /// To instatiate a BlobStorageAzureManager object we need to pass 2 objects in the constructor:
        ///     -BlobStorageSetting (This is used to configure the BlobStorageAzureManager, we can add the conx. string and the blob container name)
        ///     -ImageEditorManager (This is used to edit the image size, filter, quality etc...)
        /// 
        /// 2) Once BlobStorageAzureManager is created we will upload an image file. We need to provide:
        ///     -imageFileToUpload_path (This is where the file/image to upload is locate in the local machine)
        ///     -imageNameNoExt (This is the name of the blob file that will be create. The name should not include the extension)
        ///     -blobDirectory (This is the blob directory in the blob container)
        /// 
        /// 3) When uploading an image in the azure blob storage we need to pass ImageEditParameter to the uploader.
        ///     -The ImageEditParameter is used to create multiple images from the uploaded image. i.e. when creating derived images with dirrent dimensions
        ///     -Thus when we need to create n images from the uploaded image we need to provide n ImageEditParameter
        ///     
        /// </summary>
        [TestMethod]
        public void BasicInitialization()
        {
            //*********************
            // Arrange 
            //*********************
            BlobStorageSetting blobStorageSetting = new BlobStorageSetting(this.ConnectionStringAzureStorageBlob);
            ImageEditorManager imageEditorManager = new ImageEditorManager();
            BlobStorageAzureManager blobStorageAzureManager = new BlobStorageAzureManager(blobStorageSetting, imageEditorManager);


            string imageFileToUpload_path = $@"{this.BaseTestProjectPath}TestImagesToUpload\image_01.jpg";
            string blobDirectory = "TestDirectory/1";

            Dictionary<string, string> blobNameDefinition_sqrmd = new Dictionary<string, string>();
            blobNameDefinition_sqrmd.Add("imagecat", "productxx");
            blobNameDefinition_sqrmd.Add("id", "64534");
            blobNameDefinition_sqrmd.Add("imgshape", "sqr");
            blobNameDefinition_sqrmd.Add("imgsize", "md");
            Dictionary<string, string> imageMetaDataDesscription_sqrmd = new Dictionary<string, string>();
            imageMetaDataDesscription_sqrmd.Add("metadata01", "metadatavalue01");
            imageMetaDataDesscription_sqrmd.Add("metadata02", "metadatavalue02");
            imageMetaDataDesscription_sqrmd.Add("metadata03", "metadatavalue03");


            Dictionary<string, string> blobNameDefinition_sqrsm = new Dictionary<string, string>();
            blobNameDefinition_sqrsm.Add("imagecat", "productxx");
            blobNameDefinition_sqrsm.Add("id", "64534");
            blobNameDefinition_sqrsm.Add("imgshape", "sqr");
            blobNameDefinition_sqrsm.Add("imgsize", "sm");
            Dictionary<string, string> imageMetaDataDesscription_sqrsm = new Dictionary<string, string>();
            imageMetaDataDesscription_sqrsm.Add("metadata01", "metadatavalue01");
            imageMetaDataDesscription_sqrsm.Add("metadata02", "metadatavalue02");
            imageMetaDataDesscription_sqrsm.Add("metadata03", "metadatavalue03");


            ImageEditParameter imageEditParameter_md = new ImageEditParameter(blobNameDefinition_sqrmd, imageMetaDataDesscription_sqrmd, 1280, 960, 60, overrideWidthHeightWithOriginal: true);
            ImageEditParameter imageEditParameter_sm = new ImageEditParameter(blobNameDefinition_sqrsm, imageMetaDataDesscription_sqrsm, 640, 480, 60);



            List<ImageEditParameter> listImageEditParameter = new List<ImageEditParameter>();
            listImageEditParameter.Add(imageEditParameter_md);
            listImageEditParameter.Add(imageEditParameter_sm);


            //*********************
            // Act
            //*********************
            List<CloudBlockBlob> listCloudBlockBlob = blobStorageAzureManager
                .UploadImageByFileAsync(imageFileToUpload_path, listImageEditParameter, blobDirectory)
                .GetAwaiter().GetResult();



            //*********************
            // Assert 
            //*********************
            foreach (CloudBlockBlob cloudBlockBlob in listCloudBlockBlob)
            {
                bool doesBlobExistInCloudStorage = cloudBlockBlob.Exists();
                Assert.IsTrue(doesBlobExistInCloudStorage);
            }


        }




        /// <summary>
        /// Will create 2 image blobs 
        /// Will then delete both
        /// Will check if the deleted blobs still exist in azure storage.
        /// </summary>
        [TestMethod]
        public void DeleteBlob()
        {
            //*********************
            // Arrange 
            //*********************
            BlobStorageSetting blobStorageSetting = new BlobStorageSetting(this.ConnectionStringAzureStorageBlob);
            ImageEditorManager imageEditorManager = new ImageEditorManager();
            BlobStorageAzureManager blobStorageAzureManager = new BlobStorageAzureManager(blobStorageSetting, imageEditorManager);


            string imageFileToUpload_path = $@"{this.BaseTestProjectPath}TestImagesToUpload\image_01.jpg";
            string blobDirectory = "TestDirectory/1";

            Dictionary<string, string> blobNameDefinition_sqrmd = new Dictionary<string, string>();
            blobNameDefinition_sqrmd.Add("imagecat", "product");
            blobNameDefinition_sqrmd.Add("id", "64534");
            blobNameDefinition_sqrmd.Add("imgshape", "sqr");
            blobNameDefinition_sqrmd.Add("imgsize", "md");
            Dictionary<string, string> imageMetaDataDesscription_sqrmd = new Dictionary<string, string>();
            imageMetaDataDesscription_sqrmd.Add("metadata01", "metadatavalue01");
            imageMetaDataDesscription_sqrmd.Add("metadata02", "metadatavalue02");
            imageMetaDataDesscription_sqrmd.Add("metadata03", "metadatavalue03");


            Dictionary<string, string> blobNameDefinition_sqrsm = new Dictionary<string, string>();
            blobNameDefinition_sqrsm.Add("imagecat", "product");
            blobNameDefinition_sqrsm.Add("id", "64534");
            blobNameDefinition_sqrsm.Add("imgshape", "sqr");
            blobNameDefinition_sqrsm.Add("imgsize", "sm");
            Dictionary<string, string> imageMetaDataDesscription_sqrsm = new Dictionary<string, string>();
            imageMetaDataDesscription_sqrsm.Add("metadata01", "metadatavalue01");
            imageMetaDataDesscription_sqrsm.Add("metadata02", "metadatavalue02");
            imageMetaDataDesscription_sqrsm.Add("metadata03", "metadatavalue03");


            ImageEditParameter imageEditParameter_md = new ImageEditParameter(blobNameDefinition_sqrmd, imageMetaDataDesscription_sqrmd, 1280, 960, 70);
            ImageEditParameter imageEditParameter_sm = new ImageEditParameter(blobNameDefinition_sqrsm, imageMetaDataDesscription_sqrsm, 640, 480, 70);



            List<ImageEditParameter> listImageEditParameter = new List<ImageEditParameter>();
            listImageEditParameter.Add(imageEditParameter_md);
            listImageEditParameter.Add(imageEditParameter_sm);


            //*********************
            // Act
            //*********************
            //Upload blobs
            List<CloudBlockBlob> listCloudBlockBlob = blobStorageAzureManager
                .UploadImageByFileAsync(imageFileToUpload_path, listImageEditParameter, blobDirectory)
                .GetAwaiter().GetResult();

            //Delete blobs
            foreach (CloudBlockBlob cloudBlockBlob in listCloudBlockBlob)
            {
                blobStorageAzureManager.DeleteBlobAsync(cloudBlockBlob.Name).GetAwaiter().GetResult();
            }


            //*********************
            // Assert 
            //*********************
            foreach (CloudBlockBlob cloudBlockBlob in listCloudBlockBlob)
            {
                bool doesBlobExistInCloudStorage = cloudBlockBlob.Exists();
                Assert.IsFalse(doesBlobExistInCloudStorage);
            }

        }


        /// <summary>
        /// Will create 2 image blobs 
        /// Will then delete both
        /// Will check if the deleted blobs still exist in azure storage.
        /// </summary>
        [TestMethod]
        public void ReadJsonAppConfigData()
        {
            //*********************
            // Arrange 
            //*********************
            string key_inJsonFile = "TestKey01";

            //*********************
            // Act 
            //*********************
            string TestKey01_value = this._GetValueFromMyJsonAppConfig(key_inJsonFile);

            //*********************
            // Assert 
            //*********************
            Assert.IsTrue(TestKey01_value == "TestValue01");


        }



        private string _GetValueFromMyJsonAppConfig(string key)
        {
            string myJsonAppConfigUrl = @"..\..\myJsonAppConfig.json";

            JObject jobj = JObject.Parse(File.ReadAllText(myJsonAppConfigUrl));
            string value = jobj[key].Value<string>();

            return value;

        }

    }
}
