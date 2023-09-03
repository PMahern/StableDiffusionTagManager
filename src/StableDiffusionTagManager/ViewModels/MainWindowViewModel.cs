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
using System.Threading;
using MessageBox.Avalonia.Enums;
using StableDiffusionTagManager.Views;
using MessageBox.Avalonia.BaseWindows.Base;
using Avalonia.Controls;
using SdWebUpApi.Api;
using Newtonsoft.Json.Linq;
using StableDiffusionTagManager.Extensions;
using Avalonia.Media;

namespace StableDiffusionTagManager.ViewModels
{
    public class TagModel
    {
        public string Tag { get; set; } = "";
        public int Count { get; set; } = 0;
    }

    public partial class MainWindowViewModel : ViewModelBase
    {
        private static readonly string TagsPath = "tags.csv";
        private static readonly string ProjectFolder = ".sdtmproj";
        private static readonly string ProjecFilename = "_project.xml";
        private static readonly string ArchiveFolder = "archive";
        private static readonly string TagPrioritySets = "TagPrioritySets";

        private List<TagModel> _tagDictionary = new List<TagModel>();

        public MainWindowViewModel()
        {
            if (File.Exists(TagsPath))
            {
                _tagDictionary = File.ReadAllLines(TagsPath)
                                    .Select(line =>
                                    {
                                        var pair = line.Split(',');
                                        return new TagModel
                                        {
                                            Tag = pair[0],
                                            Count = int.Parse(pair[1])
                                        };
                                    }).ToList();
            }

            if(Directory.Exists(TagPrioritySets))
            {
                var txts = Directory.EnumerateFiles(TagPrioritySets, "*.txt").ToList();
                tagPrioritySets = txts.Select(filename => new TagPrioritySet(filename)).ToList();
            }
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

        public MessageBoxDialogHandler? ShowDialogHandler { get; set; }

        public Func<Task<string?>>? ShowFolderDialogCallback { get; set; }

        public Func<string?, Task<string?>>? ShowAddTagToAllDialogCallback { get; set; }

        public Action<TagViewModel>? FocusTagCallback { get; set; }

        public Action? ExitCallback { get; set; }

        public Func<Bitmap, bool>? ImageDirtyCallback { get; set; }

        private Task<TResult> ShowDialog<TResult>(IMsBoxWindow<TResult> mbox) where TResult : struct
        {
            if (ShowDialogHandler != null)
            {
                return ShowDialogHandler.ShowDialog(mbox);
            }
            return Task.FromResult(default(TResult));
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
                    var projectPath = Path.Combine(pickResult, ProjectFolder);
                    var projFolder = Directory.Exists(projectPath);
                    var projFile = Path.Combine(projectPath, ProjecFilename);
                    if (!projFolder)
                    {
                        var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxStandardWindow("Create Project?", "It appears you haven't created a project for this folder. Creating a project will back up all the current existing images and tag sets into a folder named .sdtmproj so you can restore them and will also let you set some properties on the project. Would you like to create a project now?", MessageBox.Avalonia.Enums.ButtonEnum.YesNo, Icon.Question);
                        if ((await ShowDialog(messageBoxStandardWindow)) == MessageBox.Avalonia.Enums.ButtonResult.Yes)
                        {

                            projFolder = true;

                            messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
                                .GetMessageBoxStandardWindow("Rename Images?", "You can optionally rename all the images to have an increasing index instead of the current existing pattern.  Would you like to do this now?", MessageBox.Avalonia.Enums.ButtonEnum.YesNo, Icon.Question);

                            var renameImages = (await ShowDialog(messageBoxStandardWindow) == MessageBox.Avalonia.Enums.ButtonResult.Yes);

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
                                    File.Move(file, Path.Combine(projectPath, System.IO.Path.GetFileName(file)));
                                }

                                var movedjpegs = Directory.EnumerateFiles(projectPath, "*.jpg").ToList();
                                var movedpngs = Directory.EnumerateFiles(projectPath, "*.png").ToList();
                                var movedtxts = Directory.EnumerateFiles(projectPath, "*.txt").ToList();
                                var imagesToCopy = movedjpegs.Concat(movedpngs).ToList();

                                int i = 1;
                                foreach (var image in imagesToCopy)
                                {
                                    var filename = i.ToString("00000");
                                    var newFileName = Path.Combine(pickResult, $"{filename}{System.IO.Path.GetExtension(image)}");
                                    File.Copy(image, newFileName);
                                    var matchingTagFile = movedtxts.FirstOrDefault(f => System.IO.Path.GetFileNameWithoutExtension(f) == System.IO.Path.GetFileNameWithoutExtension(image));
                                    if (matchingTagFile != null)
                                    {
                                        File.Copy(matchingTagFile, Path.Combine(pickResult, $"{filename}.txt"));
                                    }
                                    project.AddBackedUpFileMap(System.IO.Path.GetFileName(image), System.IO.Path.GetFileName(newFileName));

                                    ++i;
                                }
                                project.Save();
                            }
                            else
                            {
                                foreach (var file in all)
                                {
                                    File.Copy(file, Path.Combine(projectPath, System.IO.Path.GetFileName(file)));
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

        public Task<IEnumerable<object>> SearchTags(string text, CancellationToken token)
        {
            return Task.FromResult(this.ImagesWithTags?.SelectMany(iwt => iwt.Tags.Where(a => a.Tag.StartsWith(text)).Select(a => a.Tag))
                                       .GroupBy(tag => tag)
                                       .OrderByDescending(t => t.Count())
                                       .Select(t => t.Key)
                                       .Union(_tagDictionary.Where(bt => bt.Tag.StartsWith(text)).Select(bt => bt.Tag)) ??
                                       Enumerable.Empty<object>());
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
            if (ShowAddTagToAllDialogCallback != null && ImagesWithTags != null)
            {
                var tagResult = await ShowAddTagToAllDialogCallback(null);

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
            if (ShowAddTagToAllDialogCallback != null && ImagesWithTags != null)
            {
                var tagResult = await ShowAddTagToAllDialogCallback(null);

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
            if (ShowAddTagToAllDialogCallback != null && ImagesWithTags != null)
            {
                var tagResult = await ShowAddTagToAllDialogCallback(null);

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
        public async Task DeleteSelectedImage()
        {
            if (SelectedImage != null && ImagesWithTags != null)
            {
                var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxStandardWindow("Archive Image?", "Really archive selected image? It will be moved to a subdirectory named archive.", MessageBox.Avalonia.Enums.ButtonEnum.YesNo, Icon.Warning);

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

                    var destDirectory = Path.Combine(openFolder, ArchiveFolder);
                    if (!Directory.Exists(destDirectory))
                    {
                        Directory.CreateDirectory(destDirectory);
                    }

                    File.Move(Path.Combine(openFolder, imageToDelete.Filename), Path.Combine(destDirectory, imageToDelete.Filename));
                    var tagFile = Path.Combine(openFolder, imageToDelete.GetTagsFileName());
                    if (File.Exists(tagFile))
                    {
                        File.Move(tagFile, Path.Combine(destDirectory, imageToDelete.GetTagsFileName()));
                    }
                }
            }
        }

        [RelayCommand]
        public async Task RunImgToImg(Bitmap image)
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
                                    .GetMessageBoxStandardWindow("Delete all tags", "This will delete all tags for the current image, are you sure?", MessageBox.Avalonia.Enums.ButtonEnum.YesNo, Icon.Warning);

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
                                    .GetMessageBoxStandardWindow("Unsaved Changes", "You have unsaved changes, do you wish to save them now?", MessageBox.Avalonia.Enums.ButtonEnum.YesNoCancel, Icon.Warning);

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

        public async Task Interrogate(Bitmap image)
        {
            var api = new DefaultApi(App.Settings.WebUiAddress);

            if (SelectedImage != null)
            {
                var model = "deepdanbooru";

                if (openProject != null && openProject.InterrogateMethod == SdWebUpApi.InterrogateMethod.Clip)
                {
                    model = "clip";
                }
                using var uploadStream = new MemoryStream();
                SelectedImage.ImageSource.Save(uploadStream);
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
                        var tags = convertedresult.caption.Split(", ");
                        foreach (var tag in tags)
                        {
                            if (!SelectedImage.Tags.Any(t => t.Tag == tag))
                            {
                                SelectedImage.AddTag(new TagViewModel(tag));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager
                            .GetMessageBoxStandardWindow("Interrogate Failed",
                                                         $"Failed to interrogate the image. This likely means the stable diffusion webui server can't be reached. Error message: {ex.Message}", ButtonEnum.Ok, Icon.Warning);

                    await ShowDialog(messageBoxStandardWindow);
                }
            }
        }

        public void AddNewImage(Bitmap image, IEnumerable<string>? tags = null)
        {
            var index = ImagesWithTags.IndexOf(SelectedImage);
            var withoutExtension = System.IO.Path.GetFileNameWithoutExtension(SelectedImage.Filename);
            if(SelectedImage.FirstNumberedChunk != -1)
            {
                withoutExtension = SelectedImage.FirstNumberedChunk.ToString("00000");
            }

            int i = 0;
            if(SelectedImage.SecondNumberedChunk != -1)
            {
                i = SelectedImage.SecondNumberedChunk;
            }

            var newFilename = $"{withoutExtension}__{i.ToString("00")}";
            while (ImagesWithTags.Any(i => newFilename == System.IO.Path.GetFileNameWithoutExtension(i.Filename)))
            {
                newFilename = $"{withoutExtension}__{(++i).ToString("00")}";
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
        private List<TagPrioritySet>? tagPrioritySets;

        [RelayCommand]
        public void ApplyTagPrioritySet()
        {
            if(tagPrioritySets != null && tagPrioritySets.Any() && SelectedImage != null)
            {
                SelectedImage.ApplyTagOrdering(t => tagPrioritySets.First().GetTagPriority(t));
            }
        }
        #endregion
    }
}