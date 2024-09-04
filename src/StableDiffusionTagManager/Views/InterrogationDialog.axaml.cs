using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using ImageUtil;
using System;

namespace StableDiffusionTagManager.Views;

public partial class InterrogationDialog : Window
{
    public InterrogationDialog()
    {
        InitializeComponent();

        DataContext = this;
    }

    public static readonly StyledProperty<InterrogatorDescription<INaturalLanguageInterrogator>?> SelectedNaturalLanguageInterrogatorProperty =
            AvaloniaProperty.Register<InterrogationDialog, InterrogatorDescription<INaturalLanguageInterrogator>?>(nameof(SelectedNaturalLanguageInterrogator));

    public InterrogatorDescription<INaturalLanguageInterrogator>? SelectedNaturalLanguageInterrogator
    {
        get => GetValue(SelectedNaturalLanguageInterrogatorProperty);
        set => SetValue(SelectedNaturalLanguageInterrogatorProperty, value);
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

    public bool Success { get; set; } = false;

    [RelayCommand]
    public void Interrogate()
    {
        if(SelectedNaturalLanguageInterrogator != null || SelectedTagInterrogator != null)
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