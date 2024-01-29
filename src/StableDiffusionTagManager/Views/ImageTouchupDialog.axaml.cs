using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using SdWebUiApi;
using SdWebUiApi.Api;
using StableDiffusionTagManager.Extensions;
using StableDiffusionTagManager.Models;
using StableDiffusionTagManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
            UpdateSamplers();
        }

        public async Task UpdateSamplers()
        {
            try
            {
                var api = new DefaultApi(App.Settings.WebUiAddress);

                var samplers = await api.GetSamplersSdapiV1SamplersGetAsync();

                Dispatcher.UIThread.Post(() =>
                {
                    this.Samplers = samplers.Select(s => s.Name).ToObservableCollection();

                    this.SelectedSampler = this.Samplers.First();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    var messageBoxStandardWindow = MessageBoxManager
                                .GetMessageBoxStandard("Failed to get samplers",
                                                             $"Querying stable diffusion webui for the list of available samplers failed. This likely indicates the server can't be reached. Error message:\n\r {ex.Message}",
                                                             ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);

                    await messageBoxStandardWindow.ShowWindowDialogAsync(this);
                });
            }
        }
        public bool Success { get; set; }

        public static readonly StyledProperty<Bitmap?> ImagePropery =
            AvaloniaProperty.Register<ImageTouchupDialog, Bitmap?>(nameof(Image));

        public Bitmap? Image
        {
            get => GetValue(ImagePropery);
            set => SetValue(ImagePropery, value);
        }

        public static readonly StyledProperty<string> PromptProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, string>(nameof(Prompt));

        public string Prompt
        {
            get => GetValue(PromptProperty);
            set => SetValue(PromptProperty, value);
        }

        public static readonly StyledProperty<string> NegativePromptProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, string>(nameof(NegativePrompt));

        public string NegativePrompt
        {
            get => GetValue(NegativePromptProperty);
            set => SetValue(NegativePromptProperty, value);
        }

        public static readonly StyledProperty<int> MaskBlurProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, int>(nameof(MaskBlur), 4);

        public int MaskBlur
        {
            get => GetValue(MaskBlurProperty);
            set => SetValue(MaskBlurProperty, value);
        }

        public static readonly StyledProperty<bool> InpaintOnlyMaskedProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, bool>(nameof(InpaintOnlyMasked), true);

        public bool InpaintOnlyMasked
        {
            get => GetValue(InpaintOnlyMaskedProperty);
            set => SetValue(InpaintOnlyMaskedProperty, value);
        }

        public static readonly StyledProperty<decimal> DenoiseStrengthProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, decimal>(nameof(DenoiseStrength), 0.5M);

        public decimal DenoiseStrength
        {
            get => GetValue(DenoiseStrengthProperty);
            set => SetValue(DenoiseStrengthProperty, value);
        }

        public static readonly StyledProperty<int> OnlyMaskedPaddingProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, int>(nameof(OnlyMaskedPadding), 64);

        public int OnlyMaskedPadding
        {
            get => GetValue(OnlyMaskedPaddingProperty);
            set => SetValue(OnlyMaskedPaddingProperty, value);
        }

        public static readonly StyledProperty<int> SamplingStepsProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, int>(nameof(SamplingSteps), 20);

        public int SamplingSteps
        {
            get => GetValue(SamplingStepsProperty);
            set => SetValue(SamplingStepsProperty, value);
        }

        public static readonly StyledProperty<string?> SelectedSamplerProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, string?>(nameof(SelectedSampler), null);

        public string? SelectedSampler
        {
            get => GetValue(SelectedSamplerProperty);
            set => SetValue(SelectedSamplerProperty, value);
        }

        public static readonly StyledProperty<ObservableCollection<string>> SamplersProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, ObservableCollection<string>>(nameof(Samplers), new ObservableCollection<string>());

        public ObservableCollection<string> Samplers
        {
            get => GetValue(SamplersProperty);
            set => SetValue(SamplersProperty, value);
        }

        public ObservableCollection<MaskedContent> maskContents = Enum.GetValues<MaskedContent>().ToObservableCollection();

        public ObservableCollection<MaskedContent> MaskedContents { get => maskContents; }

        public static readonly StyledProperty<MaskedContent> SelectedMaskedContentProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, MaskedContent>(nameof(SelectedMaskedContent), MaskedContent.Original);

        public MaskedContent SelectedMaskedContent
        {
            get => GetValue(SelectedMaskedContentProperty);
            set => SetValue(SelectedMaskedContentProperty, value);
        }

        public static readonly StyledProperty<int> BatchSizeProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, int>(nameof(BatchSize), 1);

        public int BatchSize
        {
            get => GetValue(BatchSizeProperty);
            set => SetValue(BatchSizeProperty, value);
        }

        public static readonly StyledProperty<bool> IsLoadingProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, bool>(nameof(IsLoading), false);

        public bool IsLoading
        {
            get => GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public static readonly StyledProperty<int> ImageWidthProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, int>(nameof(ImageWidth), 512);

        public int ImageWidth
        {
            get => GetValue(ImageWidthProperty);
            set => SetValue(ImageWidthProperty, value);
        }

        public static readonly StyledProperty<int> ImageHeightProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, int>(nameof(ImageHeight), 512);

        public int ImageHeight
        {
            get => GetValue(ImageHeightProperty);
            set => SetValue(ImageHeightProperty, value);
        }

        public List<string> Tags { get; set; } = new List<string>();

        public Project? OpenProject { get; set; }

        [RelayCommand]
        public async Task GenerateImages()
        {
            if (App.Settings.WebUiAddress != null)
            {
                try
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
                            var blackBrush = new SolidColorBrush(new Color(255, 0, 0, 0));
                            using (var dc = renderTarget.CreateDrawingContext())
                            {
                                dc.FillRectangle(blackBrush, new Rect(0, 0, Image.PixelSize.Width, Image.PixelSize.Height), 0);
                                dc.DrawRectangle(whiteBrush, null, new RoundedRect(selectionRegion, 0));
                            }

                            maskImage = renderTarget;
                        }

                        using var maskStream = new MemoryStream();
                        maskImage.Save(maskStream);
                        var maskbase64 = Convert.ToBase64String(maskStream.ToArray());
                        maskImage.Dispose();


                        var r = await api.Img2imgapiSdapiV1Img2imgPostAsync(new SdWebUiApi.Model.StableDiffusionProcessingImg2Img
                        {
                            Prompt = Prompt,
                            NegativePrompt = NegativePrompt,
                            InitImages = new List<object> { imagebase64 },
                            InpaintFullRes = InpaintOnlyMasked,
                            Width = ImageWidth,
                            Height = ImageHeight,
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
                catch (Exception ex)
                {
                    var messageBoxStandardWindow = MessageBoxManager
                                .GetMessageBoxStandard("Failed to generate images",
                                                             $"Image generation failed. This likely indicates the server can't be reached or the input parameters are invalid. Error message:\n\r {ex.Message}",
                                                             ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);

                    await messageBoxStandardWindow.ShowWindowDialogAsync(this);
                    this.IsLoading = false;
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
            this.Image = ImageBox.ImageBox.CreateNewImageWithLayersFromRegion();
            Close();
        }
    }
}
