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
        public event EventHandler? RequestClose;

        public InterrogationDialogViewModel(IEnumerable<ITaggerViewModelFactory> taggers, IEnumerable<INaturalLanguageInterrogatorViewModelFactory> naturalLanguageInterrogators)
        {
            Taggers = taggers.Prepend(null);
            NaturalLanguageInterrogators =  naturalLanguageInterrogators.Prepend(null);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.PropertyName == nameof(SelectedTagInterrogator))
            {
                if (SelectedTagInterrogator != null)
                {
                    SelectedTagSettingsViewModel = SelectedTagInterrogator.CreateViewModel();
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
                    SelectedNaturalLanguageSettingsViewModel = SelectedNaturalLanguageInterrogator.CreateViewModel();
                }
                else
                {
                    SelectedNaturalLanguageSettingsViewModel = null;
                }
            }
        }


        [ObservableProperty]
        public IEnumerable<ITaggerViewModelFactory?> taggers;

        [ObservableProperty]
        private ITaggerViewModelFactory? selectedTagInterrogator;
        [ObservableProperty]
        private InterrogatorViewModel<List<string>>? selectedTagSettingsViewModel;


        [ObservableProperty]
        public IEnumerable<INaturalLanguageInterrogatorViewModelFactory?> naturalLanguageInterrogators;
        [ObservableProperty]
        private INaturalLanguageInterrogatorViewModelFactory? selectedNaturalLanguageInterrogator;
        [ObservableProperty]
        private InterrogatorViewModel<string>? selectedNaturalLanguageSettingsViewModel;


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
