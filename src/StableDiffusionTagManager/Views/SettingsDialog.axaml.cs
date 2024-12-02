using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.Models;

namespace StableDiffusionTagManager.Views
{
    public partial class SettingsDialog : Window
    {
        public SettingsDialog(Settings settings)
        {
            InitializeComponent();

            this.WebUIAddress = settings.WebUiAddress;
            this.PythonPath = settings.PythonPath;
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

        [RelayCommand]
        public void Save()
        {
            settings.WebUiAddress = WebUIAddress;
            settings.PythonPath = PythonPath;
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
    }
}
