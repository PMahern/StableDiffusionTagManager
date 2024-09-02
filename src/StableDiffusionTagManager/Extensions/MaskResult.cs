using SixLabors.ImageSharp;

namespace StableDiffusionTagManager.Extensions
{
    public class CropBounds
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
    public class MaskResult
    {
        
        public CropBounds Bounds { get; set; } = new CropBounds();
        public  Image FullMaskedImage { get; set; }
        public Image CroppedMaskedImage { get; set; }
        public Image CroppedMask { get; set; }
    }
}
