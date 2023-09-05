using Avalonia.Media;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.Extensions
{
    public static class LockedFramebufferExtensions
    {
        public static Span<byte> GetPixels(this ILockedFramebuffer framebuffer)
        {
            unsafe
            {
                return new Span<byte>((byte*)framebuffer.Address, framebuffer.RowBytes * framebuffer.Size.Height);
            }
        }

        public static Span<byte> GetPixel(this ILockedFramebuffer framebuffer, int x, int y)
        {
            unsafe
            {
                var bytesPerPixel = framebuffer.Format.GetBytesPerPixel();
                var zero = (byte*)framebuffer.Address;
                var offset = framebuffer.RowBytes * y + bytesPerPixel * x;
                return new Span<byte>(zero + offset, bytesPerPixel);
            }
        }

        public static Color GetPixelColor(this ILockedFramebuffer framebuffer, int x, int y)
        {
            var pixel = framebuffer.GetPixel(x, y);

            var alpha = 0.0;

            if (framebuffer.Format == PixelFormat.Rgb565)
            {
                var value = pixel[0] << 8 | pixel[1];
                return new Color(255, (byte)((value >> 8) & 0b11111000), (byte)((value >> 3) & 0b11111100), (byte)((value << 3 & 0b11111000)));
            }
            else if (framebuffer.Format == PixelFormat.Rgba8888)
            {
                alpha = pixel[3] * 255.0;
                return new Color(pixel[3], (byte)(pixel[0] * alpha), (byte)(pixel[1] * alpha), (byte)(pixel[2] * alpha));
            }
            else if (framebuffer.Format == PixelFormat.Bgra8888)
            {
                alpha = pixel[3] * 255.0;
                return new Color(pixel[3], (byte)(pixel[2] * alpha), (byte)(pixel[1] * alpha), (byte)(pixel[0] * alpha));
            }
            throw new ArgumentOutOfRangeException();
        }


        public static void SetPixel(this ILockedFramebuffer framebuffer, int x, int y, Color color)
        {
            var pixel = framebuffer.GetPixel(x, y);

            var alpha = color.A / 255.0;

            if (framebuffer.Format == PixelFormat.Rgb565)
            {
                var value = (((color.R & 0b11111000) << 8) + ((color.G & 0b11111100) << 3) + (color.B >> 3));
                pixel[0] = (byte)value;
                pixel[1] = (byte)(value >> 8);
            }
            else if (framebuffer.Format == PixelFormat.Rgba8888)
            {
                pixel[0] = (byte)(color.R * alpha);
                pixel[1] = (byte)(color.G * alpha);
                pixel[2] = (byte)(color.B * alpha);
                pixel[3] = color.A;
            }
            else if (framebuffer.Format == PixelFormat.Bgra8888)
            {
                pixel[0] = (byte)(color.B * alpha);
                pixel[1] = (byte)(color.G * alpha);
                pixel[2] = (byte)(color.R * alpha);
                pixel[3] = color.A;
            } else
            {
                throw new ArgumentOutOfRangeException();
            }
        }
    }
}
