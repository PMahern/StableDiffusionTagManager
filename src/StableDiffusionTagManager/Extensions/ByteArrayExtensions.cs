using Avalonia.Media.Imaging;
using System.IO;

namespace StableDiffusionTagManager.Extensions
{
    internal static class ByteArrayExtensions
    {
        public static Bitmap ToBitmap(this byte[] byteArray)
        {
            using (MemoryStream memoryStream = new MemoryStream(byteArray))
            {
                return new Bitmap(memoryStream);
            }
        }
    }
}
