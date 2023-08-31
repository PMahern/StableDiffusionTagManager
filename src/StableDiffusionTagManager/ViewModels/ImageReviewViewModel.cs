using Avalonia.Media.Imaging;

namespace StableDiffusionTagManager.ViewModels
{
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
