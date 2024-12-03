using CommunityToolkit.Mvvm.ComponentModel;
using ImageUtil;
using System;
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

        public override async Task<string> Interrogate(byte[] imageData, Action<string> updateCallBack, Action<string> consoleCallBack)
        {
            using var interrogator = factory.Invoke();
            await interrogator.Initialize(updateCallBack, consoleCallBack);
            return await interrogator.CaptionImage(Prompt, imageData, consoleCallBack);
        }
    }
}
