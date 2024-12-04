using CommunityToolkit.Mvvm.ComponentModel;
using ImageUtil;
using ImageUtil.Interrogation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class DefaultNaturalLanguageInterrogationViewModel : InterrogatorViewModel<string>
    {
        private readonly Func<INaturalLanguageInterrogator<string>> factory;
        [ObservableProperty]
        public string prompt = "Describe the image.";

        public override bool IsValid => throw new NotImplementedException();

        public DefaultNaturalLanguageInterrogationViewModel(Func<INaturalLanguageInterrogator<string>> factory, string? defaultPrompt = null)
        {
            this.factory = factory;
            this.prompt = defaultPrompt ?? "Describe the image.";
        }

        public override ConfiguredInterrogationContext<string> CreateInterrogationContext()
        {
            var interrogator = factory.Invoke();

            return new ConfiguredInterrogationContext<string>(interrogator, interrogator.Initialize, (imageData, updateCallback, consoleCallback) => interrogator.CaptionImage(Prompt, imageData, consoleCallback));
        }
    }
}
