using Avalonia.Media.Imaging;
using System.IO;

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
    }
}
