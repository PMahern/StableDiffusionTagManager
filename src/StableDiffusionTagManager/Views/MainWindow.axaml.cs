using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using StableDiffusionTagManager.Controls;
using StableDiffusionTagManager.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.Views
{
    public partial class MainWindow : Window
    {
        private const string CustomFormat = "application/xxx-avalonia-controlcatalog-custom";

        private TagViewModel? focusTag = null;


        public MainWindow()
        {
            InitializeComponent();

            AddHandler(DragDrop.DropEvent, TagDropped);

            AddHandler(KeyDownEvent, KeyDownHandler, routes: Avalonia.Interactivity.RoutingStrategies.Tunnel);
            DataContextChanged += DataContextChangedHandler;
            Closing += ClosingHandler;
        }

        private void ClosingHandler(object? sender, CancelEventArgs e)
        {
            var viewModel = this.DataContext as MainWindowViewModel;
            if(viewModel != null)
            {
                e.Cancel = true;
                Dispatcher.UIThread.Post(() => viewModel.Exit());
            }
        }

        private void DataContextChangedHandler(object? sender, EventArgs e)
        {
            var viewModel = this.DataContext as MainWindowViewModel;

            if (viewModel != null)
            {
                viewModel.ShowFolderDialogCallback = () => this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions() { AllowMultiple = false });
                viewModel.FocusTagCallback = (tag) =>
                    {
                        this.FocusTagAutoComplete(tag);
                    };
                 
                viewModel.ShowDialogHandler = new DialogHandler(this);
                viewModel.ExitCallback = () =>
                {
                    //Remove the handler to prevent an infinite loop, if the vm says to close we KNOW we're closing.
                    Closing -= ClosingHandler;
                    Close();
                };
                viewModel.ImageDirtyCallback = (image) => ImageBox.ImageBox.ImageHasPaint(image);
                viewModel.GetModifiedImageDataCallback = (originalBitmap, clear) =>
                {
                    var bitmap = ImageBox.ImageBox.CreateNewImageWithLayersFromRegion();
                    if (clear)
                    {
                        ImageBox.ImageBox.ClearMipsAndLayers(originalBitmap);
                    }
                    return bitmap;
                };
                ImageBox.SaveClicked = async (image) => await viewModel.SaveCurrentImage(image);
                ImageBox.ComicPanelsExtracted = async (images) => await viewModel.ReviewComicPanels(images);
                ImageBox.ImageCropped += (source, image) => viewModel.ImageCropped(image);
                ImageBox.InterrogateClicked = async (image) => await viewModel.InterrogateAndApplyToSelectedImage(image);
                ImageBox.RemoveBackgroundClicked = async (image) => await viewModel.ReviewRemoveBackground(image);
                ImageBox.ConvertAlphaClicked = async (image) => await viewModel.ReviewConvertAlpha(image);
                ImageBox.ExpandClicked = async (image) => await viewModel.ExpandImage(image);
                ImageBox.EditImageClicked = async (image) => await viewModel.RunImgToImg(image);
                
            };
        }

        private async void KeyDownHandler(object? sender, KeyEventArgs e)
        {
            var viewModel = this.DataContext as MainWindowViewModel;
            if (viewModel != null)
            {
                if ((e.KeyModifiers & KeyModifiers.Alt) > 0 && e.Key == Key.Right)
                {
                    e.Handled = true;
                    viewModel.NextImage();
                }

                if ((e.KeyModifiers & KeyModifiers.Alt) > 0 && e.Key == Key.Left)
                {
                    e.Handled = true;
                    viewModel.PreviousImage();
                }

                if ((e.KeyModifiers & KeyModifiers.Alt) > 0 && e.Key == Key.Enter)
                {
                    e.Handled = true;
                    viewModel.AddTagToEnd();
                }

                if ((e.KeyModifiers & KeyModifiers.Shift) > 0 && e.Key == Key.Enter)
                {
                    e.Handled = true;
                    AddTagInFrontOfCurrentFocusedTag();
                }

                if ((e.KeyModifiers & KeyModifiers.Control) > 0 && e.Key == Key.Enter)
                {
                    e.Handled = true;
                    viewModel.AddTagToFront();
                }

                if ((e.KeyModifiers & KeyModifiers.Shift) > 0 && e.Key == Key.Right)
                {
                    e.Handled = true;
                    FocusNextTag();
                }
                if ((e.KeyModifiers & KeyModifiers.Shift) > 0 && e.Key == Key.Left)
                {
                    e.Handled = true;
                    FocusPreviousTag();
                }

                if ((e.KeyModifiers & KeyModifiers.Alt) > 0 && e.Key == Key.Delete)
                {
                    e.Handled = true;
                    await viewModel.ArchiveSelectedImage();
                }

                if ((e.KeyModifiers & KeyModifiers.Shift) > 0 && e.Key == Key.Delete)
                {
                    e.Handled = true;
                    DeleteFocusedTag();
                }

                if ((e.KeyModifiers & KeyModifiers.Control) > 0 && e.Key == Key.Right)
                {
                    e.Handled = true;
                    MoveTagRight();
                }
                if ((e.KeyModifiers & KeyModifiers.Control) > 0 && e.Key == Key.Left)
                {
                    e.Handled = true;
                    MoveTagLeft();
                }
            }
        }

        public async void TagDragStart(object sender, PointerPressedEventArgs args)
        {
            var viewModel = this.DataContext as MainWindowViewModel;
            var element = sender as InputElement;
            var tagModel = element?.DataContext as TagViewModel;

            if (tagModel != null)
            {
                viewModel?.BeginTagDrag(tagModel);
                var dataObject = new DataObject();
                dataObject.Set(CustomFormat, tagModel);
                await DragDrop.DoDragDrop(args, dataObject, DragDropEffects.Move);
            }
        }

        public void TagDraggedOver(object sender, DragEventArgs args)
        {

        }

        public void TagDropped(object? sender, DragEventArgs args)
        {
            var element = args.Source as StyledElement;
            var tag = element?.DataContext as TagViewModel;
            while (tag == null && element != null)
            {
                element = element.Parent;
                tag = element?.DataContext as TagViewModel;
            }

            var viewModel = this.DataContext as MainWindowViewModel;
            viewModel?.TagDrop(tag);
        }

        public void AutoCompleteAttached(object sender, VisualTreeAttachmentEventArgs args)
        {
            var ac = sender as TagAutoCompleteBox;
            if (focusTag != null && focusTag == (ac.DataContext as TagViewModel))
            {
                Dispatcher.UIThread.Post(() => ac?.Focus());
                focusTag = null;
            }
        }

        public void FocusTagAutoComplete(TagViewModel tagViewModel)
        {
            focusTag = tagViewModel;
        }

        private TagViewModel? GetFocusedTag()
        {
            var currentFocus = FocusManager.GetFocusedElement();
            var tb = currentFocus as TextBox;
            var ac = tb?.GetVisualAncestors().OfType<TagAutoCompleteBox>().FirstOrDefault();

            if (ac != null && ac.GetVisualAncestors().Any(anc => anc == TagsList))
            {
                return ac.DataContext as TagViewModel;
            }

            return null;
        }

        public void AddTagInFrontOfCurrentFocusedTag()
        {
            var focusedTag = GetFocusedTag();
            var viewModel = this.DataContext as MainWindowViewModel;
            if (focusedTag != null)
            {
                viewModel?.AddTagInFrontOfTag(focusedTag);
            }
            else
            {
                viewModel.AddTagToFront();
            }
        }

        public void MoveTagLeft()
        {
            var focusedTag = GetFocusedTag();
            if (focusedTag != null)
            {
                var viewModel = this.DataContext as MainWindowViewModel;
                viewModel?.MoveTagLeft(focusedTag);
            }
        }

        public void MoveTagRight()
        {
            var focusedTag = GetFocusedTag();
            if (focusedTag != null)
            {
                var viewModel = this.DataContext as MainWindowViewModel;
                viewModel?.MoveTagRight(focusedTag);
            }
        }
        
        public async Task ImageBox_SavedClicked(object? sender, Bitmap image)
        {
            var viewModel = this.DataContext as MainWindowViewModel;
            await viewModel?.SaveCurrentImage(image);
        }


        public void FocusNextTag()
        {
            var currentFocus = FocusManager.GetFocusedElement();
            var tb = currentFocus as TextBox;
            var ac = tb?.GetVisualAncestors().OfType<TagAutoCompleteBox>().FirstOrDefault();
            var autoCompletes = TagsList.GetVisualDescendants().OfType<TagAutoCompleteBox>().ToList();
            if (autoCompletes.Any())
            {
                if (ac != null && ac.DataContext is TagViewModel)
                {
                    var curIndex = autoCompletes.IndexOf(ac);
                    if (curIndex != -1 && curIndex < autoCompletes.Count() - 1)
                    {
                        autoCompletes[curIndex + 1].Focus();
                    }
                    else
                    {
                        autoCompletes.First().Focus();
                    }
                }
                else
                {
                    autoCompletes.First().Focus();
                }
            }
        }

        public void FocusPreviousTag()
        {
            var currentFocus = FocusManager.GetFocusedElement();
            var tb = currentFocus as TextBox;
            var ac = tb?.GetVisualAncestors().OfType<TagAutoCompleteBox>().FirstOrDefault();
            var autoCompletes = TagsList.GetVisualDescendants().OfType<TagAutoCompleteBox>().ToList();
            if (autoCompletes.Any())
            {
                if (ac != null && ac.DataContext is TagViewModel)
                {
                    var curIndex = autoCompletes.IndexOf(ac);
                    if (curIndex != -1 && curIndex > 0)
                    {
                        autoCompletes[curIndex - 1].Focus();
                    }
                    else
                    {
                        autoCompletes.Last().Focus();
                    }
                }
                else
                {
                    autoCompletes.Last().Focus();
                }
            }
        }

        public void DeleteFocusedTag()
        {
            var currentFocus = FocusManager.GetFocusedElement();
            var tb = currentFocus as TextBox;
            var ac = tb?.GetVisualAncestors().OfType<TagAutoCompleteBox>().FirstOrDefault();
            if (ac != null)
            {
                var tagModel = ac.DataContext as TagViewModel;
                var viewModel = this.DataContext as MainWindowViewModel;
                if (tagModel != null && viewModel != null)
                {
                    viewModel.DeleteTagFromCurrentImage(tagModel);
                }
            }
        }
    }
}