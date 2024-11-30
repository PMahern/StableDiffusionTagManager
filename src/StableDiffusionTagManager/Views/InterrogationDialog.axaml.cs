using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using ImageUtil;
using StableDiffusionTagManager.ViewModels;

namespace StableDiffusionTagManager.Views;

public partial class InterrogationDialog : Window
{
    public InterrogationDialog()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property.Name == nameof(SelectedNaturalLanguageInterrogator))
        {
            Prompt = SelectedNaturalLanguageInterrogator?.DefaultPrompt ?? "Describe the image.";
        }
    }

    public static readonly StyledProperty<InterrogatorDescription<INaturalLanguageInterrogator>?> SelectedNaturalLanguageInterrogatorProperty =
            AvaloniaProperty.Register<InterrogationDialog, InterrogatorDescription<INaturalLanguageInterrogator>?>(nameof(SelectedNaturalLanguageInterrogator));

    public InterrogatorDescription<INaturalLanguageInterrogator>? SelectedNaturalLanguageInterrogator
    {
        get => GetValue(SelectedNaturalLanguageInterrogatorProperty);
        set
        {
            SetValue(SelectedNaturalLanguageInterrogatorProperty, value);
            SelectedNaturalLanguageSettingsViewModel = value?.GetSettingsViewModel();
        }
    }

    public static readonly StyledProperty<InterrogatorDescription<ITagInterrogator>?> SelectedTagInterrogatorProperty =
            AvaloniaProperty.Register<InterrogationDialog, InterrogatorDescription<ITagInterrogator>?>(nameof(SelectedTagInterrogator));

    public InterrogatorDescription<ITagInterrogator>? SelectedTagInterrogator
    {
        get => GetValue(SelectedTagInterrogatorProperty);
        set => SetValue(SelectedTagInterrogatorProperty, value);
    }

    public static readonly StyledProperty<float> TagThresholdProperty =
            AvaloniaProperty.Register<InterrogationDialog, float>(nameof(TagThreshold), 0.3f);

    public float TagThreshold
    {
        get => GetValue(TagThresholdProperty);
        set => SetValue(TagThresholdProperty, value);
    }

    public static readonly StyledProperty<string> PromptProperty =
            AvaloniaProperty.Register<InterrogationDialog, string>(nameof(Prompt), "Describe the image.");

    public string Prompt
    {
        get => GetValue(PromptProperty);
        set => SetValue(PromptProperty, value);
    }

    public static readonly StyledProperty<ViewModelBase?> SelectedInterrogatorSettingsViewModelProperty =
            AvaloniaProperty.Register<InterrogationDialog, ViewModelBase?>(nameof(SelectedNaturalLanguageSettingsViewModel));

    public ViewModelBase? SelectedNaturalLanguageSettingsViewModel
    {
        get => GetValue(SelectedInterrogatorSettingsViewModelProperty);
        set => SetValue(SelectedInterrogatorSettingsViewModelProperty, value);
    }

    public bool Success { get; set; } = false;

    [RelayCommand]
    public void Interrogate()
    {
        if (SelectedNaturalLanguageInterrogator != null || SelectedTagInterrogator != null)
        {
            Success = true;
            Close();
        }
    }

    [RelayCommand]
    public void Cancel()
    {
        Close();
    }
}
