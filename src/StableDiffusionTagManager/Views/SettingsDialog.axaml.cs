using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.Extensions;
using StableDiffusionTagManager.Models;
using StableDiffusionTagManager.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;

namespace StableDiffusionTagManager.Views
{
    public partial class ImageResolution : ViewModelBase {
        [ObservableProperty]
        private int x;

        [ObservableProperty] 
        private int y;
    }
    public partial class ImageAspectRatioSet : ViewModelBase
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private ObservableCollection<ImageResolution> resolutions = new();
    }

    public partial class SettingsDialog : Window
    {
        public SettingsDialog(Settings settings)
        {
            InitializeComponent();

            this.WebUIAddress = settings.WebUiAddress;
            this.PythonPath = settings.PythonPath;
            this.ImageAspectRatioSets = settings.ImageAspectRatioSets.Select(t => new ImageAspectRatioSet()
            {
                Name = t.Item1,
                Resolutions = t.Item2.Select(r => new ImageResolution() { X = r.Item1, Y = r.Item2 }).ToObservableCollection()
            }).ToObservableCollection();
            this.DataContext = this;
            this.settings = settings;
        }

        public static readonly StyledProperty<string?> WebUIAddressProperty =
            AvaloniaProperty.Register<SettingsDialog, string?>(nameof(WebUIAddress));

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public string? WebUIAddress
        {
            get => GetValue(WebUIAddressProperty);
            set => SetValue(WebUIAddressProperty, value);
        }

        public static readonly StyledProperty<string?> PythonPathProperty =
            AvaloniaProperty.Register<SettingsDialog, string?>(nameof(PythonPath));
        private readonly Settings settings;

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public string? PythonPath
        {
            get => GetValue(PythonPathProperty);
            set => SetValue(PythonPathProperty, value);
        }

        public static readonly StyledProperty<ObservableCollection<ImageAspectRatioSet>> ImageAspectRatioSetsProperty =
            AvaloniaProperty.Register<SettingsDialog, ObservableCollection<ImageAspectRatioSet>>(nameof(ImageAspectRatioSets));

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public ObservableCollection<ImageAspectRatioSet> ImageAspectRatioSets
        {
            get => GetValue(ImageAspectRatioSetsProperty);
            set => SetValue(ImageAspectRatioSetsProperty, value);
        }

        public static readonly StyledProperty<ImageAspectRatioSet?> SelectedImageAspectRatioSetProperty =
            AvaloniaProperty.Register<SettingsDialog, ImageAspectRatioSet?>(nameof(SelectedImageAspectRatioSet));

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public ImageAspectRatioSet? SelectedImageAspectRatioSet
        {
            get => GetValue(SelectedImageAspectRatioSetProperty);
            set => SetValue(SelectedImageAspectRatioSetProperty, value);
        }

        [RelayCommand]
        public void Save()
        {
            settings.WebUiAddress = WebUIAddress;
            settings.PythonPath = PythonPath;
            settings.ImageAspectRatioSets = ImageAspectRatioSets.Select(t => (t.Name, t.Resolutions.Select(r => (r.X, r.Y)).ToList())).ToList();
            settings.Save();

            Close();
        }

        [RelayCommand]
        public void Cancel()
        {
            Close();
        }

        [RelayCommand]
        public void HeaderClose()
        {
            Cancel();
        }

        [RelayCommand]
        public void DeleteResolution(ImageResolution resolution)
        {
            if (SelectedImageAspectRatioSet != null)
            {
                SelectedImageAspectRatioSet.Resolutions.Remove(resolution);
            }
        }

        [RelayCommand]
        public void AddResolution()
        {
            if(SelectedImageAspectRatioSet != null)
            {
                SelectedImageAspectRatioSet.Resolutions.Add(new ImageResolution());
            }
        }

        [RelayCommand]
        public void AddAspectRatioSet()
        {
            ImageAspectRatioSets.Add(new ImageAspectRatioSet());
        }

        [RelayCommand]
        public void DeleteAspectRatioSet()
        {
            if (SelectedImageAspectRatioSet != null)
            {
                ImageAspectRatioSets.Remove(SelectedImageAspectRatioSet);
                SelectedImageAspectRatioSet = null;
            }
        }
    }
}
