using ImageProcessor;
using ImageProcessor.Imaging.Formats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageUploadAzureBlob.ImageEditing
{
    public interface IImageEditorManager
    {
        byte[] EditImage(byte[] photoBytes, ImageEditParameter imageEditParameter);
    }

    public class ImageEditorManager : IImageEditorManager
    {


        public ImageEditorManager()
        {
        }


        public byte[] EditImage(byte[] photoBytes, ImageEditParameter imageEditParameter)
        {
            ISupportedImageFormat format = new JpegFormat { Quality = imageEditParameter.ImageQality };
            Size size = new Size(imageEditParameter.ImageWidth, imageEditParameter.ImageHeight);
            byte[] photoBytesOut;
            using (MemoryStream inStream = new MemoryStream(photoBytes))
            {
                using (MemoryStream outStream = new MemoryStream())
                {
                    // Initialize the ImageFactory using the overload to preserve EXIF metadata.
                    using (ImageFactory imageFactory = new ImageFactory(preserveExifData: true))
                    {
                        if (imageEditParameter.OverrideWidthHeightWithOriginal)
                        {
                            // Keep orginal size, set the format and quality and save an image.
                            imageFactory.Load(inStream)
                                        .Format(format)
                                        .Save(outStream);
                        }
                        else
                        {
                            // Load, resize, set the format and quality and save an image.
                            imageFactory.Load(inStream)
                                        .Resize(size)
                                        .Format(format)
                                        .Save(outStream);
                        }


                        photoBytesOut = outStream.ToArray();
                        return photoBytesOut;
                    }
                    // Do something with the stream.
                }
            }
        }


    }

}
