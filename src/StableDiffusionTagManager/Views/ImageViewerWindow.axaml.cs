using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using UVtools.AvaloniaControls;

namespace StableDiffusionTagManager.Views
{
    public partial class ImageViewerWindow : Window
    {
        public ImageViewerWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            Opened += ImageViewerWindow_Opened;
        }

        private void ImageViewerWindow_Opened(object? sender, System.EventArgs e)
        {
            if(Image != null)
            {
                ImageBox.ZoomToFit();
            }
        }

        public static readonly StyledProperty<Bitmap?> ImageProperty =
            AvaloniaProperty.Register<AdvancedImageBox, Bitmap?>(nameof(Image));

        /// <summary>
        /// Gets or sets the image to be displayed
        /// </summary>
        public Bitmap? Image
        {
            get => GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }
    }
}
