using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Newtonsoft.Json;
using SdWebUiApi;
using SdWebUiApi.Api;
using SdWebUiApi.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using StableDiffusionTagManager.Extensions;
using StableDiffusionTagManager.Models;
using StableDiffusionTagManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Color = Avalonia.Media.Color;

namespace StableDiffusionTagManager.Views
{
    public partial class ImageTouchupDialog : Window
    {
        public ImageTouchupDialog(Settings settings)
        {
            InitializeComponent();

            this.DataContext = this;

            Opened += ImageCleanupDialog_Opened;
            this.settings = settings;
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
                var api = new DefaultApi(settings.WebUiAddress);

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

                    if (this.IsActive)
                    {
                        await messageBoxStandardWindow.ShowWindowDialogAsync(this);
                    }
                });
            }
        }
        public bool Success { get; set; }

        public static readonly StyledProperty<bool> IsStandaloneProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, bool>(nameof(IsStandalone), false);
        public bool IsStandalone
        {
            get => GetValue(IsStandaloneProperty);
            set => SetValue(IsStandaloneProperty, value);
        }

        public static readonly StyledProperty<Bitmap?> ImageProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, Bitmap?>(nameof(Image));

        public Bitmap? Image
        {
            get => GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
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

        public static readonly StyledProperty<decimal> CFGScaleProperty =
            AvaloniaProperty.Register<ImageTouchupDialog, decimal>(nameof(CFGScale), 7);

        public decimal CFGScale
        {
            get => GetValue(CFGScaleProperty);
            set => SetValue(CFGScaleProperty, value);
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
        private readonly Settings settings;

        public int ImageHeight
        {
            get => GetValue(ImageHeightProperty);
            set => SetValue(ImageHeightProperty, value);
        }

        public List<string> Tags { get; set; } = new List<string>();

        public Project? OpenProject { get; set; }


        private Bitmap GetCurrentMaskImage()
        {
            if (ImageBox.CurrentMode == Controls.ImageViewerMode.Mask)
            {
                return ImageBox.ImageBox.CreateNewImageFromMask().InvertColors();
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

                return renderTarget;
            }
        }
        [RelayCommand]
        public async Task GenerateImages()
        {
            if (settings.WebUiAddress != null)
            {
                try
                {
                    var api = new DefaultApi(settings.WebUiAddress);

                    var flattened = ImageBox.ImageBox.CreateNewImageWithLayersFromRegion();
                    if (flattened != null)
                    {
                        this.IsLoading = true;
                        using var uploadStream = new MemoryStream();
                        flattened.Save(uploadStream);
                        var imagebase64 = Convert.ToBase64String(uploadStream.ToArray());

                        Bitmap? maskImage = GetCurrentMaskImage();

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
                            InpaintingMaskInvert = 0,
                            SamplerName = SelectedSampler ?? "Euler a",
                            Steps = SamplingSteps,
                            MaskBlur = MaskBlur,
                            CfgScale = CFGScale
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
                        viewer.ExtraActions = new List<ImageReviewActionViewModel>()
                        { 
                            new ImageReviewActionViewModel(viewer)
                            {
                                Action = (bitmap) => SaveBitmapMaskData(bitmap),
                                Name = "Save With Mask Data"
                            }
                        }.ToObservableCollection();

                        await viewer.ShowDialog(this);

                        if (viewer.Success)
                        {
                            ImageBox.ImageBox.ClearPaintAndMask(Image);
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

        public async Task SaveBitmapMaskData(Bitmap image)
        {
            var options = new FilePickerSaveOptions();
            options.FileTypeChoices = new List<FilePickerFileType>() { FilePickerFileTypes.ImagePng };
            var dialogResult = await StorageProvider.SaveFilePickerAsync(options);
            if(dialogResult != null)
            {
                var mask = GetCurrentMaskImage();

                if (mask != null)
                {
                    var maskResults = image.ApplyMask(mask);
                    maskResults.FullMaskedImage.Save(dialogResult.Path.AbsolutePath);
                    maskResults.CroppedMaskedImage.Save(dialogResult.Path.AbsolutePath.Replace(".png", "_cropped.png"));
                    maskResults.CroppedMask.Save(dialogResult.Path.AbsolutePath.Replace(".png", "_croppedmask.png"));
                    File.WriteAllText(dialogResult.Path.AbsolutePath.Replace(".png", "_maskbounds.json"), JsonConvert.SerializeObject(maskResults.Bounds, Formatting.Indented));
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

        public void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        [RelayCommand]
        public async Task LoadImage()
        {
            var options = new FilePickerOpenOptions();
            options.AllowMultiple = false;
            options.FileTypeFilter = new List<FilePickerFileType>() { FilePickerFileTypes.ImageAll };
            var files = await StorageProvider.OpenFilePickerAsync(options);

            if (files.Count > 0)
            {
                var file = files[0].Path.AbsolutePath;

                Image = new Bitmap(files.First().Path.AbsolutePath);

                //assumes each image has one and only one text chunk which is true for all images from StableDiffustion
                if (file.EndsWith(".png"))
                {
                    using var image = SixLabors.ImageSharp.Image.Load(file);

                        // Get the PNG metadata
                    var pngMetadata = image.Metadata.GetFormatMetadata(PngFormat.Instance);

                    //assumes each image has one and only one text chunk which is true for all images from StableDiffustion
                    if (pngMetadata.TextData.Any(t => t.Keyword == "parameters"))
                    {

                        var data = pngMetadata.TextData[0].Value;
                        var chunks = data.Split('\n');
                        var negativePromptIndex = data.LastIndexOf("\nNegative prompt: ");
                        var stepsIndex = data.LastIndexOf("\nSteps: ");
                        if (negativePromptIndex != -1)
                        {
                            Prompt = data.Substring(0, negativePromptIndex);
                            NegativePrompt = data.Substring(negativePromptIndex + 1, data.LastIndexOf("\nSteps: ") - negativePromptIndex - 1).Replace("Negative prompt: ", "");
                        } else
                        {
                            Prompt = data.Substring(0, stepsIndex);
                            NegativePrompt = "";
                        }
                        var otherSettings = data.Substring(stepsIndex).Split(",").Select(s =>
                        {
                            var pair = s.Split(":");
                            return new { setting = pair[0].Trim(), value = pair[1].Trim() };
                        });
                        SamplingSteps = otherSettings.FirstOrDefault(s => s.setting == "Steps")?.value.ConvertTo<int>() ?? SamplingSteps;
                        SelectedSampler = otherSettings.FirstOrDefault(s => s.setting == "Sampler")?.value ?? SelectedSampler;
                        CFGScale = otherSettings.FirstOrDefault(s => s.setting == "CFG scale")?.value.ConvertTo<decimal>() ?? CFGScale;
                        DenoiseStrength = otherSettings.FirstOrDefault(s => s.setting == "Denoising strength")?.value?.ConvertTo<decimal>() ?? DenoiseStrength;
                        var dimensions = otherSettings.FirstOrDefault(s => s.setting == "Size")?.value.Split("x");
                        if(dimensions != null)
                        {
                            ImageWidth = int.Parse(dimensions[0]);
                            ImageHeight = int.Parse(dimensions[1]);
                        }
                    }
                    
                }
            }
        }

        [RelayCommand]
        public async Task SaveImage()
        {
            var options = new FilePickerSaveOptions();
            options.FileTypeChoices = new List<FilePickerFileType>() { FilePickerFileTypes.ImagePng };
            options.Title = "Save image...";
            var file = await StorageProvider.SaveFilePickerAsync(options);

            if (file != null && file.Path.AbsolutePath.EndsWith(".png"))
            {
                var path = file.TryGetLocalPath();
                if (path != null && path.EndsWith(".png"))
                {
                    var toSave = ImageBox.CreateNewImageWithLayers();

                    if (toSave != null)
                    {
                        toSave.Save(path);
                    }
                }
            }
        }

        [RelayCommand]
        public void Exit()
        {
            Close();
        }
    }
}
