using Avalonia.Controls;
using Avalonia.Input;
using StableDiffusionTagManager.ViewModels;

namespace StableDiffusionTagManager.Views
{
    public partial class ImageSegmentationDialog : Window
    {
        public ImageSegmentationViewModel ViewModel { get; } = new ImageSegmentationViewModel();

        public ImageSegmentationDialog()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        public void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
