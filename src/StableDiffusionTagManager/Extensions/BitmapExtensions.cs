using Avalonia.Media.Imaging;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using System.IO;
using System.Reactive.Joins;
using SixLabors.ImageSharp.PixelFormats;
using Avalonia;

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
    }
}
