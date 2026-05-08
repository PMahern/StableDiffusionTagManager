using System.Linq;

namespace TagUtil
{
    public class FolderTagSets
    {
        public static List<string> GetConflicts(string path)
        {
            var jpegFilenames = Directory.EnumerateFiles(path, "*.jpg")
                .Select(f => Path.GetFileNameWithoutExtension(f)).ToHashSet();
            return Directory.EnumerateFiles(path, "*.png")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(png => jpegFilenames.Contains(png))
                .ToList();
        }

        public FolderTagSets(string path)
        {
            var jpegs = Directory.EnumerateFiles(path, "*.jpg").ToList();
            var pngs = Directory.EnumerateFiles(path, "*.png").ToList();
            var jpegFilenames = jpegs.Select(f => Path.GetFileNameWithoutExtension(f)).ToHashSet();
            var pngFilenames = pngs.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();

            var pairs = pngFilenames.Where(png => jpegFilenames.Contains(png)).ToList();
            if(pairs.Any()) {
                var aggregated = pairs.Aggregate((l, r) => $"{l}, {r}");
                throw new Exception($"There exists both a jpg and a png with the following filenames: {aggregated}.");
            }

            var jpegTagSets = jpegs.Select(jpeg => (ImageFile: jpeg, TagSet: new TagSet($"{path}/{Path.GetFileNameWithoutExtension(jpeg)}.txt")));
            var pngTagSets = pngs.Select(png => (ImageFile: png, TagSet: new TagSet($"{path}/{Path.GetFileNameWithoutExtension(png)}.txt")));

            //Load the tags
            TagsSets = jpegTagSets.Union(pngTagSets).ToList();
        }

        public List<(string ImageFile, TagSet TagSet)> TagsSets { get; set; }
    }
}
