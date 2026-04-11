using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SdWebUiApi;
using StableDiffusionTagManager.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace StableDiffusionTagManager.Views
{
    /// <summary>A single editable concept key/value pair.</summary>
    public partial class ConceptEntry : ObservableObject
    {
        [ObservableProperty]
        private string key = string.Empty;

        [ObservableProperty]
        private string value = string.Empty;
    }

    /// <summary>A single editable response-stripping open/close tag pair.</summary>
    public partial class StripPairEntry : ObservableObject
    {
        [ObservableProperty]
        private string open = string.Empty;

        [ObservableProperty]
        private string close = string.Empty;
    }

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
                    DefaultNaturalLanguageInterrogationPrompt = project.DefaultNaturalLanguageInterrogationPrompt;
                    DefaultTagInterrogationPrompt = project.DefaultTagInterrogationPrompt;
                    DefaultInterrogationEndpointUrl = project.DefaultInterrogationEndpointUrl;

                    Concepts.Clear();
                    foreach (var kv in project.Concepts)
                        Concepts.Add(new ConceptEntry { Key = kv.Key, Value = kv.Value });

                    StripPairs.Clear();
                    foreach (var (open, close) in project.ResponseStripPairs)
                        StripPairs.Add(new StripPairEntry { Open = open, Close = close });
                }
            }

        }

        public ObservableCollection<ConceptEntry> Concepts { get; } = new ObservableCollection<ConceptEntry>();

        [RelayCommand]
        public void AddConcept()
        {
            Concepts.Add(new ConceptEntry());
        }

        [RelayCommand]
        public void RemoveConcept(ConceptEntry entry)
        {
            Concepts.Remove(entry);
        }

        public ObservableCollection<StripPairEntry> StripPairs { get; } = new ObservableCollection<StripPairEntry>();

        [RelayCommand]
        public void AddStripPair()
        {
            StripPairs.Add(new StripPairEntry());
        }

        [RelayCommand]
        public void RemoveStripPair(StripPairEntry entry)
        {
            StripPairs.Remove(entry);
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

        public int? TargetImageHeight
        {
            get => GetValue(TargetImageHeightProperty);
            set => SetValue(TargetImageHeightProperty, value);
        }

        public static readonly StyledProperty<string?> DefaultNaturalLanguageInterrogationPromptProperty =
            AvaloniaProperty.Register<ProjectSettingsDialog, string?>(nameof(DefaultNaturalLanguageInterrogationPrompt));

        public string? DefaultNaturalLanguageInterrogationPrompt
        {
            get => GetValue(DefaultNaturalLanguageInterrogationPromptProperty);
            set => SetValue(DefaultNaturalLanguageInterrogationPromptProperty, value);
        }

        public static readonly StyledProperty<string?> DefaultTagInterrogationPromptProperty =
            AvaloniaProperty.Register<ProjectSettingsDialog, string?>(nameof(DefaultTagInterrogationPrompt));

        public string? DefaultTagInterrogationPrompt
        {
            get => GetValue(DefaultTagInterrogationPromptProperty);
            set => SetValue(DefaultTagInterrogationPromptProperty, value);
        }

        public static readonly StyledProperty<string?> DefaultInterrogationEndpointUrlProperty =
            AvaloniaProperty.Register<ProjectSettingsDialog, string?>(nameof(DefaultInterrogationEndpointUrl));

        public string? DefaultInterrogationEndpointUrl
        {
            get => GetValue(DefaultInterrogationEndpointUrlProperty);
            set => SetValue(DefaultInterrogationEndpointUrlProperty, value);
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
                project.DefaultNaturalLanguageInterrogationPrompt = DefaultNaturalLanguageInterrogationPrompt;
                project.DefaultTagInterrogationPrompt = DefaultTagInterrogationPrompt;
                project.DefaultInterrogationEndpointUrl = DefaultInterrogationEndpointUrl;

                project.Concepts = Concepts
                    .Where(c => !string.IsNullOrWhiteSpace(c.Key))
                    .ToDictionary(c => c.Key.Trim(), c => c.Value);

                project.ResponseStripPairs = StripPairs
                    .Where(p => !string.IsNullOrWhiteSpace(p.Open))
                    .Select(p => (p.Open, p.Close))
                    .ToList();

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
