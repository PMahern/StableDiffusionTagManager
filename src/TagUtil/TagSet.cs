namespace TagUtil
{
    public class TagSet
    {
        public List<string> Tags { get; set; }

        public string File { get;  }

        public TagSet(string targetFile, IEnumerable<string> tags) 
        {
            File = targetFile;
            Tags = tags.ToList();
        }

        public TagSet(string file)
        {
            File = file;
            if(System.IO.File.Exists(file))
            {
                var content = System.IO.File.ReadAllText(file);
                Tags = content.Split(',').Select(s => s.Trim()).Distinct().Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            } else
            {
                Tags = new List<string>();
            }
        }

        public void WriteFile()
        {
            if(Tags != null && Tags.Count > 0)
            {
                System.IO.File.WriteAllText(File, Tags.Aggregate((l, r) => $"{l}, {r}"));
            }
        }
    }
}
