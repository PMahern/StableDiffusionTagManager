using CommunityToolkit.Mvvm.ComponentModel;
using ImageUtil;
using ImageUtil.Interrogation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class DefaultTagInterrogationViewModel : InterrogatorViewModel<List<string>>
    {
        private readonly Func<ITagInterrogator<float>> interrogatorFactory;

        public DefaultTagInterrogationViewModel(Func<ITagInterrogator<float>> interrogatorFactory)
        {
            this.interrogatorFactory = interrogatorFactory;
        }

        public override ConfiguredInterrogationContext<List<string>> CreateInterrogationContext()
        {
            var interrogator = interrogatorFactory.Invoke();

            return new ConfiguredInterrogationContext<List<string>>(interrogator, interrogator.Initialize, (imageData, updateCallback, consoleCallback) => interrogator.TagImage(Threshold, imageData, consoleCallback));
        }

        [ObservableProperty]
        public float threshold = 0.5f;
        

        public override bool IsValid => Threshold >= 0 && Threshold <= 1;
    }
}
