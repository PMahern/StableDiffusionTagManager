using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace StableDiffusionTagManager.Views
{
    public partial class NewItemNameDialog : Window
    {
        public NewItemNameDialog()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public static readonly StyledProperty<string> NewItemNameProperty =
            AvaloniaProperty.Register<ProjectSettingsDialog, string>(nameof(NewItemName));

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public string NewItemName
        {
            get => GetValue(NewItemNameProperty);
            set => SetValue(NewItemNameProperty, value);
        }

        public bool Success { get; set; } = false;

        [RelayCommand]
        public void Create()
        {
            Success = true;
            Close();
        }

        [RelayCommand]
        public void Cancel()
        {
            Close();
        }
    }
}
