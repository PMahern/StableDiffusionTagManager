using Avalonia.Metadata;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.Extensions;
using StableDiffusionTagManager.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class TagCategoryViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private ObservableCollection<string> tags = new ObservableCollection<string>();
    }

    public partial class TagPrioritySetViewModel : ViewModelBase
    {
        public static TagPrioritySetViewModel CreateFromFile(string filename)
        {
            var doc = XDocument.Load(filename);
            var i = 0;
            
            var categories = TagCategorySet.ReadCategories(doc);

            var viewModels = categories.Select(category =>
                new TagCategoryViewModel
                {
                    Name = category.CategoryName,
                    Tags = category.Tags.ToObservableCollection()
                }).ToObservableCollection();

            return new TagPrioritySetViewModel(filename)
            {
                entries = viewModels
            };
        }

        public TagPrioritySetViewModel(string name)
        {
            this.name = name;
        }

        [ObservableProperty]
        private string name;


        private ObservableCollection<TagCategoryViewModel> entries = new ObservableCollection<TagCategoryViewModel>();
        public ReadOnlyObservableCollection<TagCategoryViewModel> Entries { get => new ReadOnlyObservableCollection<TagCategoryViewModel>(entries); }

        [RelayCommand]
        public void AddCategory()
        {
            entries.Add(new TagCategoryViewModel());
        }

        [RelayCommand]
        public void RemoveCategory(TagCategoryViewModel target)
        {
            entries.Remove(target);
        }

        public void Save(string directory)
        {
            var filename = Path.Combine(directory, $"{name}.xml");
            var doc = new XDocument(
                entries.Select(entry =>
                    new XElement("TagCategory",
                        new XAttribute("Name", entry.Name),
                        entry.Tags.Select(tag => new XElement("Tag", tag))
                    )
                )
            );
            doc.Save(filename);
        }

        internal void MovePrioritySet(TagCategoryViewModel vm, TagCategoryViewModel destvm)
        {
            if (vm == destvm) return;

            var index = entries.IndexOf(destvm);
            entries.Remove(vm);
            entries.Insert(index, vm);
        }
    }
}