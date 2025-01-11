using System.Collections.Generic;
using System.Xml.Linq;

namespace StableDiffusionTagManager.Models
{
    public class TagPrioritySet
    {
        public List<(string CategoryName, List<string> Tags)> Categories { get; } = new List<(string CategoryName, List<string> Tags)>();

        private Dictionary<string, int> tagPriorities = new Dictionary<string, int>();

        private List<(string tag, int index)> Wildcards = new List<(string, int)>();

        public static List<(string CategoryName, List<string> Tags)> ReadCategories(XDocument doc)
        {
            if(doc.Root == null || doc.Root.Name != "TagCategories")
            {
                throw new System.Exception("Invalid Tag Priority Set Format.");
            }

            return ReadCategories(doc.Root);
        }

        public static List<(string CategoryName, List<string> Tags)> ReadCategories(XElement categoriesNode)
        {
            var categories = new List<(string CategoryName, List<string> Tags)>();
            
            foreach (var tagCategory in categoriesNode.Descendants("TagCategory"))
            {
                var categoryName = tagCategory.Attribute("Name")?.Value;
                if(categoryName != null)
                {
                    var tags = new List<string>();
                    foreach (var tagElement in tagCategory.Descendants("Tag"))
                    {
                        var tag = tagElement.Value;
                        tags.Add(tag);
                    }
                    categories.Add((categoryName, tags));
                }
            }
            return categories;
        }

        public TagPrioritySet(string filename)
        {
            var doc = XDocument.Load(filename);
            var i = 0;

            Categories = ReadCategories(doc);

            foreach (var category in Categories)
            {
                foreach (var tag in category.Tags)
                {
                    if (tag.Contains('*'))
                    {
                        Wildcards.Add((tag, i));
                    }
                    else
                    {
                        tagPriorities[tag] = i;
                    }
                    ++i;
                }
            }
        }

        public int GetTagPriority(string tag)
        {
            int result = int.MaxValue;
            if (tagPriorities.ContainsKey(tag))
            {
                result = tagPriorities[tag];
            }

            foreach (var item in Wildcards)
            {
                if (item.index < result)
                {
                    //Test the wildcard
                    var wildcardindex = item.tag.IndexOf("*");
                    var doesMatch = (wildcardindex > 0 ? tag.StartsWith(item.tag.Substring(0, wildcardindex)) : true) &&
                                    ((wildcardindex < item.tag.Length - 1) ? tag.EndsWith(item.tag.Substring(wildcardindex + 1, item.tag.Length - wildcardindex - 1)) : true);
                    if (doesMatch)
                    {
                        return item.index;
                    }
                }
                else
                {
                    //We've gotten past the index in the standard priority list, just return it
                    return result;
                }
            }
            return result;
        }
    }
}
