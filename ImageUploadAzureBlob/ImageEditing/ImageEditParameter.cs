using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageUploadAzureBlob.ImageEditing
{

    public class ImageEditParameter
    {
        //Will be used to create the Blob/file NAME with the various segments ie: "_45_imagedata_user_9039_20180907201939.jpg"
        public Dictionary<string, string> BlobNameDefinition { get; private set; }
        
        //Will only be used in the "METADATA" of the blob. Will not be used for the file NAME definition. 
        public Dictionary<string, string> ImageMetaData { get; private set; }


        public int ImageWidth { get; private set; }
        public int ImageHeight { get; private set; }
        public int ImageQality { get; private set; }

        public bool OverrideWidthHeightWithOriginal { get; private set; }


        public ImageEditParameter(
            Dictionary<string, string> blobNameDefinition, 
            Dictionary<string, string> imageMetaData, 
            int imageWidth = 640, 
            int imageHeight = 480, 
            int imageQality = 70,
            bool overrideWidthHeightWithOriginal = false
            )
        {

            this.BlobNameDefinition = blobNameDefinition;
            this.ImageMetaData = imageMetaData;

            this.ImageWidth = imageWidth;
            this.ImageHeight = imageHeight;
            this.ImageQality = imageQality;

            this.OverrideWidthHeightWithOriginal = overrideWidthHeightWithOriginal;
        }







    }
}
