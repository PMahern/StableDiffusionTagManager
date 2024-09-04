﻿namespace ImageUtil
{
    public class InterrogatorDescription<T>
    {
        public string Name { get; set; }
        public Func<T> Factory { get; set; }
    }

    public static class Interrogators
    {
        public static List<string> SmilingWolfModels = new List<string>
        {
            "SmilingWolf/wd-swinv2-tagger-v3",
            "SmilingWolf/wd-convnext-tagger-v3",
            "SmilingWolf/wd-vit-tagger-v3",
            "SmilingWolf/wd-vit-large-tagger-v3",
            "SmilingWolf/wd-eva02-large-tagger-v3",
            "SmilingWolf/wd-v1-4-moat-tagger-v2",
            "SmilingWolf/wd-v1-4-swinv2-tagger-v2",
            "SmilingWolf/wd-v1-4-convnext-tagger-v2",
            "SmilingWolf/wd-v1-4-convnextv2-tagger-v2",
            "SmilingWolf/wd-v1-4-vit-tagger-v2"
        };

        static Interrogators()
        {
            TagInterrogators = SmilingWolfModels.Select(model => new InterrogatorDescription<ITagInterrogator>
            {
                Name = model,
                Factory = () => new SWTagger(model)
            }).ToList();

            NaturalLanguageInterrogators = new List<InterrogatorDescription<INaturalLanguageInterrogator>>
            {
                new InterrogatorDescription<INaturalLanguageInterrogator>
                {
                    Name = "Joy Caption",
                    Factory = () => new JoyCaptioner()
                }
            };
        }

        public static List<InterrogatorDescription<ITagInterrogator>> TagInterrogators;
        public static List<InterrogatorDescription<INaturalLanguageInterrogator>> NaturalLanguageInterrogators;
    }
}
