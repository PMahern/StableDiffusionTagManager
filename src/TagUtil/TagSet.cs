using System.Text;

namespace TagUtil
{
    public class TagSet
    {
        public List<string> Tags { get; set; }
        public string? Description { get; set; }
        public string File { get;  }

        public TagSet(string targetFile, string? description, IEnumerable<string> tags) 
        {
            File = targetFile;
            Tags = tags.ToList();
            Description = description;
        }

        public TagSet(string file)
        {
            File = file;
            if (System.IO.File.Exists(file))
            {
                var content = System.IO.File.ReadAllText(file);

                var chunks = content.Split(Environment.NewLine);

                var descriptionBuilder = new StringBuilder();

                var IsLastChunk = (string[] chunks, int i) =>
                {
                    for (int j = i + 1; j < chunks.Length; j++)
                    {
                        if (!string.IsNullOrWhiteSpace(chunks[j]))
                        {
                            return false;
                        }
                    }
                    return true;
                };

                int i = 0;
                for (i = 0; !IsLastChunk(chunks, i); i++)
                {
                    descriptionBuilder.AppendLine(chunks[i]);
                }

                var lastChunk = chunks[i];
                var extractedTags = lastChunk.ExtractTags();
                // If the number of tags is greater than 10% of the length of the last chunk, we assume that the last chunk is the tags
                if (extractedTags.Count > (lastChunk.Length / 10))
                {
                    Tags = extractedTags;
                }
                else
                {
                    //Assume that the last chunk is more description.
                    descriptionBuilder.AppendLine(lastChunk);
                }

                Description = descriptionBuilder.ToString().Trim();
            }
            else
            {
                Tags = new List<string>();
            }
        }

        public void WriteFile()
        {
            var output = new StringBuilder();
            if (Description != null && (Tags?.Any() ?? false))
            {
                output.AppendLine(Description);
                output.AppendLine();
                output.AppendLine(Tags.Aggregate((l, r) => $"{l}, {r}"));
            }
            else if(Description != null)
            {
                output.AppendLine(Description);
            }
            else if(Tags?.Any() ?? false)
            {
                output.AppendLine(Tags.Aggregate((l, r) => $"{l}, {r}"));
            }
            if(output.Length > 0)
                System.IO.File.WriteAllText(File, output.ToString());
        }
    }
}
