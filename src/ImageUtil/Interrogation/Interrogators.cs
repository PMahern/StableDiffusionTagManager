using ImageUtil.Interrogation;
using System;
using System.Runtime.InteropServices;

namespace ImageUtil
{
    public class InterrogatorDescription<T>
    {
        public string Name { get; set; }
        public Func<T> Factory { get; set; }

        public string DefaultPrompt { get; set; }
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

        private static JoyCaptionAlphaOne? activeAlphaOneJoyCaptioner = null;

        private static JoyCaptionAlphaOne GetJoyCaptionAlphaOne()
        {
            if (activeAlphaOneJoyCaptioner == null || activeAlphaOneJoyCaptioner.Disposed)
            {
                activeAlphaOneJoyCaptioner = new JoyCaptionAlphaOne();
            }

            return activeAlphaOneJoyCaptioner;
        }

        static Interrogators()
        {
            TagInterrogators = SmilingWolfModels.Select(model => new InterrogatorDescription<ITagInterrogator>
            {
                Name = model,
                Factory = () => new SWTagger(model)
            }).ToList();

            TagInterrogators.Add(new InterrogatorDescription<ITagInterrogator>
            {
                Name = "Joy Caption Alpha One",
                Factory = GetJoyCaptionAlphaOne
            });

            NaturalLanguageInterrogators = new List<InterrogatorDescription<INaturalLanguageInterrogator>>
            {
                new InterrogatorDescription<INaturalLanguageInterrogator>
                {
                    Name = "Joy Caption Pre Alpha",
                    Factory = () => new JoyCaptionPreAlpha(),
                    DefaultPrompt = "A descriptive caption for this image:"
                },
                new InterrogatorDescription<INaturalLanguageInterrogator>
                {
                    Name = "Joy Caption Alpha One",
                    Factory = GetJoyCaptionAlphaOne,
                    DefaultPrompt = "Write a descriptive caption for this image in a formal tone."
                }
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                NaturalLanguageInterrogators.Add(new InterrogatorDescription<INaturalLanguageInterrogator>
                {
                    Name = "CogVLM2",
                    Factory = () => new CogVLM2Interrogator(),
                    DefaultPrompt = "Describe the image with as much detail as possible."
                });
            }
        }

        public static List<InterrogatorDescription<ITagInterrogator>> TagInterrogators;
        public static List<InterrogatorDescription<INaturalLanguageInterrogator>> NaturalLanguageInterrogators;
    }
}
