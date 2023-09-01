using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using SdWebUpApi;
using StableDiffusionTagManager.Models;

namespace StableDiffusionTagManager.Views
{
    public partial class ProjectSettingsDialog : Window
    {
        public ProjectSettingsDialog()
        {
            InitializeComponent();
        }

        private Project? project = null;
        public Project? Project
        {
            get => project;
            set
            {
                project = value;
                if (project != null)
                {
                    DefaultPromptPrefix = project.DefaultPromptPrefix;
                    DefaultNegativePrompt = project.DefaultNegativePrompt;
                    DefaultDenoiseStrength = project.DefaultDenoiseStrength;
                    ActivationKeyword = project.ActivationKeyword;
                    TargetImageWidth = project.TargetImageSize?.Width;
                    TargetImageHeight = project.TargetImageSize?.Height;
                    DefaultPromptPrefix = project.DefaultPromptPrefix;
                    InterrogateMethod = project.InterrogateMethod;
                }
            }

        }
        #region Properties
        public static readonly StyledProperty<string?> DefaultPromptPrefixProperty =
            AvaloniaProperty.Register<ProjectSettingsDialog, string?>(nameof(DefaultPromptPrefix));

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public string? DefaultPromptPrefix
        {
            get => GetValue(DefaultPromptPrefixProperty);
            set => SetValue(DefaultPromptPrefixProperty, value);
        }

        public static readonly StyledProperty<string?> DefaultNegativePromptProperty =
            AvaloniaProperty.Register<ProjectSettingsDialog, string?>(nameof(DefaultNegativePrompt));

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public string? DefaultNegativePrompt
        {
            get => GetValue(DefaultNegativePromptProperty);
            set => SetValue(DefaultNegativePromptProperty, value);
        }

        public static readonly StyledProperty<decimal?> DefaultDenoiseStrengthProperty =
            AvaloniaProperty.Register<ProjectSettingsDialog, decimal?>(nameof(DefaultDenoiseStrength), 0.5M);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public decimal? DefaultDenoiseStrength
        {
            get => GetValue(DefaultDenoiseStrengthProperty);
            set => SetValue(DefaultDenoiseStrengthProperty, value);
        }

        public static readonly StyledProperty<string?> ActivationKeywordProperty =
            AvaloniaProperty.Register<ProjectSettingsDialog, string?>(nameof(ActivationKeyword));

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public string? ActivationKeyword
        {
            get => GetValue(ActivationKeywordProperty);
            set => SetValue(ActivationKeywordProperty, value);
        }

        public static readonly StyledProperty<int?> TargetImageWidthProperty =
            AvaloniaProperty.Register<ProjectSettingsDialog, int?>(nameof(TargetImageWidth), null);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public int? TargetImageWidth
        {
            get => GetValue(TargetImageWidthProperty);
            set => SetValue(TargetImageWidthProperty, value);
        }

        public static readonly StyledProperty<int?> TargetImageHeightProperty =
            AvaloniaProperty.Register<ProjectSettingsDialog, int?>(nameof(TargetImageHeight), null);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public int? TargetImageHeight
        {
            get => GetValue(TargetImageHeightProperty);
            set => SetValue(TargetImageHeightProperty, value);
        }

        public static readonly StyledProperty<InterrogateMethod> InterrogateMethodProperty =
            AvaloniaProperty.Register<ProjectSettingsDialog, InterrogateMethod>(nameof(InterrogateMethod), InterrogateMethod.DeepDanBooru);

        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public InterrogateMethod InterrogateMethod
        {
            get => GetValue(InterrogateMethodProperty);
            set => SetValue(InterrogateMethodProperty, value);
        }
        #endregion

        #region Commands
        [RelayCommand]
        public void Save()
        {
            if (project != null)
            {
                project.DefaultPromptPrefix = DefaultPromptPrefix;
                project.DefaultNegativePrompt = DefaultNegativePrompt;
                project.DefaultDenoiseStrength = DefaultDenoiseStrength;
                project.ActivationKeyword = ActivationKeyword;
                if (TargetImageWidth != null && TargetImageHeight != null)
                {
                    project.TargetImageSize = new PixelSize(TargetImageWidth.Value, TargetImageHeight.Value);
                }
                else
                {
                    project.TargetImageSize = null;
                }
                project.DefaultPromptPrefix = DefaultPromptPrefix;
                project.InterrogateMethod = InterrogateMethod;
                project.Save();

                Close();
            }
        }

        [RelayCommand]
        public void Cancel()
        {
            Close();
        }
        #endregion
    }
}
