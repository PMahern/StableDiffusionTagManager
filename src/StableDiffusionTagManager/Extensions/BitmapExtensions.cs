using Avalonia.Media.Imaging;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using System.IO;
using SixLabors.ImageSharp.PixelFormats;
using Avalonia;
using System.Collections.Generic;
using System;
using ImageUtil;
using System.Linq;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.Extensions
{
    public static class BitmapExtensions
    {

        public static Bitmap Copy(this Bitmap source)
        {
            using var mstream = new MemoryStream();
            source.Save(mstream);
            mstream.Position = 0;
            return new Bitmap(mstream);
        }

        public static Image<Rgba32> CreateSixLaborsRGBAImage(this Bitmap bitmap)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms);

            ms.Position = 0;
            return SixLabors.ImageSharp.Image.Load<Rgba32>(ms);
        }

        public static Image CreateSixLaborsImage(this Bitmap bitmap)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms);

            ms.Position = 0;
            return SixLabors.ImageSharp.Image.Load(ms);
        }

        public static Bitmap CreateBitmap(this Image image)
        {
            using var msBack = new MemoryStream();
            image.SaveAsPng(msBack);
            msBack.Position = 0;
            return new Bitmap(msBack);
        }

        public static Bitmap InvertColors(this Bitmap bitmap)
        {
            using var image = bitmap.CreateSixLaborsImage();

            image.Mutate(ctx => ctx.Invert());

            return image.CreateBitmap();
        }

        public static MaskResult ApplyMask(this Bitmap bitmap, Bitmap mask)
        {
            using var slMask = mask.CreateSixLaborsRGBAImage();
            using var slImage = bitmap.CreateSixLaborsRGBAImage();
            var alphaMaskedImage = new Image<Rgba32>(slImage.Width, slImage.Height);

            slMask.Mutate(x => x.Grayscale());

            int minX = alphaMaskedImage.Width;
            int minY = alphaMaskedImage.Height;
            int maxX = 0;
            int maxY = 0;

            // Combine the grayscale image with the alpha channel of the main image
            for (int y = 0; y < slImage.Height; y++)
            {
                for (int x = 0; x < slImage.Width; x++)
                {
                    Rgba32 mainPixel = slImage[x, y];
                    Rgba32 alphaPixel = slMask[x, y];

                    // Use the intensity of the alpha image as the alpha channel of the main image
                    mainPixel.A = alphaPixel.R; // Assuming the alpha image is in grayscale

                    alphaMaskedImage[x, y] = mainPixel;

                    if (alphaPixel.R > 0)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;
            var bounds = new Rectangle(minX, minY, width, height);
            Image<Rgba32> croppedAlphaMaskedImage = alphaMaskedImage.Clone(x => x.Crop(bounds));

            // Crop the mask to the bounding box
            Image<Rgba32> croppedMask = slMask.Clone(x => x.Crop(bounds));
            return new MaskResult
            {
                CroppedMask = croppedMask,
                CroppedMaskedImage = croppedAlphaMaskedImage,
                FullMaskedImage = alphaMaskedImage,
                Bounds = new CropBounds
                {
                    X = minX,
                    Y = minY,
                    Width = width,
                    Height = height
                }
            };
        }

        public static async Task<List<Bitmap>?> ExtractComicPanels(this Bitmap bitmap, List<RenderTargetBitmap>? layers = null)
        {
            string tmpImage = Path.Combine(App.GetTempFileDirectory(), $"{Guid.NewGuid()}.png");
            bitmap.Save(tmpImage);

            var appDir = App.GetAppDirectory();

            KumikoWrapper kwrapper = new KumikoWrapper(App.Settings.PythonPath, Path.Combine(new string[] { appDir, "Assets", "kumiko" }));
            var results = await kwrapper.GetImagePanels(tmpImage, App.GetTempFileDirectory());

            return results.Select(r => bitmap.CreateNewImageFromRegion(new Rect(r.TopLeftX, r.TopLeftY, r.Width, r.Height), null, layers)).ToList();
        }

        public static Bitmap CreateNewImageFromRegion(this Bitmap bitmap, Rect? region = null, PixelSize? targetSize = null, List<RenderTargetBitmap>? layers = null)
        {
            var finalRegion = region ?? new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height);
            var finalSize = targetSize ?? new PixelSize(Convert.ToInt32(finalRegion.Width), Convert.ToInt32(finalRegion.Height));
            var newImage = new RenderTargetBitmap(finalSize);
            using (var drawingContext = newImage.CreateDrawingContext())
            {
                drawingContext.DrawImage(bitmap, finalRegion, new Rect(0, 0, finalSize.Width, finalSize.Height));
                if (layers != null)
                {
                    foreach (var paintLayer in layers)
                    {
                        drawingContext.DrawImage(paintLayer, finalRegion, new Rect(0, 0, finalSize.Width, finalSize.Height));
                    }
                }
            }

            return newImage;
        }

        public static byte[] ToByteArray(this Bitmap image)
        {
            using var uploadStream = new MemoryStream();
            image.Save(uploadStream);
            return uploadStream.ToArray();
        }
    }
}
