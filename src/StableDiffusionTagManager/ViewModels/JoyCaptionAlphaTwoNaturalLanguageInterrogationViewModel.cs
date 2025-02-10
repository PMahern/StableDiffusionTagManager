using CommunityToolkit.Mvvm.ComponentModel;
using ImageUtil.Interrogation;
using StableDiffusionTagManager.Extensions;
using System.Collections.ObjectModel;
using System.Linq;

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

        public override ConfiguredInterrogationContext<string> CreateInterrogationContext()
        {
            var interrogator = new JoyCaptionAlphaTwo();

            var args = new JoyCaptionAlphaTwoArgs
            {
                CaptionType = SelectedPrompt,
                Length = SelectedLength,
                ExtraOptions = ExtraOptions.Where(x => x.IsChecked).Any() ? ExtraOptions.Where(x => x.IsChecked).Select(x => x.Text).Aggregate((l, r) => $"{l},{r}") : "",
                NameInput = CharacterName
            };

            return new ConfiguredInterrogationContext<string>(interrogator, interrogator.Initialize, (imageData, updateCallback, consoleCallback) => interrogator.CaptionImage(args, imageData, consoleCallback));
        }

		public override bool IsValid => true;
    }
}
