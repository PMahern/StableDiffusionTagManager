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

        [RelayCommand]
        public void Save()
        {
            App.Settings.WebUiAddress = WebUIAddress;
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
