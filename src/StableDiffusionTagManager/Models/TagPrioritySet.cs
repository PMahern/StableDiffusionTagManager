using System;
using System.Collections.Generic;
using System.IO;

namespace StableDiffusionTagManager.Models
{
    public class TagPrioritySet
    {

        private Dictionary<string, int> tagPriorities = new Dictionary<string, int>();

        private List<(string tag, int index)> Wildcards = new List<(string, int)>();

        public TagPrioritySet(string filename)
        {
            var lines = File.ReadLines(filename);
            var i = 0;
            var subsetFoldername = Path.GetFileNameWithoutExtension(filename);
            foreach (var line in lines)
            {
                if (line.StartsWith("###"))
                {
                    //Load other file and process it
                    var directory = Path.GetDirectoryName(filename);
                    var subsetname = Path.Combine(new string[] { directory, subsetFoldername, line.Substring(3).Trim() });
                    var innerFileLines = File.ReadLines(subsetname);
                    foreach (var subline in innerFileLines)
                    {
                        if (subline.Contains('*'))
                        {
                            Wildcards.Add((subline, i));
                        }
                        else
                        {
                            tagPriorities[subline] = i;
                        }

                        ++i;
                    }
                }
                else
                {
                    if (line.Contains('*'))
                    {
                        Wildcards.Add((line, i));
                    }
                    else
                    {
                        tagPriorities[line] = i;
                    }
                }
                
                ++i;
            }
        }

        public int GetTagPriority(string tag)
        {
            int result = int.MaxValue;
            if(tagPriorities.ContainsKey(tag))
            {
                result = tagPriorities[tag];
            }


            foreach (var item in Wildcards)
            {
                if(item.index < result)
                {
                    //Test the wildcard
                    var wildcardindex = item.tag.IndexOf("*");
                    var doesMatch = (wildcardindex > 0 ? tag.StartsWith(item.tag.Substring(0, wildcardindex)) : true) &&
                                    (wildcardindex < item.tag.Length - 1) ? tag.EndsWith(item.tag.Substring(wildcardindex + 1, item.tag.Length - wildcardindex - 1)) : true;
                    if(doesMatch)
                    {
                        return item.index;
                    }
                } else 
                {
                    //We've gotten past the index in the standard priority list, just return it
                    return result;
                }
            }
            return result;
        }
    }
}
