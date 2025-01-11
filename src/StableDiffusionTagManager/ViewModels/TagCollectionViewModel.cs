using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StableDiffusionTagManager.ViewModels
{

    public partial class TagCollectionViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel parent;

        public TagCollectionViewModel(
            MainWindowViewModel parent,
            string name,
            IEnumerable<string> tags)
        {
            this.parent = parent;
            Name = name;
            Tags = tags.Select(t => new TagCollectionTagViewModel(this, t)).ToObservableCollection();
        }

        public TagCollectionViewModel(
            MainWindowViewModel parent,
            string name)
        {
            this.parent = parent;
            Name = name;
            Tags = new ObservableCollection<TagCollectionTagViewModel>();
        }

        [ObservableProperty]
        public bool isSelected;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private ObservableCollection<TagCollectionTagViewModel> tags;

        [RelayCommand]
        public void AddTags()
        {
            parent.AddMissingTagsToEnd(Tags.Select(t => t.Tag).ToList());
        }

        [RelayCommand]
        public void EditTags()
        {
            parent.EditTagCollectionTarget = this;
        }

        [RelayCommand]
        public void RemoveTagCollection()
        {
            parent.RemoveTagCollection(this);
        }

        public void RemoveTag(TagCollectionTagViewModel toRemove)
        {
            this.Tags.Remove(toRemove);
        }

        public void AddEmptyTag()
        {
            this.Tags.Add(new TagCollectionTagViewModel(this, ""));
        }
    }

    public partial class TagCollectionTagViewModel : ViewModelBase
    {
        TagCollectionViewModel parent;

        [ObservableProperty]
        private string tag;

        public TagCollectionTagViewModel(TagCollectionViewModel parent, string tag)
        {
            this.parent = parent;
            this.Tag = tag;
        }

        [RelayCommand]
        public void RemoveTagFromCollection()
        {
            parent.RemoveTag(this);
        }
    }
}
