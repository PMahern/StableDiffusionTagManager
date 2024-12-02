using StableDiffusionTagManager.Collections;

namespace StableDiffusionTagManager.ViewModels
{
    public  class TagCountLetterGroupViewModel
    {
        public TagCountLetterGroupViewModel(char letter)
        {
            Letter = letter;
        }

        public char Letter { get; }
        public OrderedSetObservableCollection<TagWithCountViewModel> TagCounts { get; set; } = new OrderedSetObservableCollection<TagWithCountViewModel>((l, r) => l.Tag.CompareTo(r.Tag));
    }
}
