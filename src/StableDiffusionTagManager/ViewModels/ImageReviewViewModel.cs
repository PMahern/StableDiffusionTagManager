using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.Views;
using System;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class ImageReviewActionViewModel : ViewModelBase
    {
        private ImageReviewDialog _parent;

        public ImageReviewActionViewModel(ImageReviewDialog parent)
        {
            _parent = parent;
        }

        public Func<Bitmap, Task>? Action { get; set; }

        public bool CanClick => _parent.SelectedImage != null;

        [RelayCommand(CanExecute = nameof(CanClick))]
        public async Task ActionClick(Bitmap selectedImage)
        {
            if(Action != null)
            {
                await Action.Invoke(selectedImage);
            }
        }

        public string Name { get; set; } = "";
    }

    public class ImageReviewViewModel : ViewModelBase
    {
        private bool isSelected = false;
        private Bitmap image;

        public ImageReviewViewModel(Bitmap image)
        {
            this.image = image;
        }

        public Bitmap Image
        {
            get => image; 
            set
            {
                SetProperty(ref image, value);
            }
        }

        public bool IsSelected
        {
            get => isSelected; 
            set
            {
                SetProperty(ref isSelected, value);
            }
        }
    }
}
