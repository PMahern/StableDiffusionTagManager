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

        public TagWithCountViewModel(MainWindowViewModel parent)
        {
            this.parent = parent;
        }

        [RelayCommand]
        public void AddTag()
        {
            parent.AddTagToEnd(Tag);
        }

        public string Tag { get; set; } = "";
        public int Count { get; set;  }
    }
}
