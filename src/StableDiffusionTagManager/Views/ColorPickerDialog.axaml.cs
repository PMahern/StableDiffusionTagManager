using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.Controls;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.Views
{
    public partial class ColorPickerDialog : Window, IDialogWithResultAsync<Color?>
    {
        public ColorPickerDialog()
        {
            InitializeComponent();
        }

        public bool Success { get; set; } = false;

        [RelayCommand]
        public void Save()
        {
            this.Success = true;
            Close();
        }

        [RelayCommand]
        public void Cancel()
        {
            Close();
        }

        public async Task<Color?> ShowWithResult(Window parent)
        {
            await ShowDialog(parent);

            if (this.Success)
            {
                return SelectedColor;
            }
            else
            {
                return null;
            }
        }

        public static readonly StyledProperty<Color> SelectedColorProperty =
            AvaloniaProperty.Register<ProjectSettingsDialog, Color>(nameof(SelectedColor), new Color(255, 255, 255, 255));

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public Color SelectedColor
        {
            get => GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public void WindowKeyDown(object sender, KeyEventArgs e) 
        {
            if(e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
