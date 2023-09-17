using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.Extensions;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class TagPrioritySetViewModel : ViewModelBase
    {
        public string? Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }

        public bool IsSubset { get => entries.Count > 0; }

        private ObservableCollection<TagPrioritySetViewModel> entries = new ObservableCollection<TagPrioritySetViewModel>();
        private string? name;

        public static TagPrioritySetViewModel CreateFromFile(string filename)
        {
            var lines = File.ReadLines(filename);
            var i = 0;
            var name = Path.GetFileNameWithoutExtension(filename);
            var viewModels = lines.Select(line =>
            {
                if (line.StartsWith("###"))
                {
                    var directory = Path.GetDirectoryName(filename);
                    var subsetname = Path.Combine(new string[] { directory, name, line.Substring(3).Trim() });
                    return CreateFromFile(subsetname);
                }
                else
                {
                    return new TagPrioritySetViewModel(line);
                }
            }).ToObservableCollection();

            return new TagPrioritySetViewModel(name)
            {
                entries = viewModels
            };
        }

        public TagPrioritySetViewModel(string name)
        {
            this.name = name;
        }

        public ReadOnlyObservableCollection<TagPrioritySetViewModel> Entries { get => new ReadOnlyObservableCollection<TagPrioritySetViewModel>(entries); }

        [RelayCommand]
        public void AddEntry()
        {
            entries.Add(new TagPrioritySetViewModel(""));
            OnPropertyChanged(nameof(IsSubset));
        }

        [RelayCommand]
        public void RemoveEntry(TagPrioritySetViewModel target)
        {
            entries.Remove(target);
            OnPropertyChanged(nameof(IsSubset));
        }

        public void Save(string directory)
        {
            var filename = Path.Combine(directory, $"{name}.txt");
            using var destFile = File.Create(filename);
            using var fw = new StreamWriter(destFile);
            foreach (var entry in entries)
            {

                if (entry.entries.Count > 0)
                {
                    fw.WriteLine($"### {entry.Name}.txt");
                    entry.Save(Path.Combine(directory, name));
                }
                else
                {
                    fw.WriteLine(entry.name);
                }
            }
        }

        internal void MovePrioritySet(TagPrioritySetViewModel vm, TagPrioritySetViewModel destvm)
        {
            if(vm == destvm) return;

            var index = entries.IndexOf(destvm);
            entries.Remove(vm);
            entries.Insert(index, vm);
        }
    }
}