using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StableDiffusionTagManager.Views
{
    public enum ImageReviewDialogMode
    {
        SingleSelect = 0,
        MultiSelect = 1
    }

    public partial class ImageReviewDialog : Window
    {
        public ImageReviewDialog()
        {
            InitializeComponent();

            this.DataContext = this;
        }

        public static readonly StyledProperty<ObservableCollection<ImageReviewViewModel>?> ImagesPropery =
            AvaloniaProperty.Register<ImageReviewDialog, ObservableCollection<ImageReviewViewModel>?>(nameof(Images), null);

        /// <summary>
        /// Gets or sets the images to review.
        /// </summary>
        public ObservableCollection<ImageReviewViewModel>? Images
        {
            get => GetValue(ImagesPropery);
            set => SetValue(ImagesPropery, value);
        }

        public static readonly StyledProperty<ImageReviewViewModel?> SelectedImageProperty =
            AvaloniaProperty.Register<ImageReviewDialog, ImageReviewViewModel?>(nameof(SelectedImage));

        /// <summary>
        /// Gets or sets the currently selected image of the dialog.
        /// </summary>
        public ImageReviewViewModel? SelectedImage
        {
            get => GetValue(SelectedImageProperty);
            set => SetValue(SelectedImageProperty, value);
        }

        public IEnumerable<Bitmap> SelectedImages { get => Images?.Where(i => i.IsSelected).Select(i => i.Image) ?? Enumerable.Empty<Bitmap>(); }

        public static readonly StyledProperty<ImageReviewDialogMode> ReviewModeProperty =
            AvaloniaProperty.Register<ImageReviewDialog, ImageReviewDialogMode>(nameof(ReviewMode), ImageReviewDialogMode.SingleSelect);

        /// <summary>
        /// Gets or sets the review behavior of the dialog.
        /// </summary>
        public ImageReviewDialogMode ReviewMode
        {
            get => GetValue(ReviewModeProperty);
            set => SetValue(ReviewModeProperty, value);
        }

        [RelayCommand]
        public void ChooseImage()
        {
            this.Success = true;
            Close();
        }

        [RelayCommand]
        public void Cancel()
        {
            Close();
        }

        /// <summary>
        /// Indiates if the user completed the dialog of cancelled out.
        /// </summary>
        public bool Success { get; set; } = false;
    }
}
