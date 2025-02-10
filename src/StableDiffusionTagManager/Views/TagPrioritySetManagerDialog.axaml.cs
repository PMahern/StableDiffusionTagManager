using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.Extensions;
using StableDiffusionTagManager.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.Views
{
    public partial class TagPrioritySetManagerDialog : Window
    {
        private const string CUSTOM_DRAG_FORMAT = "application/xxx-avalonia-controlcatalog-custom";

        private static readonly string PRIORITY_SETS_FOLDER = "TagPrioritySets";

        public static string GetPrioritySetFolderPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PRIORITY_SETS_FOLDER);
        }

        public TagPrioritySetManagerDialog()
        {
            InitializeComponent();

            AddHandler(DragDrop.DropEvent, PrioritySetDropped);
            AddHandler(PointerPressedEvent, InitiateDrag, handledEventsToo: true);
            Opened += TagPriortySetManagerDialog_Opened;
            PropertyChanged += TagPrioritySetManagerDialog_OnPropertyChanged;
        }

        private void TagPrioritySetManagerDialog_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == SelectedCategoryProperty)
            {
                AddTagCommand.NotifyCanExecuteChanged();
            }
        }

        private void TagPriortySetManagerDialog_Opened(object? sender, System.EventArgs e)
        {
            //LoadTagPrioritySets();
        }

        private async Task InitiateDrag(object sender, PointerPressedEventArgs args)
        {
            //var element = args.Source as Visual;
            //if (element != null && (element.FindAncestorOfType<Button>()  == null) && (element.FindAncestorOfType<TagAutoCompleteBox>() == null))
            //{
            //    var prioritySetViewModel = element.DataContext as TagPrioritySetViewModel;

            //    if (prioritySetViewModel != null && !TagPrioritySets.Contains(prioritySetViewModel) && SelectedPrioritySet != null)
            //    {
            //        IsDraggingParent = SelectedPrioritySet.Entries.Contains(prioritySetViewModel);
            //        IsDraggingChild = !IsDraggingParent;

            //        var dataObject = new DataObject();
            //        dataObject.Set(CUSTOM_DRAG_FORMAT, prioritySetViewModel);
            //        await DragDrop.DoDragDrop(args, dataObject, DragDropEffects.Move);
            //    }
            //}
        }

        public void PrioritySetDropped(object? sender, DragEventArgs args)
        {
            //var targetDataSet = IsDraggingParent ? PrioritySet : SelectedCategory;
            //if (targetDataSet != null)
            //{
            //    var vm = args.Data.Get(CUSTOM_DRAG_FORMAT) as TagPrioritySetViewModel;
            //    var sourceVisual = (args.Source as Visual);
            //    if (vm != null && sourceVisual != null)
            //    {
            //        var destVm = sourceVisual
            //                                    .GetSelfAndVisualAncestors()
            //                                    .FirstOrDefault(anc => anc.DataContext is TagPrioritySetViewModel)
            //                                    ?.DataContext as TagPrioritySetViewModel;
            //        if (destVm != null)
            //        {
            //            targetDataSet.MovePrioritySet(vm, destVm);
            //        }
            //    }
            //}
        }

        //public void LoadTagPrioritySets()
        //{
        //    if (!Directory.Exists(PRIORITY_SETS_FOLDER))
        //    {
        //        Directory.CreateDirectory(PRIORITY_SETS_FOLDER);
        //    }
        //    var txts = Directory.EnumerateFiles(PRIORITY_SETS_FOLDER, "*.xml").ToList();
        //    var tagPrioritySets = new ObservableCollection<TagPrioritySetViewModel>(txts.Select(filename => TagPrioritySetViewModel.CreateFromFile(filename)));
        //    SetValue(TagPrioritySetsProperty, tagPrioritySets);
        //}

        public static readonly StyledProperty<TagPrioritySetViewModel> PrioritySetProperty =
            AvaloniaProperty.Register<TagPrioritySetManagerDialog, TagPrioritySetViewModel>(nameof(PrioritySet), new TagPrioritySetViewModel());

        /// <summary>
        /// The tag priority set being edited.
        /// </summary>
        public TagPrioritySetViewModel PrioritySet
        {
            get => GetValue(PrioritySetProperty);
            set => SetValue(PrioritySetProperty, value);
        }


        public static readonly StyledProperty<TagCategoryViewModel?> SelectedCategoryProperty =
        AvaloniaProperty.Register<TagPrioritySetManagerDialog, TagCategoryViewModel?>(nameof(SelectedCategory), null);

        /// <summary>
        /// The selected tag category to edit.
        /// </summary>
        public TagCategoryViewModel? SelectedCategory
        {
            get => GetValue(SelectedCategoryProperty);
            set
            {
                SetValue(SelectedCategoryProperty, value);
            }
        }

        public static readonly DirectProperty<TagPrioritySetManagerDialog, bool> IsDraggingChildProperty =
            AvaloniaProperty.RegisterDirect<TagPrioritySetManagerDialog, bool>(
                nameof(IsDraggingChild),
                o => o.IsDraggingChild);

        private bool _isDraggingChild = false;
        /// <summary>
        /// Indicates if a tag is being dragged.
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
        /// Indicates if a category is being dragged.
        /// </summary>
        public bool IsDraggingParent
        {
            get => _isDraggingParent;
            set
            {
                SetAndRaise(IsDraggingParentProperty, ref _isDraggingParent, value);
            }
        }
        public bool Loading { get; set; } = false;

        public bool Success { get; set; }

        public static readonly DirectProperty<TagPrioritySetManagerDialog, string?> CurrentOpenFileProperty =
            AvaloniaProperty.RegisterDirect<TagPrioritySetManagerDialog, string?>(
                nameof(CurrentOpenFile),
                o => o.CurrentOpenFile);

        private string? _CurrentOpenFile = null;
        /// <summary>
        /// Indicates if a category is being dragged.
        /// </summary>
        public string? CurrentOpenFile
        {
            get => _CurrentOpenFile;
            set
            {
                SetAndRaise(CurrentOpenFileProperty, ref _CurrentOpenFile, value);
            }
        }

        #region Commands

        [RelayCommand]
        public async Task Save()
        {
            if (_CurrentOpenFile != null)
            {
                PrioritySet.Save(Path.Combine(GetPrioritySetFolderPath(), $"{CurrentOpenFile}.xml"));
            }
            else
            {
                SaveTagCollectionAs();
                var textEntryDialog = new TextInputDialog();
                textEntryDialog.DialogTitle = "Enter Name for Priority Set";
                var result = await textEntryDialog.ShowWithResult(this);
                if (result != null)
                {
                    CurrentOpenFile = result;
                    PrioritySet.AddCategory(result);
                }
            }
        }

        [RelayCommand]
        public void Cancel()
        {
            Close();
        }

        [RelayCommand]
        public void DeleteCategory(TagCategoryViewModel toDelete)
        {
            if (PrioritySet != null)
            {
               PrioritySet.RemoveCategory(toDelete);
            }
        }

        public bool CanAddTag()
        {
            return SelectedCategory != null;
        }

        [RelayCommand]
        public async Task AddCategory()
        {
            var textEntryDialog = new TextInputDialog();
            textEntryDialog.DialogTitle = "New Tag Collection Name";
            var result = await textEntryDialog.ShowWithResult(this);
            if (result != null)
            {
                PrioritySet.AddCategory(result);
            }
        }

        [RelayCommand]
        public void DeleteTag(string toDelete)
        {
            if (SelectedCategory != null)
            {
                SelectedCategory.RemoveTag(toDelete);
            }
        }

        [RelayCommand(CanExecute = nameof(CanAddTag))]
        public async Task AddTag()
        {
            if (SelectedCategory!= null)
            {
                var textEntryDialog = new TextInputDialog();
                textEntryDialog.DialogTitle = "New Tag Collection Name";
                var result = await textEntryDialog.ShowWithResult(this);
                if (result != null)
                {
                    SelectedCategory.Tags.Add(result);
                }
            }
        }

        [RelayCommand]
        public async Task CreateTagCollection()
        {
        }

        [RelayCommand]
        public async Task SaveTagCollection()
        {         
        }

        [RelayCommand]
        public async Task SaveTagCollectionAs()
        {
        }

        [RelayCommand]
        public async Task LoadTagCollection()
        {
            if (!Directory.Exists(PRIORITY_SETS_FOLDER))
            {
                Directory.CreateDirectory(PRIORITY_SETS_FOLDER);
            }
            var dialog = new DropdownSelectDialog();
            var xmls = Directory.EnumerateFiles(PRIORITY_SETS_FOLDER, "*.xml").ToList();
            dialog.DropdownItems = xmls.ToDropdownSelectItems(xml => Path.GetFileNameWithoutExtension(xml));

            var result = await dialog.ShowWithResult(this);
            var itemResult = result as DropdownSelectItem<string>;
            if(itemResult != null)
            {
                PrioritySet = TagPrioritySetViewModel.CreateFromFile(itemResult.Value);
            }
        }
        #endregion
    }
}