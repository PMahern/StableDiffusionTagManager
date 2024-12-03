using CommunityToolkit.Mvvm.ComponentModel;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class CheckBoxViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool isChecked = false;

        [ObservableProperty]
        private string text = string.Empty;
    }
}
