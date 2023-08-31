using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.ViewModels
{

    public partial class TagCollectionViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel parent;

        public TagCollectionViewModel(
            MainWindowViewModel parent, 
            string name, 
            List<string> tags)
        {
            this.parent = parent;
            Name = name;
            Tags = tags.ToList();
        }

        public string Name { get; }
        public List<string> Tags { get; }

        [RelayCommand]
        public void AddTags()
        {
            parent.AddMissingTagsToEnd(Tags);
        }
    }
}
