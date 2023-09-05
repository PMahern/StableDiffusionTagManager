using Avalonia.Platform;
using System;

namespace StableDiffusionTagManager.Extensions
{
    public static class PixelFormatExtensions
    {
        public static int GetBytesPerPixel(this PixelFormat pixelFormat)
        {
            if(pixelFormat == PixelFormat.Rgb565)
            {
                return 2;
            }

            //PixelFormat.Rgba8888 and PixelFormat.Bgra8888
            return 4;
        }
    }
}
