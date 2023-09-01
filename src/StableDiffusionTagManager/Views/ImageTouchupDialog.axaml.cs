using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using SdWebUpApi;
using SdWebUpApi.Api;
using StableDiffusionTagManager.Extensions;
using StableDiffusionTagManager.Models;
using StableDiffusionTagManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static UVtools.AvaloniaControls.AdvancedImageBox;

namespace StableDiffusionTagManager.Views
{
    public partial class ImageTouchupDialog : Window
    {
        public ImageTouchupDialog()
        {
            InitializeComponent();

            this.DataContext = this;

            Opened += ImageCleanupDialog_Opened;
        }

        private void ImageCleanupDialog_Opened(object? sender, EventArgs e)
        {
            var validtags = Tags.Where(t => (OpenProject?.ActivationKeyword ?? null) != t).ToList();
            ImageBox.ZoomToFit();
            Prompt = (OpenProject?.DefaultPromptPrefix ?? "") + (validtags.Any() ? validtags.Aggregate((l, r) => $"{l}, {r}") : "");
            NegativePrompt = OpenProject?.DefaultNegativePrompt ?? "";
            DenoiseStrength = OpenProject?.DefaultDenoiseStrength ?? 0.25M;

            var api = new DefaultApi(App.Settings.WebUiAddress);

            var samplers = api.GetSamplersSdapiV1SamplersGet();

            this.Samplers = samplers.Select(s => s.Name).ToObservableCollection();

            this.SelectedSampler = this.Samplers.First();
        }

        public bool Success { get; set; }

        public static readonly StyledProperty<Bitmap?> ImagePropery =
            AvaloniaProperty.Register<ImageTouchupDialog, Bitmap?>(nameof(Image));

        /// <summary>
        /// Gets or sets the image to be displayed
        /// </summary>
        public Bitmap? Image
        {
            get => GetValue(ImagePropery);
            set => SetValue(ImagePropery, value);
        }

        public static readonly StyledProperty<string> PromptProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, string>(nameof(Prompt));

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public string Prompt
        {
            get => GetValue(PromptProperty);
            set => SetValue(PromptProperty, value);
        }

        public static readonly StyledProperty<string> NegativePromptProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, string>(nameof(NegativePrompt));

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public string NegativePrompt
        {
            get => GetValue(NegativePromptProperty);
            set => SetValue(NegativePromptProperty, value);
        }

        public static readonly StyledProperty<int> MaskBlurProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, int>(nameof(MaskBlur), 4);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public int MaskBlur
        {
            get => GetValue(MaskBlurProperty);
            set => SetValue(MaskBlurProperty, value);
        }

        public static readonly StyledProperty<bool> InpaintOnlyMaskedProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, bool>(nameof(InpaintOnlyMasked), true);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public bool InpaintOnlyMasked
        {
            get => GetValue(InpaintOnlyMaskedProperty);
            set => SetValue(InpaintOnlyMaskedProperty, value);
        }

        public static readonly StyledProperty<decimal> DenoiseStrengthProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, decimal>(nameof(DenoiseStrength), 0.5M);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public decimal DenoiseStrength
        {
            get => GetValue(DenoiseStrengthProperty);
            set => SetValue(DenoiseStrengthProperty, value);
        }

        public static readonly StyledProperty<int> OnlyMaskedPaddingProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, int>(nameof(OnlyMaskedPadding), 64);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public int OnlyMaskedPadding
        {
            get => GetValue(OnlyMaskedPaddingProperty);
            set => SetValue(OnlyMaskedPaddingProperty, value);
        }

        public static readonly StyledProperty<int> SamplingStepsProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, int>(nameof(SamplingSteps), 20);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public int SamplingSteps
        {
            get => GetValue(SamplingStepsProperty);
            set => SetValue(SamplingStepsProperty, value);
        }

        public static readonly StyledProperty<string?> SelectedSamplerProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, string?>(nameof(SelectedSampler), null);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public string? SelectedSampler
        {
            get => GetValue(SelectedSamplerProperty);
            set => SetValue(SelectedSamplerProperty, value);
        }

        public static readonly StyledProperty<ObservableCollection<string>> SamplersProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, ObservableCollection<string>>(nameof(Samplers), new ObservableCollection<string>());

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public ObservableCollection<string> Samplers
        {
            get => GetValue(SamplersProperty);
            set => SetValue(SamplersProperty, value);
        }

