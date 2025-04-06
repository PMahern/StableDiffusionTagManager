using CommunityToolkit.Mvvm.ComponentModel;
using StableDiffusionTagManager.ViewModels;

namespace StableDiffusionTagManager.Views
{
    public partial class ImageResolution : ViewModelBase {

        partial void OnXChanged(int oldValue, int newValue)
        {
            _imageAspectRatio = null;
        }

        partial void OnYChanged(int oldValue, int newValue)
        {
            _imageAspectRatio = null;
        }

        [ObservableProperty]
        private int x;

        [ObservableProperty] 
        private int y;

        private double? _imageAspectRatio = null;
        public double ImageAspectRatio
        {
            get
            {
                if (_imageAspectRatio == null)
                {
                    _imageAspectRatio = _imageAspectRatio ?? (Y != 0 ? (double?)X / (double)Y : 0.0);
                }
                return _imageAspectRatio.Value;
            }
        }
    }
}
