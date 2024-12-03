using ImageUtil;
using System.Collections.Generic;

namespace StableDiffusionTagManager.ViewModels
{
    public interface ITaggerViewModelFactory
    {
        string Name { get; }
        InterrogatorViewModel<List<string>> CreateViewModel();
    }

    public class wdswinv2taggerv3ViewModelFactory : ITaggerViewModelFactory
    {
        public string Name => "SmilingWolf/wd-swinv2-tagger-v3";

        public InterrogatorViewModel<List<string>> CreateViewModel()
        {
            return new DefaultTagInterrogationViewModel(() => new SWTagger(Name));
        }
    }

    public class wdconvnexttaggerv3ViewModelFactory : ITaggerViewModelFactory
    {
        public string Name => "SmilingWolf/wd-convnext-tagger-v3";

        public InterrogatorViewModel<List<string>> CreateViewModel()
        {
            return new DefaultTagInterrogationViewModel(() => new SWTagger(Name));
        }
    }
    public class wdvittaggerv3ViewModelFactory : ITaggerViewModelFactory
    {
        public string Name => "SmilingWolf/wd-vit-tagger-v3";

        public InterrogatorViewModel<List<string>> CreateViewModel()
        {
            return new DefaultTagInterrogationViewModel(() => new SWTagger(Name));
        }
    }

    public class wdvitlargetaggerv3ViewModelFactory : ITaggerViewModelFactory
    {
        public string Name => "SmilingWolf/wd-vit-large-tagger-v3";

        public InterrogatorViewModel<List<string>> CreateViewModel()
        {
            return new DefaultTagInterrogationViewModel(() => new SWTagger(Name));
        }
    }

    public class wdeva02largetaggerv3ViewModelFactory : ITaggerViewModelFactory
    {
        public string Name => "SmilingWolf/wd-eva02-large-tagger-v3";

        public InterrogatorViewModel<List<string>> CreateViewModel()
        {
            return new DefaultTagInterrogationViewModel(() => new SWTagger(Name));
        }
    }

    public class wdv14moattaggerv2ViewModelFactory : ITaggerViewModelFactory
    {
        public string Name => "SmilingWolf/wd-v1-4-moat-tagger-v2";

        public InterrogatorViewModel<List<string>> CreateViewModel()
        {
            return new DefaultTagInterrogationViewModel(() => new SWTagger(Name));
        }
    }

    public class wdv14swinv2taggerv2ViewModelFactory : ITaggerViewModelFactory
    {
        public string Name => "SmilingWolf/wd-v1-4-swinv2-tagger-v2";

        public InterrogatorViewModel<List<string>> CreateViewModel()
        {
            return new DefaultTagInterrogationViewModel(() => new SWTagger(Name));
        }
    }

    public class wdv14convnexttaggerv2ViewModelFactory : ITaggerViewModelFactory
    {
        public string Name => "SmilingWolf/wd-v1-4-convnext-tagger-v2";

        public InterrogatorViewModel<List<string>> CreateViewModel()
        {
            return new DefaultTagInterrogationViewModel(() => new SWTagger(Name));
        }
    }

    public class wdv14convnextv2taggerv2ViewModelFactory : ITaggerViewModelFactory
    {
        public string Name => "SmilingWolf/wd-v1-4-convnextv2-tagger-v2";

        public InterrogatorViewModel<List<string>> CreateViewModel()
        {
            return new DefaultTagInterrogationViewModel(() => new SWTagger(Name));
        }
    }

    public class wdv14vittaggerv2ViewModelFactory : ITaggerViewModelFactory
    {
        public string Name => "SmilingWolf/wd-v1-4-vit-tagger-v2";

        public InterrogatorViewModel<List<string>> CreateViewModel()
        {
            return new DefaultTagInterrogationViewModel(() => new SWTagger(Name));
        }
    }
}
