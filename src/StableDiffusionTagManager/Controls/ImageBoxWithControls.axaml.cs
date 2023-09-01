using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using ImageUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static UVtools.AvaloniaControls.AdvancedImageBox;

namespace StableDiffusionTagManager.Controls
{
    public enum ImageViewerMode
    {
        Pan,
        Selection,
        Paint,
        Mask
    }

    public partial class ImageBoxWithControls : UserControl, INotifyPropertyChanged
    {
        public ImageBoxWithControls()
        {
            InitializeComponent();

            PaintBrushColor = new Color(255, 255, 255, 255);
        }

        public event EventHandler<Bitmap>? ImageCropped;
        public Func<Bitmap, Task> SaveClicked;
        public Func<Bitmap, Task>? InterrogateClicked;
        public Func<Bitmap, Task>? EditImageClicked;
        public Func<List<Bitmap?>, Task>? ComicPanelsExtracted;


        private (int x, int y) _lockedSize;
        private bool isAspectRatioLocked = false;
        public bool IsAspectRatioLocked
        {
            get => isAspectRatioLocked;
            set
            {
                if (RaiseAndSetIfChanged(ref isAspectRatioLocked, value))
                {
                    if (value)
                    {
                        _lockedSize = ((int)SelectionRegion.Width, (int)SelectionRegion.Height);
                    }
                }
            }
        }

        public static readonly StyledProperty<bool> ShowEditImageButtonProperty =
            AvaloniaProperty.Register<ImageBoxWithControls, bool>(nameof(ShowEditImageButton), true);

        public bool ShowEditImageButton
        {
            get => GetValue(ShowEditImageButtonProperty);
            set => SetValue(ShowEditImageButtonProperty, value);
        }

        public static readonly StyledProperty<bool> ShowSaveButtonProperty =
            AvaloniaProperty.Register<ImageBoxWithControls, bool>(nameof(ShowSaveButton), false);

        public bool ShowSaveButton
        {
            get => GetValue(ShowSaveButtonProperty);
            set => SetValue(ShowSaveButtonProperty, value);
        }

        public static readonly StyledProperty<bool> ShowMaskModeButtonProperty =
            AvaloniaProperty.Register<ImageBoxWithControls, bool>(nameof(ShowMaskModeButton), true);

        public bool ShowMaskModeButton
        {
            get => GetValue(ShowMaskModeButtonProperty);
            set => SetValue(ShowMaskModeButtonProperty, value);
        }

        public static readonly StyledProperty<bool> ShowExtractComicPanelsButtonProperty =
            AvaloniaProperty.Register<ImageBoxWithControls, bool>(nameof(ShowExtractComicPanelsButton), true);

        public bool ShowExtractComicPanelsButton
        {
            get => GetValue(ShowExtractComicPanelsButtonProperty);
            set => SetValue(ShowExtractComicPanelsButtonProperty, value);
        }

        public static readonly StyledProperty<bool> ShowInterrogateButtonProperty =
            AvaloniaProperty.Register<ImageBoxWithControls, bool>(nameof(ShowInterrogateButton), true);

        public bool ShowInterrogateButton
        {
            get => GetValue(ShowInterrogateButtonProperty);
            set => SetValue(ShowInterrogateButtonProperty, value);
        }

        public static readonly StyledProperty<Bitmap?> ImageProperty =
            AvaloniaProperty.Register<ImageBoxWithControls, Bitmap?>(nameof(Image), null, notifying: (control, before) => ImageChanged((ImageBoxWithControls)control, before));

        public static void ImageChanged(ImageBoxWithControls control, bool before)
        {
            if (!before)
            {
                control.UpdateSelectionRegion();
                control.ImageBox.Image = control.Image;
            }
        }
        public Bitmap? Image
        {
            get => GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        public static readonly StyledProperty<PixelSize?> TargetImageSizeProperty =
            AvaloniaProperty.Register<ImageBoxWithControls, PixelSize?>(nameof(TargetImageSize), null);

        public PixelSize? TargetImageSize
        {
            get => GetValue(TargetImageSizeProperty);
            set => SetValue(TargetImageSizeProperty, value);
        }

        private ImageViewerMode currentMode;
        public ImageViewerMode CurrentMode
        {
            get => currentMode;
            set
            {
                currentMode = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ImageBoxSelectionMode));
            }
        }

        public SelectionModes ImageBoxSelectionMode => currentMode == ImageViewerMode.Selection ? SelectionModes.Rectangle : SelectionModes.None;

        private Rect selectionRegion;

        public Rect SelectionRegion
        {
            get => selectionRegion;
            set
            {
                if (RaiseAndSetIfChanged(ref selectionRegion, value))
                {
                    RaisePropertyChanged(nameof(SelectionX));
                    RaisePropertyChanged(nameof(SelectionY));
                    RaisePropertyChanged(nameof(SelectionWidth));
                    RaisePropertyChanged(nameof(SelectionHeight));
                }
            }
        }

