using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace StableDiffusionTagManager.Views
{
    public partial class SettingsDialog : Window
    {
        public SettingsDialog()
        {
            InitializeComponent();

            this.WebUIAddress = App.Settings.WebUiAddress;
            this.PythonPath = App.Settings.PythonPath;
            this.DataContext = this;
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
            App.Settings.WebUiAddress = WebUIAddress;
            App.Settings.PythonPath = PythonPath;
            App.Settings.Save();

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
