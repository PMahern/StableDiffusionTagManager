using System.Collections.ObjectModel;

namespace StableDiffusionTagManager.ViewModels
{
    public  class TagCountLetterGroupViewModel
    {
        public char Letter { get; set; } = 'A';
        public ObservableCollection<TagWithCountViewModel> TagCounts { get; set; } = new ObservableCollection<TagWithCountViewModel>();
    }
}
