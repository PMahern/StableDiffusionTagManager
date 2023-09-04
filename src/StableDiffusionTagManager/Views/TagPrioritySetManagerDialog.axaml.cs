using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using StableDiffusionTagManager.Models;
using StableDiffusionTagManager.ViewModels;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace StableDiffusionTagManager.Views
{
    public partial class TagPrioritySetManagerDialog : Window
    {
        private static readonly string PRIORITY_SETS_FOLDER = "TagPrioritySets";

        public TagPrioritySetManagerDialog()
        {
            InitializeComponent();

            Opened += TagPriortySetManagerDialog_Opened;
        }

        private void TagPriortySetManagerDialog_Opened(object? sender, System.EventArgs e)
        {
            Loading = true;
            LoadTagPrioritySets();
        }

        public async void LoadTagPrioritySets()
        {
            var txts = Directory.EnumerateFiles(PRIORITY_SETS_FOLDER, "*.txt").ToList();
            var tagPrioritySets = new ObservableCollection<TagPrioritySetViewModel>(txts.Select(filename => new TagPrioritySetViewModel(filename)));
            Dispatcher.UIThread.Post(() =>
            {
                SetValue(TagPrioritySetsProperty, tagPrioritySets);
            });
        }

        public static readonly StyledProperty<ObservableCollection<TagPrioritySetViewModel>?> TagPrioritySetsProperty =
            AvaloniaProperty.Register<TagPrioritySetManagerDialog, ObservableCollection<TagPrioritySetViewModel>?>(nameof(TagPrioritySets), null);

        /// <summary>
        /// Gets or sets the image to be displayed
        /// </summary>
        public ReadOnlyObservableCollection<TagPrioritySetViewModel>? TagPrioritySets
        {
            get
            {
                var value = GetValue(TagPrioritySetsProperty);
                if (value != null)
                {
                    return new ReadOnlyObservableCollection<TagPrioritySetViewModel>(GetValue(TagPrioritySetsProperty));
                }
                else
                {
                    return null;
                }
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

        //private ObservableCollection<TagPrioritySetViewModel> tagPrioritySets;
        //public ReadOnlyObservableCollection<TagPrioritySetViewModel> TagPrioritySets { get => new ReadOnlyObservableCollection<TagPrioritySetViewModel>(TagPrioritySets); }
        public bool Loading { get; set; } = false;

    }
}