        public int SelectionX
        {
            get => (int)SelectionRegion.X;
            set
            {
                if (Image != null)
                {
                    double val = 0.0;
                    if (value > 0)
                    {
                        var imgWidth = Image.PixelSize.Width;
                        val = (value + (int)SelectionRegion.Width) > imgWidth ? imgWidth - SelectionRegion.Width : value;
                    }
                    SelectionRegion = new Rect(val, SelectionRegion.Y, SelectionRegion.Width, SelectionRegion.Height);
                }
            }
        }

        public int SelectionY
        {
            get => (int)SelectionRegion.Y;
            set
            {
                if (Image != null)
                {
                    double val = 0.0;
                    if (value > 0)
                    {
                        var imgHeight = Image.PixelSize.Height;
                        val = (value + (int)SelectionRegion.Height) > imgHeight ? imgHeight - SelectionRegion.Height : value;
                    }
                    SelectionRegion = new Rect(SelectionRegion.X, val, SelectionRegion.Width, SelectionRegion.Height);
                }
            }
        }

        public int SelectionWidth
        {
            get => (int)SelectionRegion.Width;
            set
            {
                if (Image != null)
                {
                    double selWidth = 0;
                    double xpos = 0;
                    if (value >= 0)
                    {
                        var imgWidth = Image.PixelSize.Width;
                        selWidth = Math.Min(imgWidth, value);
                        xpos = (SelectionRegion.X + (int)selWidth) > imgWidth ? imgWidth - selWidth : SelectionRegion.X;
                    }
                    SelectionRegion = new Rect(xpos, SelectionRegion.Y, selWidth, SelectionRegion.Height);
                }
            }
        }

        public int SelectionHeight
        {
            get => (int)SelectionRegion.Height;
            set
            {
                if (Image != null)
                {
                    double selHeight = 0;
                    double ypos = 0;
                    if (value >= 0)
                    {
                        var imgHeight = Image.PixelSize.Height;
                        selHeight = Math.Min(imgHeight, value);
                        ypos = (SelectionRegion.Y + (int)selHeight) > imgHeight ? imgHeight - selHeight : SelectionRegion.Y;
                    }
                    SelectionRegion = new Rect(SelectionRegion.X, ypos, SelectionRegion.Width, selHeight);
                }
            }
        }

        public void UpdateSelectionRegion()
        {
            if (isAspectRatioLocked)
            {
                SelectionHeight = _lockedSize.y;
                SelectionWidth = _lockedSize.x;
            }
        }

        private MouseButtons selectWithMouseButtons;

        public MouseButtons SelectWithMouseButtons
        {
            get => selectWithMouseButtons;
            set
            {
                if (RaiseAndSetIfChanged(ref selectWithMouseButtons, value))
                {
                    RaisePropertyChanged(nameof(ImageBoxSelectionMode));
                }
            }
        }

        private MouseButtons paintWithMouseButtons;

        public MouseButtons PaintWithMouseButtons
        {
            get => paintWithMouseButtons;
            set
            {
                if (RaiseAndSetIfChanged(ref paintWithMouseButtons, value))
                {
                    RaisePropertyChanged(nameof(ImageBoxSelectionMode));
                }
            }
        }

        private MouseButtons maskWithMouseButtons;

        public MouseButtons MaskWithMouseButtons
        {
            get => maskWithMouseButtons;
            set
            {
                if (RaiseAndSetIfChanged(ref maskWithMouseButtons, value))
                {
                    RaisePropertyChanged(nameof(ImageBoxSelectionMode));
                }
            }
        }

        private bool isChoosingColor = false;

        public bool IsChoosingColor
        {
            get => isChoosingColor;
            set
            {
                RaiseAndSetIfChanged(ref isChoosingColor, value);
            }
        }

        ObservableCollection<int> brushSizes = new ObservableCollection<int>(Enumerable.Range(1, 25).ToList());
        public ObservableCollection<int> BrushSizes => brushSizes;

        private int brushSize = 5;

        public int SelectedBrushSize
        {
            get => brushSize;
            set
            {
                RaiseAndSetIfChanged(ref brushSize, value);
                ImageBox.PaintBrushSize = brushSize;
            }
        }

        private int maskSize = 5;

        public int SelectedMaskSize
        {
            get => maskSize;
            set
            {
                RaiseAndSetIfChanged(ref maskSize, value);
                ImageBox.MaskBrushSize = maskSize;
            }
        }

        private Color paintBrushColor = new Color(255, 255, 255, 255);
        public Color PaintBrushColor
        {
            get => paintBrushColor;
            set
            {
                if (RaiseAndSetIfChanged(ref paintBrushColor, value)) ;
            }
        }

        #region Commands
        [RelayCommand]
        public void SetSelectMode()
        {
            SelectWithMouseButtons = MouseButtons.LeftButton;
            PaintWithMouseButtons = MouseButtons.None;
            MaskWithMouseButtons = MouseButtons.None;
            CurrentMode = ImageViewerMode.Selection;
        }

