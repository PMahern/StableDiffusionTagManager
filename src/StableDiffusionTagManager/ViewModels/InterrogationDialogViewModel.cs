using ImageUtil;
using System.Collections.Generic;
using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class InterrogationDialogViewModel : ViewModelBase
    {
        public static Dictionary<InterrogatorDescription<ITagInterrogator>, Func<InterrogatorViewModel<List<string>>>> TaggerViewModelFactories;
        public static Dictionary<InterrogatorDescription<INaturalLanguageInterrogator>, Func<InterrogatorViewModel<string>>> NaturalLanguageViewModelFactories;
        public event EventHandler? RequestClose;

        public InterrogationDialogViewModel()
        { }

        static InterrogationDialogViewModel()
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

        protected override void OnPropertyChanged(PropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.PropertyName == nameof(SelectedTagInterrogator))
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

            if (change.PropertyName == nameof(SelectedNaturalLanguageInterrogator))
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

        [ObservableProperty]
        private InterrogatorDescription<INaturalLanguageInterrogator>? selectedNaturalLanguageInterrogator;

        [ObservableProperty]
        private InterrogatorDescription<ITagInterrogator>? selectedTagInterrogator;

        [ObservableProperty]
        private InterrogatorViewModel<string>? selectedNaturalLanguageSettingsViewModel;

        [ObservableProperty]
        private InterrogatorViewModel<List<string>>? selectedTagSettingsViewModel;

        public bool Success { get; set; } = false;

        [RelayCommand]
        public void Interrogate()
        {
            if (SelectedNaturalLanguageInterrogator != null || SelectedTagInterrogator != null)
            {
                Success = true;
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
        }

        [RelayCommand]
        public void Cancel()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
