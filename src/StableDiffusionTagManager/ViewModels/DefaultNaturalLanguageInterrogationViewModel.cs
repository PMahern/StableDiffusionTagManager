using CommunityToolkit.Mvvm.ComponentModel;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class DefaultNaturalLanguageInterrogationViewModel : ViewModelBase
    {
        [ObservableProperty]
        public string prompt = "Describe the image.";
    }
}
