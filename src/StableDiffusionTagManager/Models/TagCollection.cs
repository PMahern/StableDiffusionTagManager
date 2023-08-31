using System.Collections.Generic;

namespace StableDiffusionTagManager.Models
{
    public class TagCollection
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
    }
}
