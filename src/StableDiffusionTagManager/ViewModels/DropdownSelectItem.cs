using CommunityToolkit.Mvvm.ComponentModel;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class DropdownSelectItem : ViewModelBase
    {
        public DropdownSelectItem(string name)
        {
            this.name = name;
        }

        [ObservableProperty]
        private string name;
    }

    public class DropdownSelectItem<T> : DropdownSelectItem
    {
        public DropdownSelectItem(string name, T value) : base(name)
        {
            Value = value;
        }

        public T Value { get; }
    }
}
