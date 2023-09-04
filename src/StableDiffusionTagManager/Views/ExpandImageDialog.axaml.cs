using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;

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

        public void ComputeExpansionNeededForTargetAspectRatio(int currentWidth, int currentHeight, int targetWidth, int targetHeight)
        {
            var currentAspect = (double)currentWidth / (double)currentHeight;
            var targetAspect = (double)targetWidth / (double)targetHeight;
            if (targetAspect > currentAspect)
            {
                var computeWidth = (double)currentWidth * (targetAspect / currentAspect);
                ExpandLeft = (int)((computeWidth - currentWidth) / 2.0);
                ExpandRight = (int)((computeWidth - currentWidth) / 2.0);
            } else
            {
                var computeHeight = (double)currentHeight * (currentAspect / targetAspect);
                ExpandUp = (int)((computeHeight - currentHeight) / 2.0);
                ExpandDown = (int)((computeHeight - currentHeight) / 2.0);
            }
            
        }
    }
}
