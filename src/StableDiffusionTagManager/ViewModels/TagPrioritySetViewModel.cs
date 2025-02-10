using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.Extensions;
using StableDiffusionTagManager.Models;
using System.Collections.ObjectModel;
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

        public void RemoveTag(string tag)
        {
            tags.Remove(tag);
        }
    }

    public partial class TagPrioritySetViewModel : ViewModelBase
    {
        public static TagPrioritySetViewModel CreateFromFile(string filename)
        {
            var doc = XDocument.Load(filename);
            
            var categories = TagCategorySet.ReadCategories(doc);

            var viewModels = categories.Select(category =>
                new TagCategoryViewModel
                {
                    Name = category.CategoryName,
                    Tags = category.Tags.ToObservableCollection()
                }).ToObservableCollection();

            return new TagPrioritySetViewModel()
            {
                categories = viewModels
            };
        }

        public TagPrioritySetViewModel()
        {
        }


        [ObservableProperty]
        private ObservableCollection<TagCategoryViewModel> categories = new ObservableCollection<TagCategoryViewModel>();

        [RelayCommand]
        public void AddCategory(string name)
        {
            categories.Add(new TagCategoryViewModel() {  Name = name });
        }

        [RelayCommand]
        public void RemoveCategory(TagCategoryViewModel target)
        {
            categories.Remove(target);
        }

        public void Save(string filename)
        {
            var doc = new XDocument(
                categories.Select(entry =>
                    new XElement("TagCategory",
                        new XAttribute("Name", entry.Name),
                        entry.Tags.Select(tag => new XElement("Tag", tag))
                    )
                )
            );
            doc.Save(filename);
        }

        internal void MoveTagCategory(TagCategoryViewModel vm, TagCategoryViewModel destvm)
        {
            if (vm == destvm) return;

            var index = categories.IndexOf(destvm);
            categories.Remove(vm);
            categories.Insert(index, vm);
        }
    }
}