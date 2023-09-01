using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using StableDiffusionTagManager.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
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

            AddHandler(KeyDownEvent, KeyDownHandler, handledEventsToo: true);
            DataContextChanged += DataContextChangedHandler;
        }

        public void asdf(object? sender, CancelEventArgs args)
        {

        }
        private void DataContextChangedHandler(object? sender, EventArgs e)
        {
            var viewModel = this.DataContext as MainWindowViewModel;

            if (viewModel != null)
            {
                viewModel.ShowAddTagToAllDialogCallback = (title) =>
                {
                    var dialog = new TagSearchDialog();

                    if (title != null)
                    {
                        dialog.Title = title;
                    }
                    dialog.SetSearchFunc(viewModel.SearchTags);
                    return dialog.ShowAsync(this);
                };

                viewModel.ShowFolderDialogCallback = () => new OpenFolderDialog().ShowAsync(this);
                viewModel.FocusTagCallback = (tag) =>
                    {
                        this.FocusTagAutoComplete(tag);
                    };

                viewModel.ShowDialogHandler = new MessageBoxDialogHandler(this);
                viewModel.ExitCallback = () => Close();
                ImageBox.SaveClicked = async (image) => await viewModel.SaveCurrentImage(image);
                ImageBox.ComicPanelsExtracted = async (images) => await viewModel.ReviewComicPanels(images);
                ImageBox.ImageCropped += (source, image) => viewModel.AddNewImage(image);
                ImageBox.InterrogateClicked = async (image) => await viewModel.Interrogate(image);
                ImageBox.EditImageClicked = async (image) => await viewModel.RunImgToImg(image);
            };
        }

        private async void KeyDownHandler(object? sender, KeyEventArgs e)
        {
            var viewModel = this.DataContext as MainWindowViewModel;
            if (viewModel != null)
            {
                var focusedElement = FocusManager.Instance.Current;
                var imageListFocused = ImageList.IsFocused || (focusedElement != null && focusedElement.GetVisualAncestors().Any(p => p == ImageList));
                if ((e.KeyModifiers & KeyModifiers.Alt) > 0 && e.Key == Key.Right && !imageListFocused)
                {
                    viewModel.NextImage();
                }
                if ((e.KeyModifiers & KeyModifiers.Alt) > 0 && e.Key == Key.Left && !imageListFocused)
                {
                    viewModel.PreviousImage();
                }

                if ((e.KeyModifiers & KeyModifiers.Alt) > 0 && e.Key == Key.Enter)
                {
                    viewModel.AddTagToEnd();
                }

                if ((e.KeyModifiers & KeyModifiers.Shift) > 0 && e.Key == Key.Enter)
                {
                    AddTagInFrontOfCurrentFocusedTag();
                }

                if ((e.KeyModifiers & KeyModifiers.Control) > 0 && e.Key == Key.Enter)
                {
                    viewModel.AddTagToFront();
                }

                if ((e.KeyModifiers & KeyModifiers.Shift) > 0 && e.Key == Key.Right)
                {
                    FocusNextTag();
                }
                if ((e.KeyModifiers & KeyModifiers.Shift) > 0 && e.Key == Key.Left)
                {
                    FocusPreviousTag();
                }

                if ((e.KeyModifiers & KeyModifiers.Alt) > 0 && e.Key == Key.Delete)
                {
                    await viewModel.DeleteSelectedImage();
                }

                if ((e.KeyModifiers & KeyModifiers.Shift) > 0 && e.Key == Key.Delete)
                {
                    DeleteFocusedTag();
                }

                if ((e.KeyModifiers & KeyModifiers.Control) > 0 && e.Key == Key.Right)
                {
                    MoveTagRight();
                }
                if ((e.KeyModifiers & KeyModifiers.Control) > 0 && e.Key == Key.Left)
                {
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
            var element = args.Source as IStyledElement;
            var tag = element?.DataContext as TagViewModel;
            while (tag == null && element != null)
            {
                element = element.Parent;
                tag = element?.DataContext as TagViewModel;
            }

            var viewModel = this.DataContext as MainWindowViewModel;
            viewModel?.TagDrop(tag);
        }

        public Task<IEnumerable<object>> SearchTags(string text, CancellationToken token)
        {
            var viewModel = this.DataContext as MainWindowViewModel;
            return viewModel?.SearchTags(text, token) ?? Task.FromResult(Enumerable.Empty<object>());
        }

        public void AutoCompleteAttached(object sender, VisualTreeAttachmentEventArgs args)
        {
            var ac = sender as AutoCompleteBox;
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
            var currentFocus = FocusManager.Instance?.Current;
            var tb = currentFocus as TextBox;
            var ac = tb?.GetVisualAncestors().OfType<AutoCompleteBox>().FirstOrDefault();

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
            var currentFocus = FocusManager.Instance?.Current;
            var tb = currentFocus as TextBox;
            var ac = tb?.GetVisualAncestors().OfType<AutoCompleteBox>().FirstOrDefault();
            var autoCompletes = TagsList.GetVisualDescendants().OfType<AutoCompleteBox>().ToList();
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
            var currentFocus = FocusManager.Instance?.Current;
            var tb = currentFocus as TextBox;
            var ac = tb?.GetVisualAncestors().OfType<AutoCompleteBox>().FirstOrDefault();
            var autoCompletes = TagsList.GetVisualDescendants().OfType<AutoCompleteBox>().ToList();
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
            var currentFocus = FocusManager.Instance?.Current;
            var tb = currentFocus as TextBox;
            var ac = tb?.GetVisualAncestors().OfType<AutoCompleteBox>().FirstOrDefault();
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