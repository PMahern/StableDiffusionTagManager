using CommunityToolkit.Mvvm.Input;
using HarfBuzzSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class TagWithCountViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel parent;
        private string tag = "";
        private int count;

        public TagWithCountViewModel(MainWindowViewModel parent)
        {
            this.parent = parent;
        }

        [RelayCommand]
        public void AddTag()
        {
            parent.AddTagToEnd(Tag);
        }

        public string Tag { get => tag; set => SetProperty(ref tag, value); }
        public int Count { get => count; set => SetProperty(ref count, value); }
    }
}