        [RelayCommand]
        public void SetPaintMode()
        {
            PaintWithMouseButtons = MouseButtons.LeftButton;
            SelectWithMouseButtons = MouseButtons.None;
            MaskWithMouseButtons = MouseButtons.None;
            CurrentMode = ImageViewerMode.Paint;
        }

        [RelayCommand]
        public void SetMaskMode()
        {
            MaskWithMouseButtons = MouseButtons.LeftButton;
            SelectWithMouseButtons = MouseButtons.None;
            PaintWithMouseButtons = MouseButtons.None;
            CurrentMode = ImageViewerMode.Mask;
        }

        [RelayCommand]
        public void ChooseColor()
        {
            IsChoosingColor = true;
        }

        [RelayCommand]
        public void CropSelection()
        {
            if (!SelectionRegion.IsEmpty && Image != null)
            {
                CropImageRegionAndCreateNewImage(SelectionRegion, TargetImageSize);
            }
        }

        [RelayCommand]
        public async Task ExtractComicPanels()
        {
            var window = this.VisualRoot as Window;
            string tmpImage = System.IO.Path.GetTempFileName().Replace(".tmp", ".jpg");
            if (Image != null)
            {
                Image.Save(tmpImage);

                var appDir = App.GetAppDirectory();

                KumikoWrapper kwrapper = new KumikoWrapper(App.Settings.PythonPath, Path.Combine(new string[] { appDir, "Assets", "kumiko" }));
                if (Image != null)
                {
                    try
                    {
                        var results = await kwrapper.GetImagePanels(tmpImage);

                        var comicPanels = results.Select(r => ImageBox.CreateNewImageWithLayersFromRegion(new Rect(r.TopLeftX, r.TopLeftY, r.Width, r.Height))).ToList();
                        ComicPanelsExtracted?.Invoke(comicPanels);
                    }
                    catch (Exception e)
                    {
                        {
                            if (window != null)
                            {
                                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
                                        .GetMessageBoxStandardWindow(
                                            "Panel Extraction Failed",
                                            $"Failed to execute kumiko panel extraction. Exception message was : {e.Message}",
                                            MessageBox.Avalonia.Enums.ButtonEnum.Ok);

                                await messageBoxStandardWindow.ShowDialog(window);
                            }
                        }
                    }
                }
            }
        }

        #endregion
        public void CropImageRegionAndCreateNewImage(Rect region, PixelSize? targetSize = null)
        {
            var finalSize = new PixelSize(Convert.ToInt32(region.Width), Convert.ToInt32(region.Height));
            if (targetSize != null && targetSize.Value.Height > 0 && targetSize.Value.Width > 0)
            {
                finalSize = targetSize.Value;
            }
            var newImage = ImageBox.CreateNewImageWithLayersFromRegion(region, finalSize);
            if (newImage != null)
            {
                ImageCropped?.Invoke(this, newImage);
            }
        }

        [RelayCommand]
        public void UndoLastPaint()
        {
            if (Image != null)
                ImageBox.UndoLastPaint(Image);
        }

        [RelayCommand]
        public void UndoLastMask()
        {
            if (Image != null)
                ImageBox.UndoLastMask(Image);
        }

        [RelayCommand]
        public async void Interrogate()
        {
            if (Image != null)
            {
                var image = ImageBox.CreateNewImageWithLayersFromRegion(null);
                if (image != null)
                {
                    if (InterrogateClicked != null)
                    {
                        await InterrogateClicked(image);
                    }
                }
            }
        }

        [RelayCommand]
        public async void EditImage()
        {
            if (Image != null)
            {
                if (this.EditImageClicked != null)
                {
                    var image = ImageBox.CreateNewImageWithLayersFromRegion();
                    if (image != null)
                    {
                        await this.EditImageClicked.Invoke(image);
                    }
                }

            }
        }

        [RelayCommand]
        public void CloseColorSelector()
        {
            IsChoosingColor = false;
        }

        [RelayCommand]
        public void Save()
        {
            if (ImageBox.IsPainted)
            {
                var bitmap = ImageBox.CreateNewImageWithLayersFromRegion();
                if (bitmap != null)
                {
                    SaveClicked?.Invoke(bitmap);
                }
            }
        }

        #region Bindable Base
        private PropertyChangedEventHandler? _propertyChanged;

        public new event PropertyChangedEventHandler? PropertyChanged
        {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }
        protected bool RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
        }

        /// <summary>
        ///     Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">
        ///     Name of the property used to notify listeners.  This
        ///     value is optional and can be provided automatically when invoked from compilers
        ///     that support <see cref="CallerMemberNameAttribute" />.
        /// </param>
        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            var e = new PropertyChangedEventArgs(propertyName);
            OnPropertyChanged(e);
            _propertyChanged?.Invoke(this, e);
        }

        internal void ZoomToFit()
        {
            ImageBox.ZoomToFit();
        }
        #endregion
    }
}
