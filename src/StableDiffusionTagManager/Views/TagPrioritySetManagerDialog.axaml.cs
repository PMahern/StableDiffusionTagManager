using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.Controls;
using StableDiffusionTagManager.ViewModels;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.Views
{
    public partial class TagPrioritySetManagerDialog : Window
    {
        private const string CUSTOM_DRAG_FORMAT = "application/xxx-avalonia-controlcatalog-custom";

        private static readonly string PRIORITY_SETS_FOLDER = "TagPrioritySets";

        public TagPrioritySetManagerDialog()
        {
            InitializeComponent();

            SetValue(TagPrioritySetsProperty, new ObservableCollection<TagPrioritySetViewModel>());
            AddHandler(DragDrop.DropEvent, PrioritySetDropped);
            AddHandler(PointerPressedEvent, InitiateDrag, handledEventsToo: true);
            Opened += TagPriortySetManagerDialog_Opened;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SelectedPrioritySetProperty)
            {
                SelectedChildSet = null;
            }
        }
        private void TagPriortySetManagerDialog_Opened(object? sender, System.EventArgs e)
        {
            Loading = true;
            LoadTagPrioritySets();
        }

        private async Task InitiateDrag(object sender, PointerPressedEventArgs args)
        {
            var element = args.Source as Visual;
            if (element != null && (element.FindAncestorOfType<Button>()  == null) && (element.FindAncestorOfType<TagAutoCompleteBox>() == null))
            {
                var prioritySetViewModel = element.DataContext as TagPrioritySetViewModel;

                if (prioritySetViewModel != null && !TagPrioritySets.Contains(prioritySetViewModel) && SelectedPrioritySet != null)
                {
                    IsDraggingParent = SelectedPrioritySet.Entries.Contains(prioritySetViewModel);
                    IsDraggingChild = !IsDraggingParent;

                    var dataObject = new DataObject();
                    dataObject.Set(CUSTOM_DRAG_FORMAT, prioritySetViewModel);
                    await DragDrop.DoDragDrop(args, dataObject, DragDropEffects.Move);
                }
            }
        }

        public void PrioritySetDropped(object? sender, DragEventArgs args)
        {
            var targetDataSet = IsDraggingParent ? SelectedPrioritySet : SelectedChildSet;
            if (targetDataSet != null)
            {
                var vm = args.Data.Get(CUSTOM_DRAG_FORMAT) as TagPrioritySetViewModel;
                var sourceVisual = (args.Source as Visual);
                if (vm != null && sourceVisual != null)
                {
                    var destVm = sourceVisual
                                                .GetSelfAndVisualAncestors()
                                                .FirstOrDefault(anc => anc.DataContext is TagPrioritySetViewModel)
                                                ?.DataContext as TagPrioritySetViewModel;
                    if (destVm != null)
                    {
                        targetDataSet.MovePrioritySet(vm, destVm);
                    }
                }
            }
        }

        public async void LoadTagPrioritySets()
        {
            if (!Directory.Exists(PRIORITY_SETS_FOLDER))
            {
                Directory.CreateDirectory(PRIORITY_SETS_FOLDER);
            }
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


        public static readonly DirectProperty<TagPrioritySetManagerDialog, bool> IsDraggingChildProperty =
            AvaloniaProperty.RegisterDirect<TagPrioritySetManagerDialog, bool>(
                nameof(IsDraggingChild),
                o => o.IsDraggingChild);

        private bool _isDraggingChild = false;
        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public bool IsDraggingChild
        {
            get => _isDraggingChild;
            set
            {
                SetAndRaise(IsDraggingChildProperty, ref _isDraggingChild, value);
            }
        }

        public static readonly DirectProperty<TagPrioritySetManagerDialog, bool> IsDraggingParentProperty =
            AvaloniaProperty.RegisterDirect<TagPrioritySetManagerDialog, bool>(
                nameof(IsDraggingParent),
                o => o.IsDraggingParent);

        private bool _isDraggingParent = false;
        /// <summary>
        /// Gets or sets if control can render the image
        /// </summary>
        public bool IsDraggingParent
        {
            get => _isDraggingParent;
            set
            {
                SetAndRaise(IsDraggingParentProperty, ref _isDraggingParent, value);
            }
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
            if (SelectedPrioritySet != null)
            {
                SelectedPrioritySet.RemoveEntry(toDelete);
            }
        }

        [RelayCommand]
        public void AddPrioritySetEntry()
        {
            if (SelectedPrioritySet != null)
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