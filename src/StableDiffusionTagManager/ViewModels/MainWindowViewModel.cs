using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using TagUtil;
using System.Linq;
using System;
using Avalonia;
using Avalonia.Media.Imaging;
using System.IO;
using StableDiffusionTagManager.Models;
using System.Collections.Generic;
using MessageBox.Avalonia.Enums;
using StableDiffusionTagManager.Views;
using MessageBox.Avalonia.BaseWindows.Base;
using Avalonia.Controls;
using SdWebUpApi.Api;
using Newtonsoft.Json.Linq;
using StableDiffusionTagManager.Extensions;
using Avalonia.Media;
using StableDiffusionTagManager.Controls;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private static readonly string PROJECT_FOLDER_NAME = ".sdtmproj";
        private static readonly string PROJECT_FILE_NAME = "_project.xml";
        private static readonly string ARCHIVE_FOLDER = "archive";
        private static readonly string TAG_PRIORITY_SETS_FOLDER = "TagPrioritySets";

        public MainWindowViewModel()
        {
            UpdateTagPrioritySets();
        }

        private ObservableCollection<ImageWithTagsViewModel>? imageWithTagsViewModel;
        public ObservableCollection<ImageWithTagsViewModel>? ImagesWithTags
        {
            get => imageWithTagsViewModel;
            set
            {
                imageWithTagsViewModel = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ImagesLoaded));
            }
        }

        private ObservableCollection<TagCollectionViewModel>? tagCollections;
        public ObservableCollection<TagCollectionViewModel>? TagCollections { get => tagCollections; set { tagCollections = value; OnPropertyChanged(); } }

        List<string>? tagCache;
        ImageWithTagsViewModel? selectedImage;
        public ImageWithTagsViewModel? SelectedImage
        {
            get => selectedImage;
            set
            {
                if (selectedImage != value)
                {
                    if (tagCache != null && selectedImage != null)
                    {
                        var newTags = selectedImage.Tags.Select(t => t.Tag);

                        var removedTags = tagCache.Except(newTags);
                        var addedTags = newTags.Except(tagCache);

                        foreach (var removedTag in removedTags)
                        {
                            var tag = TagCounts.First(t => t.Tag == removedTag);
                            if (tag.Count == 1)
                            {
                                TagCounts.Remove(tag);
                            }
                            else
                            {
                                tag.Count--;
                            }
                        }

                        foreach (var addedTag in addedTags)
                        {
                            var tag = TagCounts.FirstOrDefault(t => t.Tag == addedTag);
                            if (tag == null)
                            {
                                TagCounts.Add(new TagWithCountViewModel(this) { Tag = addedTag, Count = 1 });
                            }
                            else
                            {
                                tag.Count++;
                            }
                        }

                        TagCounts = TagCounts.OrderBy(t => t.Tag).ToObservableCollection();
                    }

                    selectedImage = value;

                    tagCache = selectedImage?.Tags.Select(t => t.Tag).ToList();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsImageSelected));
                }
            }
        }

        FolderTagSets? FolderTagSets { get; set; }
        private Project? openProject = null;
        private string? openFolder = null;

        public bool IsProject { get => openProject != null; }

        #region Callbacks and Events
        public DialogHandler? ShowDialogHandler { get; set; }

        public Func<Task<string?>>? ShowFolderDialogCallback { get; set; }

        public Action<TagViewModel>? FocusTagCallback { get; set; }

        public Action? ExitCallback { get; set; }

        public Func<Bitmap, bool>? ImageDirtyCallback { get; set; }
        #endregion

        private Task<TResult> ShowDialog<TResult>(IMsBoxWindow<TResult> mbox) where TResult : struct
        {
            if (ShowDialogHandler != null)
            {
                return ShowDialogHandler.ShowDialog(mbox);
            }
            return Task.FromResult(default(TResult));
        }

        private async Task<TResult?> ShowDialog<TResult>(IDialogWithResultAsync<TResult> dialog)
        {
            if (ShowDialogHandler != null)
            {
                return await ShowDialogHandler.ShowDialog(dialog);
            }
            return default;
        }

        private Task<TResult> ShowDialog<TResult>(Window dialog) where TResult : struct
        {
            if (ShowDialogHandler != null)
            {
                return ShowDialogHandler.ShowDialog<TResult>(dialog);
            }
            return Task.FromResult(default(TResult));
        }

        private Task ShowDialog(Window dialog)
        {
            if (ShowDialogHandler != null)
            {
                return ShowDialogHandler.ShowDialog(dialog);
            }
            return Task.CompletedTask;
        }

        [RelayCommand]
        public async Task LoadFolder()
        {
            if (!await CheckCanExit())
            {
                return;
            }

            if (ShowFolderDialogCallback != null)
            {
                var pickResult = await ShowFolderDialogCallback();

                if (pickResult != null)
                {
                    var projectPath = Path.Combine(pickResult, PROJECT_FOLDER_NAME);
                    var projFolder = Directory.Exists(projectPath);
                    var projFile = Path.Combine(projectPath, PROJECT_FILE_NAME);
                    if (!projFolder)
                    {
                        var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxStandardWindow("Create Project?",
                                                         "It appears you haven't created a project for this folder. Creating a project will back up all the current existing images and tag sets into a folder named .sdtmproj so you can restore them and will also let you set some properties on the project. Would you like to create a project now?",
                                                         ButtonEnum.YesNo,
                                                         Icon.Question);
                        if ((await ShowDialog(messageBoxStandardWindow)) == ButtonResult.Yes)
                        {

                            projFolder = true;

                            messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
                                .GetMessageBoxStandardWindow("Rename Images?",
                                                             "You can optionally rename all the images to have an increasing index instead of the current existing pattern.  Would you like to do this now?",
                                                             ButtonEnum.YesNo,
                                                             Icon.Question);

                            var renameImages = (await ShowDialog(messageBoxStandardWindow) == ButtonResult.Yes);

                            var jpegs = Directory.EnumerateFiles(pickResult, "*.jpg").ToList();
                            var pngs = Directory.EnumerateFiles(pickResult, "*.png").ToList();
                            var txts = Directory.EnumerateFiles(pickResult, "*.txt").ToList();

                            Directory.CreateDirectory(projectPath);

                            var project = new Project(projFile);
                            project.ProjectUpdated = UpdateProjectSettings;

                            var all = jpegs.Concat(pngs).Concat(txts).ToList();
                            if (renameImages)
                            {
                                foreach (var file in all)
                                {
                                    File.Move(file, Path.Combine(projectPath, Path.GetFileName(file)));
                                }

                                var movedjpegs = Directory.EnumerateFiles(projectPath, "*.jpg").ToList();
                                var movedpngs = Directory.EnumerateFiles(projectPath, "*.png").ToList();
                                var movedtxts = Directory.EnumerateFiles(projectPath, "*.txt").ToList();
                                var imagesToCopy = movedjpegs.Concat(movedpngs).ToList();

                                int i = 1;
                                foreach (var image in imagesToCopy)
                                {
                                    var filename = i.ToString("00000");
                                    var newFileName = Path.Combine(pickResult, $"{filename}{Path.GetExtension(image)}");
                                    File.Copy(image, newFileName);
                                    var matchingTagFile = movedtxts.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == Path.GetFileNameWithoutExtension(image));
                                    if (matchingTagFile != null)
                                    {
                                        File.Copy(matchingTagFile, Path.Combine(pickResult, $"{filename}.txt"));
                                    }
                                    project.AddBackedUpFileMap(Path.GetFileName(image), Path.GetFileName(newFileName));

                                    ++i;
                                }
                                project.Save();
                            }
                            else
                            {
                                foreach (var file in all)
                                {
                                    File.Copy(file, Path.Combine(projectPath, Path.GetFileName(file)));
                                }
                            }
                        }
                    }

                    if (projFolder)
                    {
                        openProject = new Project(projFile);
                        openProject.ProjectUpdated += UpdateProjectSettings;
                        UpdateProjectSettings();
                    }

                    FolderTagSets = new FolderTagSets(pickResult);

                    ImagesWithTags = new(FolderTagSets.TagsSets.Select(tagSet => new ImageWithTagsViewModel(tagSet.ImageFile, tagSet.TagSet, ImageDirtyHandler))
                                    .OrderBy(iwt => iwt.FirstNumberedChunk)
                                    .ThenBy(iwt => iwt.SecondNumberedChunk));

                    if (openProject != null)
                    {
                        TagCollections = new ObservableCollection<TagCollectionViewModel>(openProject.TagCollections.Select(c => new TagCollectionViewModel(this, c.Name, c.Tags)));
                    }

                    openFolder = pickResult;

                    OnPropertyChanged(nameof(IsProject));
                    UpdateTagCounts();
                }
            }
        }

        private void UpdateProjectSettings()
        {
            TargetImageSize = openProject?.TargetImageSize;
        }

        [RelayCommand]
        public void SaveChanges()
        {
            if (ImagesWithTags != null)
            {
                foreach (var image in ImagesWithTags)
                {
                    var set = new TagSet(Path.Combine(openFolder, image.GetTagsFileName()), image.Tags.Select(t => t.Tag));
                    set.WriteFile();

                    image.ClearTagsDirty();
                }
            }
        }

        //Tag Drag handling
        public void BeginTagDrag(TagViewModel tagViewModel)
        {
            selectedImage?.BeginTagDrag(tagViewModel);
        }

        public void TagDrop(TagViewModel dropTarget)
        {
            selectedImage?.TagDrop(dropTarget);
        }

        public bool IsImageSelected => SelectedImage != null;

        public void AddTagInFrontOfTag(TagViewModel tag)
        {
            var newTag = new TagViewModel("");
            var index = SelectedImage?.Tags.IndexOf(tag) ?? -1;
            if (index != -1)
            {
                SelectedImage?.InsertTag(index, newTag);
            }
            FocusTagCallback?.Invoke(newTag);
        }

        [RelayCommand(CanExecute = nameof(IsImageSelected))]
        public void AddTagToFront()
        {
            var newTag = new TagViewModel("");
            SelectedImage?.InsertTag(0, newTag);
            FocusTagCallback?.Invoke(newTag);
        }

        [RelayCommand(CanExecute = nameof(IsImageSelected))]
        public void AddTagToEnd()
        {
            var newTag = new TagViewModel("");
            SelectedImage?.AddTag(newTag);
            FocusTagCallback?.Invoke(newTag);
        }

        public void AddTagToEnd(string tag)
        {
            var newTag = new TagViewModel(tag);
            SelectedImage?.AddTag(newTag);
        }

        public void AddMissingTagsToEnd(List<string> Tags)
        {
            if (SelectedImage != null)
            {
                foreach (var tag in Tags)
                {
                    if (!SelectedImage.Tags.Any(t => t.Tag == tag))
                    {
                        SelectedImage.AddTag(new TagViewModel(tag));
                    }
                }
            }
        }

        public bool ImagesLoaded => this.ImagesWithTags?.Any() ?? false;

        [RelayCommand(CanExecute = nameof(ImagesLoaded))]
        public async void AddTagToEndOfAllImages()
        {
            if (ImagesWithTags != null)
            {
                var dialog = new TagSearchDialog();
                var tagResult = await ShowDialog<string?>(dialog);

                if (tagResult != null)
                {
                    foreach (var image in ImagesWithTags)
                    {
                        if (!image.Tags.Any(t => t.Tag == tagResult))
                        {
                            image.AddTag(new TagViewModel(tagResult));
                        }
                    }
                }
            }
        }

        [RelayCommand(CanExecute = nameof(ImagesLoaded))]
        public async void AddTagToStartOfAllImages()
        {
            if (ImagesWithTags != null)
            {
                var dialog = new TagSearchDialog();
                var tagResult = await ShowDialog<string?>(dialog);

                if (tagResult != null)
                {
                    foreach (var image in ImagesWithTags)
                    {
                        if (!image.Tags.Any(t => t.Tag == tagResult))
                        {
                            image.InsertTag(0, new TagViewModel(tagResult));
                        }
                    }
                }
            }

            UpdateTagCounts();
        }

        [RelayCommand(CanExecute = nameof(ImagesLoaded))]
        public async void RemoveTagFromAllImages()
        {
            if (ImagesWithTags != null)
            {
                var dialog = new TagSearchDialog();
                var tagResult = await ShowDialog<string?>(dialog);

                if (tagResult != null)
                {
                    foreach (var image in ImagesWithTags)
                    {
                        var toRemove = image.Tags.Where(t => t.Tag == tagResult).ToList();
                        foreach (var tagToRemove in toRemove)
                        {
                            image.RemoveTag(tagToRemove);
                        }
                    }
                }
            }

            UpdateTagCounts();
        }

        internal void NextImage()
        {
            if (ImagesWithTags?.Any() ?? false)
            {
                if (SelectedImage == null)
                {
                    SelectedImage = this.ImagesWithTags.First();
                }
                else
                {
                    var index = ImagesWithTags.IndexOf(SelectedImage);
                    if (index < ImagesWithTags.Count() - 1)
                    {
                        SelectedImage = ImagesWithTags[index + 1];
                    }
                }
            }
        }

        internal void PreviousImage()
        {
            if (ImagesWithTags?.Any() ?? false)
            {
                if (SelectedImage != null)
                {
                    var index = ImagesWithTags.IndexOf(SelectedImage);
                    if (index > 0)
                    {
                        SelectedImage = ImagesWithTags[index - 1];
                    }
                }
            }
        }

        public void DeleteTagFromCurrentImage(TagViewModel tag)
        {
            if (SelectedImage != null)
            {
                SelectedImage.RemoveTag(tag);
            }
        }

        private ObservableCollection<TagWithCountViewModel> tagCounts = new ObservableCollection<TagWithCountViewModel>();
        public ObservableCollection<TagWithCountViewModel> TagCounts { get => tagCounts; set { tagCounts = value; OnPropertyChanged(); } }

        private PixelSize? targetImageSize;
        public PixelSize? TargetImageSize
        {
            get => targetImageSize; set
            {
                targetImageSize = value;
                OnPropertyChanged();
            }
        }

        #region Progress indicator
        private bool showProgressIndicator = false;
        public bool ShowProgressIndicator
        {
            get => showProgressIndicator;
            private set
            {
                showProgressIndicator = value;
                OnPropertyChanged();
            }
        }

        private int progressIndicatorMax = 0;
        public int ProgressIndicatorMax
        {
            get => progressIndicatorMax;
            private set
            {
                progressIndicatorMax = value;
                OnPropertyChanged();
            }
        }

        private int progressIndicatorProgress = 0;
        public int ProgressIndicatorProgress
        {
            get => progressIndicatorProgress;
            private set
            {
                progressIndicatorProgress = value;
                OnPropertyChanged();
            }
        }

        #endregion

        private Dictionary<string, int> TagCountDictionary = new Dictionary<string, int>();


        public void UpdateTagCounts()
        {
            if (ImagesWithTags != null)
            {
                TagCounts = new ObservableCollection<TagWithCountViewModel>(ImagesWithTags.SelectMany(i => i.Tags.Select(t => t.Tag).Where(t => t != ""))
                        .GroupBy(t => t)
                        .Select(pair => new TagWithCountViewModel(this)
                        {
                            Tag = pair.Key,
                            Count = pair.Count()
                        }).OrderBy(tc => tc.Tag));

                tagCache = selectedImage?.Tags.Select(t => t.Tag).ToList();
            }
        }

        public void MoveTagLeft(TagViewModel tag)
        {
            if (SelectedImage != null)
            {
                var index = SelectedImage.Tags.IndexOf(tag);
                if (index > 0)
                {
                    SelectedImage.RemoveTagAt(index);
                    SelectedImage.InsertTag(index - 1, tag);
                    FocusTagCallback?.Invoke(tag);
                }
            }

        }

        public void MoveTagRight(TagViewModel tag)
        {
            if (SelectedImage != null)
            {
                var index = SelectedImage.Tags.IndexOf(tag);
                if (index < SelectedImage.Tags.Count() - 1)
                {
                    SelectedImage.RemoveTagAt(index);
                    SelectedImage.InsertTag(index + 1, tag);
                    FocusTagCallback?.Invoke(tag);
                }
            }
        }

        [RelayCommand]
        public async Task ArchiveSelectedImage()
        {
            if (SelectedImage != null && ImagesWithTags != null)
            {
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxStandardWindow("Archive Image?",
                                                         "Really archive selected image? It will be moved to a subdirectory named archive.",
                                                         ButtonEnum.YesNo,
                                                         Icon.Warning);

                var result = await ShowDialog(messageBoxStandardWindow);

                if (result == ButtonResult.Yes)
                {
                    var imageToDelete = SelectedImage;
                    var index = ImagesWithTags.IndexOf(imageToDelete);
                    ImagesWithTags.Remove(imageToDelete);
                    if (ImagesWithTags.Count > 0)
                    {
                        if (index < ImagesWithTags.Count())
                        {
                            SelectedImage = ImagesWithTags[index];
                        }
                        else
                        {
                            SelectedImage = ImagesWithTags.Last();
                        }
                    }
                    else
                    {
                        SelectedImage = null;
                    }

                    var destDirectory = Path.Combine(openFolder, ARCHIVE_FOLDER);
                    if (!Directory.Exists(destDirectory))
                    {
                        Directory.CreateDirectory(destDirectory);
                    }


                    var destFile = Path.Combine(destDirectory, imageToDelete.Filename);
                    var destFileWithoutExtension = Path.Combine(destDirectory, Path.GetFileNameWithoutExtension(imageToDelete.Filename));
                    var i = 0;
                    while (File.Exists(destFile))
                    {
                        destFileWithoutExtension = Path.Combine(destDirectory, $"{Path.GetFileNameWithoutExtension(imageToDelete.Filename)}_{i.ToString("00")}");
                        destFile = $"{destFileWithoutExtension}{Path.GetExtension(imageToDelete.Filename)}";
                    }
                    File.Move(Path.Combine(openFolder, imageToDelete.Filename), destFile);
                    var tagFile = Path.Combine(openFolder, imageToDelete.GetTagsFileName());
                    if (File.Exists(tagFile))
                    {
                        File.Move(tagFile, Path.Combine(destDirectory, $"{destFileWithoutExtension}.txt"));
                    }
                }
            }
        }

        [RelayCommand]
        public async Task RunImgToImg(Bitmap image)
        {
            if (SelectedImage != null)
            {
                var dialog = new ImageTouchupDialog();

                dialog.Tags = SelectedImage.Tags.Select(t => t.Tag).ToList();
                dialog.Image = image;
                dialog.OpenProject = this.openProject;
                await ShowDialog(dialog);

                if (dialog.Success)
                {
                    SelectedImage.ImageSource = dialog.Image;
                    SelectedImage.ImageSource.Save(Path.Combine(this.openFolder, SelectedImage.Filename));
                }
            }
        }

        [RelayCommand]
        public async void ApplicationSettings()
        {
            var dialog = new SettingsDialog();

            await ShowDialog(dialog);
        }

        [RelayCommand]
        public async void ProjectSettings()
        {
            if (openProject != null)
            {
                var dialog = new ProjectSettingsDialog();

                dialog.Project = openProject;

                await ShowDialog(dialog);
            }
        }

        [RelayCommand]
        public async void Exit()
        {
            if (await CheckCanExit())
            {
                ExitCallback?.Invoke();
            }
        }

        [RelayCommand]
        public async void ClearTags()
        {
            if (SelectedImage.Tags.Any())
            {
                var dialog = MessageBox.Avalonia.MessageBoxManager
                                    .GetMessageBoxStandardWindow("Delete all tags",
                                                                 "This will delete all tags for the current image, are you sure?",
                                                                 ButtonEnum.YesNo,
                                                                 Icon.Warning);

                var result = await ShowDialog(dialog);

                if (result == ButtonResult.Yes)
                {
                    SelectedImage.ClearTags();
                }
            }
        }

        public async Task<bool> CheckCanExit()
        {
            if (ImagesWithTags != null)
            {
                var dirtyImages = ImagesWithTags.Where(iwt => iwt.IsImageDirty());

                var dirtyTags = ImagesWithTags.Where(iwt => iwt.AreTagsDirty());

                if (dirtyImages.Any() || dirtyTags.Any())
                {
                    var dialog = MessageBox.Avalonia.MessageBoxManager
                                    .GetMessageBoxStandardWindow("Unsaved Changes",
                                                                 "You have unsaved changes, do you wish to save them now?",
                                                                 ButtonEnum.YesNoCancel,
                                                                 Icon.Warning);

                    var result = (await ShowDialog(dialog));
                    if (result == ButtonResult.Yes)
                    {
                        SaveChanges();
                    }

                    if (result == ButtonResult.Cancel)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        #region Image Interrogation
        public async Task InterrogateAndApplyToSelectedImage(Bitmap bitmap)
        {
            //Cache the selected image in case it's changed during async operation
            var selectedImage = SelectedImage;
            var tags = await Interrogate(bitmap);

            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    selectedImage.AddTagIfNotExists(tag);
                }
            }
        }
        public async Task<IEnumerable<TagViewModel>?> Interrogate(Bitmap image)
        {
            var api = new DefaultApi(App.Settings.WebUiAddress);

            var model = "deepdanbooru";

            if (openProject != null && openProject.InterrogateMethod == SdWebUpApi.InterrogateMethod.Clip)
            {
                model = "clip";
            }
            using var uploadStream = new MemoryStream();
            image.Save(uploadStream);
            var imagebase64 = Convert.ToBase64String(uploadStream.ToArray());

            try
            {
                var result = await api.InterrogateapiSdapiV1InterrogatePostAsync(new SdWebUpApi.Model.InterrogateRequest
                {
                    Image = imagebase64,
                    Model = model
                });

                var jtokResult = result as JToken;
                var convertedresult = jtokResult?.ToObject<InterrogateResult>();
                if (convertedresult != null)
                {
                    return convertedresult.caption.Split(", ")
                                                  .Select(t => new TagViewModel(t));
                }
            }
            catch (Exception ex)
            {
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
                        .GetMessageBoxStandardWindow("Interrogate Failed",
                                                     $"Failed to interrogate the image. This likely means the stable diffusion webui server can't be reached. Error message: {ex.Message}",
                                                     ButtonEnum.Ok,
                                                     Icon.Warning);

                await ShowDialog(messageBoxStandardWindow);
            }

            return null;
        }

        [RelayCommand]
        public async Task InterrogateAllImages()
        {
            if (ImagesWithTags != null && ImagesWithTags.Count > 0)
            {
                ShowProgressIndicator = true;
                ProgressIndicatorMax = ImagesWithTags.Count();
                ProgressIndicatorProgress = 0;

                foreach (var image in ImagesWithTags)
                {
                    var tags = await Interrogate(image.ImageSource);

                    if (tags != null)
                    {
                        foreach (var tag in tags)
                        {
                            image.AddTagIfNotExists(tag);
                        }
                    }
                    else
                    {
                        //The interrogate failed, likely connection issue, cancel out
                        break;
                    }
                    ++ProgressIndicatorProgress;
                }

                ShowProgressIndicator = false;
            }
        }

        #endregion
        public void AddNewImage(Bitmap image, IEnumerable<string>? tags = null)
        {
            var index = ImagesWithTags.IndexOf(SelectedImage);
            var withoutExtension = Path.GetFileNameWithoutExtension(SelectedImage.Filename);
            if (SelectedImage.FirstNumberedChunk != -1)
            {
                withoutExtension = SelectedImage.FirstNumberedChunk.ToString("00000");
            }

            int i = 0;
            if (SelectedImage.SecondNumberedChunk != -1)
            {
                i = SelectedImage.SecondNumberedChunk;
            }

            var newFilename = $"{withoutExtension}__{i:00}";
            while (ImagesWithTags.Any(i => newFilename == Path.GetFileNameWithoutExtension(i.Filename)))
            {
                newFilename = $"{withoutExtension}__{++i:00}";
            }
            newFilename = Path.Combine(openFolder, $"{newFilename}.png");
            image.Save(newFilename);
            var newImageViewModel = new ImageWithTagsViewModel(image, newFilename, ImageDirtyHandler, tags);
            if (tags != null)
            {
                var set = new TagSet(Path.Combine(openFolder, newImageViewModel.GetTagsFileName()), tags);
                set.WriteFile();
            }
            ImagesWithTags.Insert(index + 1, newImageViewModel);
        }

        public void ImageCropped(Bitmap image)
        {
            AddNewImage(image, SelectedImage.Tags.Select(t => t.Tag));
        }

        public async Task ReviewComicPanels(List<Bitmap> panels)
        {
            ImageReviewDialog dialog = new ImageReviewDialog();
            dialog.Images = panels.Select(p => new ImageReviewViewModel(p)).ToObservableCollection();
            dialog.ReviewMode = ImageReviewDialogMode.MultiSelect;
            dialog.Title = "Select Images to Keep";

            await ShowDialog(dialog);

            if (dialog.Success)
            {
                foreach (var image in dialog.SelectedImages)
                {
                    AddNewImage(image);
                }
            }
        }

        public Task SaveCurrentImage(Bitmap image)
        {
            if (openFolder != null && SelectedImage != null)
            {
                if (Path.GetExtension(SelectedImage.Filename) != ".png")
                {
                    image.Save(Path.Combine(openFolder, SelectedImage.Filename));
                    SelectedImage.Filename = $"{Path.GetFileNameWithoutExtension(SelectedImage.Filename)}.png";
                }
                image.Save(Path.Combine(openFolder, SelectedImage.Filename));
                SelectedImage.ImageSource = image;
                SelectedImage.UpdateThumbnail();
            }

            return Task.CompletedTask;
        }

        public async Task ExpandImage(Bitmap image)
        {
            var dialog = new ExpandImageDialog();
            if (openProject != null && openProject.TargetImageSize.HasValue && openProject.TargetImageSize.Value.Width > 0 && openProject.TargetImageSize.Value.Height > 0)
            {
                dialog.ComputeExpansionNeededForTargetAspectRatio(image.PixelSize.Width, image.PixelSize.Height, openProject.TargetImageSize.Value.Width, openProject.TargetImageSize.Value.Height);
            }
            await ShowDialog(dialog);
            if (dialog.Success)
            {
                var finalSize = new PixelSize(image.PixelSize.Width + dialog.ExpandLeft + dialog.ExpandRight, image.PixelSize.Height + dialog.ExpandUp + dialog.ExpandDown);
                var imageRegion = new Rect(dialog.ExpandLeft, dialog.ExpandUp, image.PixelSize.Width, image.PixelSize.Height);
                var newImage = new RenderTargetBitmap(finalSize);
                using (var drawingContext = newImage.CreateDrawingContext(null))
                {
                    drawingContext.Clear(new Color(255, 255, 255, 255));
                    var dc = new DrawingContext(drawingContext);

                    dc.DrawImage(image, new Rect(0, 0, image.PixelSize.Width, image.PixelSize.Height), imageRegion, Avalonia.Visuals.Media.Imaging.BitmapInterpolationMode.HighQuality);
                }

                AddNewImage(newImage, SelectedImage.Tags.Select(t => t.Tag));
            }
        }

        public bool ImageDirtyHandler(Bitmap image)
        {
            return ImageDirtyCallback?.Invoke(image) ?? false;
        }


        #region Tag Priority Sets
        private ObservableCollection<TagPrioritySetButtonViewModel>? tagPrioritySets;
        public ObservableCollection<TagPrioritySetButtonViewModel>? TagPrioritySets
        {
            get => tagPrioritySets;
            set
            {
                tagPrioritySets = value;
                OnPropertyChanged();
            }
        }

        [RelayCommand]
        public void ApplyTagPrioritySet(TagPrioritySetButtonViewModel buttonVM)
        {
            if (tagPrioritySets != null && tagPrioritySets.Any() && SelectedImage != null)
            {
                SelectedImage.ApplyTagOrdering(t => buttonVM.PrioritySet.GetTagPriority(t));
            }
        }

        [RelayCommand]
        public async Task EditTagPrioritySets()
        {
            var dialog = new TagPrioritySetManagerDialog();

            await ShowDialog(dialog);

            if (dialog.Success)
            {
                UpdateTagPrioritySets();
            }
        }

        public void UpdateTagPrioritySets()
        {
            if (Directory.Exists(TAG_PRIORITY_SETS_FOLDER))
            {
                var txts = Directory.EnumerateFiles(TAG_PRIORITY_SETS_FOLDER, "*.txt").ToList();
                TagPrioritySets = txts.Select(filename =>
                                                new TagPrioritySetButtonViewModel(
                                                        Path.GetFileNameWithoutExtension(filename), 
                                                        new TagPrioritySet(filename)))
                                  .ToObservableCollection();
            }
        }
        #endregion
    }
}