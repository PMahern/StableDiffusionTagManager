﻿using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System;
using System.IO;

namespace StableDiffusionTagManager.Extensions
{
    public enum Resampler : int
    {
        Bicubic,
        Box,
        CatmullRom,
        Hermite,
        Lanczos2,
        Lanczos3,
        Lanczos5,
        Lanczos8,
        MitchellNetravali,
        NearestNeighbor,
        Robidoux,
        RobidouxSharp,
        Spline,
        Triangle,
        Welch
    }
    
    public static class BitmapExtensions
    {
        public static IResampler ToResampler(this Resampler resampler) => resampler switch
        {
            Resampler.Bicubic => KnownResamplers.Bicubic,
            Resampler.Box => KnownResamplers.Box,
            Resampler.CatmullRom => KnownResamplers.CatmullRom,
            Resampler.Lanczos2 => KnownResamplers.Lanczos2,
            Resampler.Lanczos3 => KnownResamplers.Lanczos3,
            Resampler.Lanczos5 => KnownResamplers.Lanczos5,
            Resampler.Lanczos8 => KnownResamplers.Lanczos8,
            Resampler.MitchellNetravali => KnownResamplers.MitchellNetravali,
            Resampler.NearestNeighbor => KnownResamplers.NearestNeighbor,
            Resampler.Robidoux => KnownResamplers.Robidoux,
            Resampler.RobidouxSharp => KnownResamplers.RobidouxSharp,
            Resampler.Spline => KnownResamplers.Spline,
            Resampler.Triangle => KnownResamplers.Triangle,
            Resampler.Welch => KnownResamplers.Welch,
            _ => throw new ArgumentOutOfRangeException(nameof(resampler), $"Not expected resampler value: {resampler}"),
        };


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

        public static Bitmap CreateNewImageFromRegion(this Bitmap bitmap, Rect? region = null, PixelSize? targetSize = null, RenderTargetBitmap? paint = null, Resampler resampler = Resampler.Lanczos8)
        {
            var finalRegion = region ?? new Rect(0, 0, bitmap.PixelSize.Width, bitmap.PixelSize.Height);
            var finalSize = new PixelSize(Convert.ToInt32(finalRegion.Width), Convert.ToInt32(finalRegion.Height));
            var newImage = new RenderTargetBitmap(finalSize);
            using (var drawingContext = newImage.CreateDrawingContext())
            {
                using (drawingContext.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
                {
                    drawingContext.DrawImage(bitmap, finalRegion, new Rect(0, 0, finalSize.Width, finalSize.Height));
                    if (paint != null)
                    {
                        drawingContext.DrawImage(paint, finalRegion, new Rect(0, 0, finalSize.Width, finalSize.Height));
                    }
                }
            }
            
            if(targetSize.HasValue)
            {
                if(targetSize.Value.Width != finalSize.Width || targetSize.Value.Height != finalSize.Height)
                {
                    var sixLabors = newImage.CreateSixLaborsImage();

                    var rescaled = sixLabors.Clone(ctx => ctx.Resize(targetSize.Value.Width, targetSize.Value.Height, resampler.ToResampler()));
                    return rescaled.CreateBitmap();
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

        public static RenderTargetBitmap ToRenderTargetBitmap(this Bitmap bitmap)
        {
            var pixelSize = new PixelSize(bitmap.PixelSize.Width, bitmap.PixelSize.Height);
            var renderTargetBitmap = new RenderTargetBitmap(pixelSize);

            using (var drawingContext = renderTargetBitmap.CreateDrawingContext())
            {
                using (drawingContext.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
                {
                    drawingContext.DrawImage(bitmap, new Rect(0, 0, pixelSize.Width, pixelSize.Height));
                }
            }

            return renderTargetBitmap;
        }
        public static Bitmap ConvertMaskToAlpha(this Bitmap mask)
        {
            using var slMask = mask.CreateSixLaborsRGBAImage();
            var alphaMaskedImage = new Image<Rgba32>(slMask.Width, slMask.Height);

            // Convert the mask to alpha channel
            for (int y = 0; y < slMask.Height; y++)
            {
                for (int x = 0; x < slMask.Width; x++)
                {
                    Rgba32 maskPixel = slMask[x, y];

                    // Set the alpha channel to the intensity of the mask pixel
                    maskPixel.A = maskPixel.R;
                    maskPixel.R = 0;
                    maskPixel.G = 0;
                    maskPixel.B = 0;

                    alphaMaskedImage[x, y] = maskPixel;
                }
            }

            return alphaMaskedImage.CreateBitmap();
        }

        public static Bitmap ExpandMask(this Bitmap bitmap, int n)
        {
            if (n == 0)
            {
                return bitmap;
            }
            var mask = bitmap.CreateSixLaborsRGBAImage();
            int width = mask.Width;
            int height = mask.Height;
            // Perform edge detection
            var edgeMask = mask.Clone();
            edgeMask.Mutate(x => x.DetectEdges());
            var radius = Math.Abs(n);
            var radiussquared = radius & radius;
            var setColor = n > 0 ? new Rgba32(255, 255, 255, 255) : new Rgba32(0, 0, 0, 0);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Rgba32 pixel = mask[x, y];
                    Rgba32 edgePixel = edgeMask[x, y];

                    // Check if the pixel is near an edge
                    if (edgePixel.R > 0)
                    {
                        // Check neighboring pixels within n distance
                        for (int i = -radius; i <= radius; i++)
                        {
                            for (int j = -radius; j <= radius; j++)
                            {
                                int neighborX = x + i;
                                int neighborY = y + j;

                                if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
                                {
                                    if ((i * i + j * j) <= radiussquared) // Check if the pixel is within the circle
                                    {
                                        mask[neighborX, neighborY] = setColor;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return mask.CreateBitmap();
        }
    }
}
