using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using ImageUtil;
using StableDiffusionTagManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StableDiffusionTagManager.Views;


public partial class InterrogationDialog : Window
{
    public static Dictionary<InterrogatorDescription<ITagInterrogator>, Func<InterrogatorViewModel<List<string>>>> TaggerViewModelFactories;
    public static Dictionary<InterrogatorDescription<INaturalLanguageInterrogator>, Func<InterrogatorViewModel<string>>> NaturalLanguageViewModelFactories;

    static InterrogationDialog()
    {
        TaggerViewModelFactories = Interrogators.TagInterrogators.ToDictionary(i => i, i =>
        {
            var expr = () => (InterrogatorViewModel<List<string>>)new DefaultTagInterrogationViewModel(i.Factory);
            return expr;
        });

        NaturalLanguageViewModelFactories = Interrogators.NaturalLanguageInterrogators.ToDictionary(i => i, i =>
        {
            var expr = () => (InterrogatorViewModel<string>)new DefaultNaturalLanguageInterrogationViewModel(i);
            return expr;
        });
    }

    public InterrogationDialog()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedTagInterrogatorProperty)
        {
            if (SelectedTagInterrogator != null)
            {
                SelectedTagSettingsViewModel = TaggerViewModelFactories[SelectedTagInterrogator]();
            }
            else
            {
                SelectedTagSettingsViewModel = null;
            }
        }

        if (change.Property == SelectedNaturalLanguageInterrogatorProperty)
        {
            if (SelectedNaturalLanguageInterrogator != null)
            {
                SelectedNaturalLanguageSettingsViewModel = NaturalLanguageViewModelFactories[SelectedNaturalLanguageInterrogator]();
            }
            else
            {
                SelectedNaturalLanguageSettingsViewModel = null;
            }
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
        }
    }

    public static readonly StyledProperty<InterrogatorDescription<ITagInterrogator>?> SelectedTagInterrogatorProperty =
            AvaloniaProperty.Register<InterrogationDialog, InterrogatorDescription<ITagInterrogator>?>(nameof(SelectedTagInterrogator));

    public InterrogatorDescription<ITagInterrogator>? SelectedTagInterrogator
    {
        get => GetValue(SelectedTagInterrogatorProperty);
        set => SetValue(SelectedTagInterrogatorProperty, value);
    }

    public static readonly StyledProperty<InterrogatorViewModel<string>?> SelectedNaturalLanguageSettingsViewModelProperty =
            AvaloniaProperty.Register<InterrogationDialog, InterrogatorViewModel<string>?>(nameof(SelectedNaturalLanguageSettingsViewModel));

    public InterrogatorViewModel<string>? SelectedNaturalLanguageSettingsViewModel
    {
        get => GetValue(SelectedNaturalLanguageSettingsViewModelProperty);
        set => SetValue(SelectedNaturalLanguageSettingsViewModelProperty, value);
    }

    public static readonly StyledProperty<InterrogatorViewModel<List<string>>?> SelectedTagSettingsViewModelProperty =
            AvaloniaProperty.Register<InterrogationDialog, InterrogatorViewModel<List<string>>?>(nameof(SelectedTagSettingsViewModel));

    public InterrogatorViewModel<List<string>>? SelectedTagSettingsViewModel
    {
        get => GetValue(SelectedTagSettingsViewModelProperty);
        set => SetValue(SelectedTagSettingsViewModelProperty, value);
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
