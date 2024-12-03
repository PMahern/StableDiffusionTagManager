using CommunityToolkit.Mvvm.ComponentModel;
using ImageUtil.Interrogation;
using StableDiffusionTagManager.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class JoyCaptionAlphaTwoNaturalLanguageInterrogationViewModel : InterrogatorViewModel<string>
    {
        public JoyCaptionAlphaTwoNaturalLanguageInterrogationViewModel()
        {
            extraOptions = JoyCaptionAlphaTwo.ExtraOptionText.Select(text => new CheckBoxViewModel { Text = text }).ToObservableCollection();
        }

        [ObservableProperty]
        private ObservableCollection<CheckBoxViewModel> extraOptions;

        [ObservableProperty]
        private string selectedPrompt;

        [ObservableProperty]
        private string selectedLength;

        [ObservableProperty]
        private string characterName;

        [ObservableProperty]
        private string customPrompt;

        public async override Task<string> Interrogate(byte[] imageData, Action<string> updateCallBack, Action<string> consoleCallBack)
        {
            var interrogator = new JoyCaptionAlphaTwo();
            await interrogator.Initialize(updateCallBack, consoleCallBack);
            var args = new JoyCaptionAlphaTwoArgs
            {
                CaptionType = SelectedPrompt,
                Length = SelectedLength,
                ExtraOptions = ExtraOptions.Where(x => x.IsChecked).Any() ? ExtraOptions.Where(x => x.IsChecked).Select(x => x.Text).Aggregate((l, r) => $"{l},{r}") : "",
                NameInput = CharacterName
            };
            return await interrogator.CaptionImage(args, imageData, consoleCallBack);
        }

		public override bool IsValid => true;
    }
}
