using CommunityToolkit.Mvvm.ComponentModel;
using StableDiffusionTagManager.Services;
namespace StableDiffusionTagManager.ViewModels
{
    public partial class YOLOModelSelectorDialogViewModel : ViewModelBase
    {
        public YOLOModelSelectorDialogViewModel(ViewModelFactory viewModelFactory)
        {
            selectorViewModel = viewModelFactory.CreateViewModel<YOLOModelSelectorViewModel>();
        }

        [ObservableProperty]
        private YOLOModelSelectorViewModel selectorViewModel;
    }
}
