using CommunityToolkit.Mvvm.Input;
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

        private TagPrioritySetViewModel()
        {

        }
        public TagPrioritySetViewModel(string filename)
        {
            var lines = File.ReadLines(filename);
            var i = 0;
            Name = Path.GetFileNameWithoutExtension(filename);
            var viewModels = lines.Select(line =>
            {
                if (line.StartsWith("###"))
                {
                    var directory = Path.GetDirectoryName(filename);
                    var subsetname = Path.Combine(new string[] { directory, Name, line.Substring(3).Trim() });
                    return new TagPrioritySetViewModel(subsetname);
                }
                else
                {
                    return new TagPrioritySetViewModel() { name = line };
                }
            });

            entries = new ObservableCollection<TagPrioritySetViewModel>(viewModels);
        }

        public ReadOnlyObservableCollection<TagPrioritySetViewModel> Entries { get => new ReadOnlyObservableCollection<TagPrioritySetViewModel>(entries); }

        [RelayCommand]
        public void AddEntry()
        {
            entries.Add(new TagPrioritySetViewModel());
            OnPropertyChanged(nameof(IsSubset));
        }

        [RelayCommand]
        public void RemoveEntry(TagPrioritySetViewModel target)
        {
            entries.Remove(target);
            OnPropertyChanged(nameof(IsSubset));
        }
    }
}
