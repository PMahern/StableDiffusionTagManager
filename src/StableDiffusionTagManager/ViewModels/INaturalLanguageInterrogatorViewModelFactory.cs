using ImageUtil;
using ImageUtil.Interrogation;
using StableDiffusionTagManager.Attributes;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace StableDiffusionTagManager.ViewModels
{
    public  interface INaturalLanguageInterrogatorViewModelFactory
    {
        string Name { get; }

        public InterrogatorViewModel<string> CreateViewModel();

        public List<OSPlatform> SupportedPlatforms => new List<OSPlatform> { OSPlatform.Windows, OSPlatform.Linux, OSPlatform.OSX };
    }

    [SupportedPlatforms("Linux")]
    public class CogVLM2InterrogatorViewModelFactory : INaturalLanguageInterrogatorViewModelFactory
    {
        public string Name => "CogVLM2";

        public InterrogatorViewModel<string> CreateViewModel()
        {
            return new DefaultNaturalLanguageInterrogationViewModel(() => new CogVLM2Interrogator(), "Describe the image with as much detail as possible.");
        }

        public List<OSPlatform> SupportedPlatforms => new List<OSPlatform> { OSPlatform.Linux };
    }


    public class JoyCaptionPreAlphaViewModelFactory : INaturalLanguageInterrogatorViewModelFactory
    {
        public string Name => "Joy Caption Pre-Alpha";

        public InterrogatorViewModel<string> CreateViewModel()
        {
            return new DefaultNaturalLanguageInterrogationViewModel(() => new JoyCaptionPreAlpha(), "A descriptive caption for this image:");
        }
    }

    public class JoyCaptionAlphaOneViewModelFactory : INaturalLanguageInterrogatorViewModelFactory
    {
        public string Name => "Joy Caption Alpha One";

        public InterrogatorViewModel<string> CreateViewModel()
        {
            return new DefaultNaturalLanguageInterrogationViewModel(() => new JoyCaptionAlphaOne(), "Write a descriptive caption for this image in a formal tone.");
        }
    }

    public class JoyCaptionAlphaTwoViewModelFactory : INaturalLanguageInterrogatorViewModelFactory
    {
        public string Name => "Joy Caption Alpha Two";

        public InterrogatorViewModel<string> CreateViewModel()
        {
            return new JoyCaptionAlphaTwoNaturalLanguageInterrogationViewModel();
        }
    }

    public class JoyCaptionBetaOneNaturalLanguageViewModelFactory : INaturalLanguageInterrogatorViewModelFactory
    {
        public string Name => "Joy Caption Beta One";

        public InterrogatorViewModel<string> CreateViewModel()
        {
            return new JoyCaptionBetaOneNaturalLanguageInterrogationViewModel();
        }
    }
}
