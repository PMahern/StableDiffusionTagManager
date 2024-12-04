using System.Text;
using System.Text.RegularExpressions;

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

                if (content != null)
                {
                    content = content.Trim();

                    var chunks = Regex.Split(content, @"\r\n|\n");

                    var descriptionBuilder = new StringBuilder();

                    foreach (var chunk in chunks.Take(chunks.Length - 1))
                    {
                        descriptionBuilder.AppendLine(chunk);
                    }

                    var lastChunk = chunks.Last();
                    var extractedTags = lastChunk.ExtractTags();
                    // If there's more than one comma per 25 characters, no periods, and no upper case letters assume that the last chunk is tags.
                    if (extractedTags.Count > (lastChunk.Length / 25) && !lastChunk.Contains(".") && !lastChunk.Any(c => char.IsUpper(c)))
                    {
                        Tags = extractedTags;
                    }
                    else
                    {
                        //Assume that the last chunk is more description.
                        descriptionBuilder.AppendLine(lastChunk);
                        Tags = new List<string>();
                    }

                    Description = descriptionBuilder.ToString().Trim();
                }
            }
            else
            {
                Tags = new List<string>();
            }
        }

        public void WriteFile()
        {
            var output = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(Description) && (Tags?.Any() ?? false))
            {
                output.AppendLine(Description);
                output.AppendLine();
                output.AppendLine(Tags.Aggregate((l, r) => $"{l}, {r}"));
            }
            else if (!string.IsNullOrWhiteSpace(Description))
            {
                output.AppendLine(Description);
            }
            else if (Tags?.Any() ?? false)
            {
                output.AppendLine(Tags.Aggregate((l, r) => $"{l}, {r}"));
            }
            if (output.Length > 0)
                System.IO.File.WriteAllText(File, output.ToString());
        }
    }
}
