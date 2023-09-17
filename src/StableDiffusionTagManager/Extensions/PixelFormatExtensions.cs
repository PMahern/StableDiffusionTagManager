using Avalonia.Platform;

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

            return 4;
        }
    }
}
