using CommunityToolkit.Mvvm.ComponentModel;

namespace StableDiffusionTagManager.ViewModels
{
    public class ViewModelBase : ObservableObject
    {
    }

    public abstract class ValidatedViewModel : ViewModelBase
    { 
        public abstract bool IsValid { get; }
    }
}