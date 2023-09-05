using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.Models;
using StableDiffusionTagManager.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.Views
{
    public partial class TagPrioritySetManagerDialog : Window
    {
        private static readonly string PRIORITY_SETS_FOLDER = "TagPrioritySets";

        public TagPrioritySetManagerDialog()
        {
            InitializeComponent();

            SetValue(TagPrioritySetsProperty, new ObservableCollection<TagPrioritySetViewModel>());
            Opened += TagPriortySetManagerDialog_Opened;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if(change.Property == SelectedPrioritySetProperty)
            {
                SelectedChildSet = null;
            }
        }
        private void TagPriortySetManagerDialog_Opened(object? sender, System.EventArgs e)
        {
            Loading = true;
            LoadTagPrioritySets();
        }

        public async void LoadTagPrioritySets()
        {
            var txts = Directory.EnumerateFiles(PRIORITY_SETS_FOLDER, "*.txt").ToList();
            var tagPrioritySets = new ObservableCollection<TagPrioritySetViewModel>(txts.Select(filename => TagPrioritySetViewModel.CreateFromFile(filename)));
            Dispatcher.UIThread.Post(() =>
            {
                SetValue(TagPrioritySetsProperty, tagPrioritySets);
            });
        }

        public static readonly StyledProperty<ObservableCollection<TagPrioritySetViewModel>> TagPrioritySetsProperty =
            AvaloniaProperty.Register<TagPrioritySetManagerDialog, ObservableCollection<TagPrioritySetViewModel>>(nameof(TagPrioritySets), null);

        

        /// <summary>
        /// Gets or sets the image to be displayed
        /// </summary>
        public ReadOnlyObservableCollection<TagPrioritySetViewModel> TagPrioritySets
        {

            get
            { 
                return new ReadOnlyObservableCollection<TagPrioritySetViewModel>(GetValue(TagPrioritySetsProperty));
            }
        }

        public static readonly StyledProperty<TagPrioritySetViewModel?> SelectedPrioritySetProperty =
            AvaloniaProperty.Register<TagPrioritySetManagerDialog, TagPrioritySetViewModel?>(nameof(SelectedPrioritySet), null);

        /// <summary>
        /// Gets or sets the image to be displayed
        /// </summary>
        public TagPrioritySetViewModel? SelectedPrioritySet
        {
            get => GetValue(SelectedPrioritySetProperty);
            set => SetValue(SelectedPrioritySetProperty, value);
        }



        public static readonly StyledProperty<TagPrioritySetViewModel?> SelectedChildSetProperty =
        AvaloniaProperty.Register<TagPrioritySetManagerDialog, TagPrioritySetViewModel?>(nameof(SelectedChildSet), null);

        /// <summary>
        /// Gets or sets the image to be displayed
        /// </summary>
        public TagPrioritySetViewModel? SelectedChildSet
        {
            get => GetValue(SelectedChildSetProperty);
            set => SetValue(SelectedChildSetProperty, value);
        }

        //private ObservableCollection<TagPrioritySetViewModel> tagPrioritySets;
        //public ReadOnlyObservableCollection<TagPrioritySetViewModel> TagPrioritySets { get => new ReadOnlyObservableCollection<TagPrioritySetViewModel>(TagPrioritySets); }
        public bool Loading { get; set; } = false;

        public bool Success { get; set; }

        #region Commands
        [RelayCommand]
        public async Task AddTagPrioritySetCommand()
        {
            var dialog = new NewItemNameDialog();
            dialog.Title = "New Tag Priority Set Name";

            await dialog.ShowDialog(this);

            if (dialog.Success && dialog.Name != null)
            {
                var collection = GetValue(TagPrioritySetsProperty);
                collection.Add(new TagPrioritySetViewModel(dialog.NewItemName));
            }
        }

        [RelayCommand]
        public void Save()
        {
            this.Success = true;
            foreach (var item in TagPrioritySets)
            {
                item.Save(PRIORITY_SETS_FOLDER);
            }
            Close();
        }

        [RelayCommand]
        public void Cancel()
        {
            Close();
        }

        [RelayCommand]
        public void DeletePrioritySetEntry(TagPrioritySetViewModel toDelete)
        {
            if(SelectedPrioritySet != null)
            {
                SelectedPrioritySet.RemoveEntry(toDelete);
            }
        }

        [RelayCommand]
        public void AddPrioritySetEntry()
        {
            if(SelectedPrioritySet != null)
            {
                SelectedPrioritySet.AddEntry();
            }    
        }

        [RelayCommand]
        public void DeleteChildSetEntry(TagPrioritySetViewModel toDelete)
        {
            if (SelectedChildSet != null)
            {
                SelectedChildSet.RemoveEntry(toDelete);
            }
        }

        [RelayCommand]
        public void AddChildSetEntry()
        {
            if (SelectedChildSet != null)
            {
                SelectedChildSet.AddEntry();
            }
        }
        #endregion
    }
}