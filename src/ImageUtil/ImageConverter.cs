namespace ImageUtil
{
    public static class ImageConverter
    {
        public static void ConvertImageFileToPng(string filename)
        {
            var image = Image.Load(filename);

            var newFileName = Path.Combine(Path.GetDirectoryName(filename), $"{Path.GetFileNameWithoutExtension(filename)}.png");

            image.Save(newFileName);
        }
    }
}
