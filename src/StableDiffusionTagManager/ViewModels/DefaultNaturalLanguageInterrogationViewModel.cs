using CommunityToolkit.Mvvm.ComponentModel;
using ImageUtil;
using System;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class DefaultNaturalLanguageInterrogationViewModel : InterrogatorViewModel<string>
    {
        [ObservableProperty]
        public string prompt = "Describe the image.";
        private readonly InterrogatorDescription<INaturalLanguageInterrogator> description;

        public override bool IsValid => throw new NotImplementedException();

        public DefaultNaturalLanguageInterrogationViewModel(InterrogatorDescription<INaturalLanguageInterrogator> description)
        {
            this.description = description;
            this.prompt = description.DefaultPrompt;
        }

        public override async Task<string> Interrogate(byte[] imageData, Action<string> updateCallBack, Action<string> consoleCallBack)
        {
            using var interrogator = description.Factory.Invoke();
            await interrogator.Initialize(updateCallBack, consoleCallBack);
            return await interrogator.CaptionImage(Prompt, imageData, consoleCallBack);
        }
    }
}