        public ObservableCollection<MaskedContent> maskContents = Enum.GetValues<MaskedContent>().ToObservableCollection();

        public ObservableCollection<MaskedContent> MaskedContents { get => maskContents; }

        public static readonly StyledProperty<MaskedContent> SelectedMaskedContentProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, MaskedContent>(nameof(SelectedMaskedContent), MaskedContent.Original);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public MaskedContent SelectedMaskedContent
        {
            get => GetValue(SelectedMaskedContentProperty);
            set => SetValue(SelectedMaskedContentProperty, value);
        }

        public static readonly StyledProperty<int> BatchSizeProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, int>(nameof(BatchSize), 1);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public int BatchSize
        {
            get => GetValue(BatchSizeProperty);
            set => SetValue(BatchSizeProperty, value);
        }

        public static readonly StyledProperty<bool> IsLoadingProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, bool>(nameof(IsLoading), false);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public bool IsLoading
        {
            get => GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public List<string> Tags { get; set; } = new List<string>();

        public Project? OpenProject { get; set; }

        [RelayCommand]
        public async Task GenerateImages()
        {
            if (App.Settings.WebUiAddress != null)
            {
                var api = new DefaultApi(App.Settings.WebUiAddress);

                var flattened = ImageBox.ImageBox.CreateNewImageWithLayersFromRegion();
                if (flattened != null)
                {
                    this.IsLoading = true;
                    using var uploadStream = new MemoryStream();
                    flattened.Save(uploadStream);
                    var imagebase64 = Convert.ToBase64String(uploadStream.ToArray());

                    Bitmap? maskImage = null;
                    var invertmask = false;
                    if (ImageBox.CurrentMode == Controls.ImageViewerMode.Mask)
                    {
                        maskImage = ImageBox.ImageBox.CreateNewImageFromMask();
                        invertmask = true;
                    }
                    else
                    {
                        var renderTarget = new RenderTargetBitmap(Image.PixelSize);
                        var selectionRegion = ImageBox.SelectionRegion;
                        var whiteBrush = new SolidColorBrush(new Color(255, 255, 255, 255));
                        using (var dc = renderTarget.CreateDrawingContext(null))
                        {
                            dc.Clear(new Color(255, 0, 0, 0));
                            dc.DrawRectangle(whiteBrush, null, new RoundedRect(selectionRegion, 0));
                        }

                        maskImage = renderTarget;
                    }

                    using var maskStream = new MemoryStream();
                    maskImage.Save(maskStream);
                    var maskbase64 = Convert.ToBase64String(maskStream.ToArray());
                    maskImage.Dispose();


                    var r = await api.Img2imgapiSdapiV1Img2imgPostAsync(new SdWebUpApi.Model.StableDiffusionProcessingImg2Img
                    {
                        Prompt = Prompt,
                        NegativePrompt = NegativePrompt,
                        InitImages = new List<object> { imagebase64 },
                        InpaintFullRes = InpaintOnlyMasked,
                        Mask = maskbase64,
                        InpaintFullResPadding = OnlyMaskedPadding,
                        DenoisingStrength = DenoiseStrength,
                        InpaintingFill = (int)SelectedMaskedContent,
                        BatchSize = BatchSize,
                        InpaintingMaskInvert = invertmask ? 1 : 0,
                        SamplerName = SelectedSampler ?? "Euler a",
                        Steps = SamplingSteps,
                        MaskBlur = MaskBlur
                    });

                    var bitmaps = r.Images.Select(imageData =>
                    {
                        using (var mstream = new MemoryStream(Convert.FromBase64String(imageData)))
                        {
                            return new ImageReviewViewModel(new Bitmap(mstream));
                        }
                    }).ToObservableCollection();

                    this.IsLoading = false;
                    var viewer = new ImageReviewDialog();
                    viewer.Title = "Select New Image";
                    viewer.Images = bitmaps;
                    await viewer.ShowDialog(this);

                    if (viewer.Success)
                    {
                        ImageBox.ImageBox.ClearMipsAndLayers(Image);
                        this.Image = viewer.SelectedImage.Image;
                    }
                }
            }
        }

        [RelayCommand]
        public void Cancel()
        {
            Close();
        }

        [RelayCommand]
        public void SaveChanges()
        {
            this.Success = true;
            Close();
        }
    }
}
