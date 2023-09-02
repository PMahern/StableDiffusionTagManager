using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace StableDiffusionTagManager.Views
{
    public partial class ExpandImageDialog : Window
    {
        public ExpandImageDialog()
        {
            InitializeComponent();
        }

        public bool Success { get; set;  } = false;

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

        #region Properties
        public static readonly StyledProperty<int> ExpandLeftProperty =
            AvaloniaProperty.Register<ProjectSettingsDialog, int>(nameof(ExpandLeft), 0);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public int ExpandLeft
        {
            get => GetValue(ExpandLeftProperty);
            set => SetValue(ExpandLeftProperty, value);
        }

        public static readonly StyledProperty<int> ExpandRightProperty =
            AvaloniaProperty.Register<ProjectSettingsDialog, int>(nameof(ExpandRight), 0);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public int ExpandRight
        {
            get => GetValue(ExpandRightProperty);
            set => SetValue(ExpandRightProperty, value);
        }

        public static readonly StyledProperty<int> ExpandUpProperty =
            AvaloniaProperty.Register<ProjectSettingsDialog, int>(nameof(ExpandUp), 0);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public int ExpandUp
        {
            get => GetValue(ExpandUpProperty);
            set => SetValue(ExpandUpProperty, value);
        }

        public static readonly StyledProperty<int> ExpandDownProperty =
            AvaloniaProperty.Register<ProjectSettingsDialog, int>(nameof(ExpandDown), 0);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public int ExpandDown
        {
            get => GetValue(ExpandDownProperty);
            set => SetValue(ExpandDownProperty, value);
        }
        #endregion
    }
}
