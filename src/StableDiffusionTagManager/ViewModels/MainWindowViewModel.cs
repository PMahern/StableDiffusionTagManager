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
using StableDiffusionTagManager.Views;
using StableDiffusionTagManager.Extensions;
using Avalonia.Media;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Collections.Specialized;
using ImageUtil;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using StableDiffusionTagManager.Collections;
using Avalonia.Threading;
using StableDiffusionTagManager.Services;
using ImageUtil.Interrogation;
using Avalonia.Controls;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private static readonly string BACKUP_FOLDER_NAME = ".sdtmproj";
        private static readonly string PROJECT_FILE_NAME = "_project.xml";
        private static readonly string ARCHIVE_FOLDER = "archive";
        private static readonly string TAG_PRIORITY_SETS_FOLDER = "TagPrioritySets";
        private readonly ViewModelFactory viewModelFactory;
        private readonly ComicPanelExtractor comicPanelExtractorService;
        private readonly Settings settings;
        private readonly DialogHandler dialogHandler;
        private readonly WebApiFactory webApiFactory;
        private readonly ICurrentProjectDefaults currentProjectDefaults;

        public BatchQueueViewModel BatchQueue { get; } = new BatchQueueViewModel();

        public MainWindowViewModel(ViewModelFactory viewModelFactory,
            ComicPanelExtractor comicPanelExtractorService,
            Settings settings,
            DialogHandler dialogHandler,
            WebApiFactory webApiFactory,
            ICurrentProjectDefaults currentProjectDefaults)
        {
            UpdateTagPrioritySets();
            this.settings = settings;
            this.dialogHandler = dialogHandler;
            this.webApiFactory = webApiFactory;
            this.viewModelFactory = viewModelFactory;
            this.comicPanelExtractorService = comicPanelExtractorService;
            this.currentProjectDefaults = currentProjectDefaults;
            UpdateImageAspectRatioSets();

        }

        private void UpdateImageAspectRatioSets()
        {
            this.ImageAspectRatioSets = settings.ImageAspectRatioSets.Select(t => new ImageAspectRatioSet()
            {
                Name = t.Item1,
                Resolutions = t.Item2.Select(r => new ImageResolution() { X = r.Item1, Y = r.Item2 }).ToObservableCollection()
            }).ToObservableCollection();
            this.ImageAspectRatioSets.Insert(0, null);
        }

        private List<string>? _CopiedTags = null;

        private List<string>? CopiedTags
        {
            get => _CopiedTags;
            set
            {
                _CopiedTags = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasCopiedTags));
            }
        }

        public bool HasCopiedTags
        {
            get
            {
                return _CopiedTags != null && _CopiedTags.Any();
            }
        }

        private ObservableCollection<ImageWithTagsViewModel>? imageWithTagsViewModel;
        public ObservableCollection<ImageWithTagsViewModel>? ImagesWithTags
        {
            get => imageWithTagsViewModel;
            set
            {
                if (imageWithTagsViewModel != null)
                {
                    imageWithTagsViewModel.CollectionChanged -= ImageWithTagsViewModel_CollectionChanged;
                }
                imageWithTagsViewModel = value;
                if (imageWithTagsViewModel != null)
                {
                    imageWithTagsViewModel.CollectionChanged += ImageWithTagsViewModel_CollectionChanged;
                }
                RebuildFilteredImageSet();
                OnPropertyChanged();
                OnPropertyChanged(nameof(ImagesLoaded));
                OnPropertyChanged(nameof(AnyImagesLoaded));
                AddTagToEndOfAllImagesCommand.NotifyCanExecuteChanged();
                AddTagToStartOfAllImagesCommand.NotifyCanExecuteChanged();
                RemoveTagFromAllImagesCommand.NotifyCanExecuteChanged();
                RemoveMetaTagsCommand.NotifyCanExecuteChanged();
                InterrogateAllImagesCommand.NotifyCanExecuteChanged();
            }
        }

        public ObservableCollection<ImageWithTagsViewModel>? filteredImageSet;
        public ObservableCollection<ImageWithTagsViewModel>? FilteredImageSet
        {
            get => filteredImageSet;
            set
            {
                filteredImageSet = value;
                OnPropertyChanged();
            }
        }

        private void ImageWithTagsViewModel_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildFilteredImageSet();
        }

        private void RebuildFilteredImageSet()
        {
            if (this.imageWithTagsViewModel != null)
            {
                this.FilteredImageSet = new ObservableCollection<ImageWithTagsViewModel>(this.imageWithTagsViewModel.Where(PassesCurrentFilter));
            }
            else
            {
                this.FilteredImageSet = null;
            }
        }

        public bool PassesCurrentFilter(ImageWithTagsViewModel imageWithTagsViewModel)
        {
            switch (currentImageFilterMode)
            {
                case ImageFilterMode.None:
                    return true;
                case ImageFilterMode.NotCompleted:
                    return !imageWithTagsViewModel.IsComplete;
                case ImageFilterMode.Completed:
                    return imageWithTagsViewModel.IsComplete;
            }

            return false;
        }

        ImageWithTagsViewModel? selectedImage;
        public ImageWithTagsViewModel? SelectedImage
        {
            get => selectedImage;
            set
            {
                if (selectedImage != value)
                {
                    if (selectedImage != null)
                        selectedImage.PropertyChanged -= OnSelectedImagePropertyChanged;

                    selectedImage = value;

                    if (selectedImage != null)
                        selectedImage.PropertyChanged += OnSelectedImagePropertyChanged;

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsImageSelected));
                    OnPropertyChanged(nameof(IsSelectedImageEditable));
                    OnPropertyChanged(nameof(SelectedImageHasPendingOperation));
                    AddTagToEndCommand?.NotifyCanExecuteChanged();
                    AddTagToFrontCommand?.NotifyCanExecuteChanged();
                }
            }
        }

        private void OnSelectedImagePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImageWithTagsViewModel.HasPendingOperation))
            {
                OnPropertyChanged(nameof(IsSelectedImageEditable));
                OnPropertyChanged(nameof(SelectedImageHasPendingOperation));
            }
        }

        /// <summary>True when an image is selected and has no pending batch operation.</summary>
        public bool IsSelectedImageEditable => SelectedImage != null && !SelectedImage.HasPendingOperation;

        /// <summary>True only when an image is selected AND it has a pending batch operation.</summary>
        public bool SelectedImageHasPendingOperation => SelectedImage?.HasPendingOperation ?? false;



        [ObservableProperty]
        private ObservableCollection<ImageAspectRatioSet?> imageAspectRatioSets;

        [ObservableProperty]
        private ImageAspectRatioSet? selectedImageAspectRatioSet;

        [ObservableProperty]
        private Resampler selectedImageResampler = Resampler.Lanczos8;

        #region Tag Collections
        private TagCollectionViewModel? editTagCollectionTarget;

        public TagCollectionViewModel? EditTagCollectionTarget
        {
            get => editTagCollectionTarget;
            set
            {
                var old = editTagCollectionTarget;
                if (SetProperty(ref editTagCollectionTarget, value))
                {
                    if (old != null)
                    {
                        old.IsSelected = false;
                    }
                    if (value != null)
                    {
                        value.IsSelected = true;
                    }
                    AddTagToCollectionCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public void RemoveTagCollection(TagCollectionViewModel collection)
        {
            if (this.TagCollections != null)
            {
                this.TagCollections.Remove(collection);
                if (this.EditTagCollectionTarget == collection)
                {
                    this.EditTagCollectionTarget = null;
                }
            }
        }

        public bool IsEditingTagCollection { get => EditTagCollectionTarget != null; }
        [ObservableProperty]
        private ObservableCollection<TagCollectionViewModel>? tagCollections;

        [RelayCommand(CanExecute = nameof(IsProject))]
        public async Task AddTagCollection()
        {
            if (OpenProject != null)
            {
                if (this.TagCollections == null)
                {
                    this.TagCollections = new ObservableCollection<TagCollectionViewModel>();
                }
                this.TagCollections.Add(new TagCollectionViewModel(this, ""));
            }
        }

        [RelayCommand(CanExecute = nameof(IsEditingTagCollection))]
        public void AddTagToCollection()
        {
            if (EditTagCollectionTarget != null)
            {
                EditTagCollectionTarget.AddEmptyTag();
            }
        }

        #endregion

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddTagCollectionCommand))]
        [NotifyPropertyChangedFor(nameof(IsProject))]
        private Project? openProject = null;

        public bool IsProject { get => OpenProject != null; }

        // Root folder of the loaded project; never changes after LoadFolder()
        private string? openFolder = null;

        // The folder images are currently loaded from (root or a selected subfolder)
        private string? currentImagesFolder = null;

        [ObservableProperty]
        private ObservableCollection<SubfolderViewModel> availableSubfolders = new ObservableCollection<SubfolderViewModel>();

        partial void OnAvailableSubfoldersChanged(ObservableCollection<SubfolderViewModel> value)
        {
            OnPropertyChanged(nameof(AnyImagesLoaded));
            InterrogateAllImagesCommand.NotifyCanExecuteChanged();
            AddTagToEndOfAllImagesCommand.NotifyCanExecuteChanged();
            AddTagToStartOfAllImagesCommand.NotifyCanExecuteChanged();
            RemoveTagFromAllImagesCommand.NotifyCanExecuteChanged();
            RemoveMetaTagsCommand.NotifyCanExecuteChanged();
        }

        private SubfolderViewModel? selectedSubfolder;
        public SubfolderViewModel? SelectedSubfolder
        {
            get => selectedSubfolder;
            set
            {
                if (selectedSubfolder != value)
                {
                    selectedSubfolder = value;
                    OnPropertyChanged();
                    _ = SwitchToSubfolder(value);
                }
            }
        }

        /// <summary>
        /// The project file owned directly by the current folder.
        /// Null when viewing a subfolder that has no _project.xml yet.
        /// </summary>
        public Project? CurrentFolderProject =>
            (currentImagesFolder == null || string.Equals(currentImagesFolder, openFolder, StringComparison.OrdinalIgnoreCase))
                ? OpenProject
                : selectedSubfolder?.SubProject;

        /// <summary>
        /// Walk the folder ancestry from root down to the current folder, yielding
        /// each level's own project (null for levels with no _project.xml).
        /// </summary>
        private IEnumerable<Project?> GetAncestryProjects()
        {
            if (openFolder == null || currentImagesFolder == null) yield break;

            // Build ordered chain: root first, current folder last
            var chain = new List<string>();
            var path = currentImagesFolder;
            while (path != null)
            {
                chain.Insert(0, path);
                if (string.Equals(path, openFolder, StringComparison.OrdinalIgnoreCase)) break;
                path = Path.GetDirectoryName(path);
            }

            foreach (var folder in chain)
            {
                if (string.Equals(folder, openFolder, StringComparison.OrdinalIgnoreCase))
                    yield return OpenProject;
                else
                {
                    var sf = AvailableSubfolders.FirstOrDefault(s =>
                        string.Equals(s.FolderPath, folder, StringComparison.OrdinalIgnoreCase));
                    yield return sf?.SubProject;
                }
            }
        }

        /// <summary>Returns the deepest (most-specific) non-null value for a reference-type project setting.</summary>
        private T? GetEffectiveSetting<T>(Func<Project, T?> selector) where T : class
        {
            T? result = null;
            foreach (var p in GetAncestryProjects())
                if (p != null) { var v = selector(p); if (v != null) result = v; }
            return result;
        }

        /// <summary>Returns the deepest (most-specific) non-null value for a value-type project setting.</summary>
        private T? GetEffectiveSettingValue<T>(Func<Project, T?> selector) where T : struct
        {
            T? result = null;
            foreach (var p in GetAncestryProjects())
                if (p != null) { var v = selector(p); if (v.HasValue) result = v; }
            return result;
        }

        /// <summary>
        /// Returns the deepest non-null value for a reference-type setting resolved for
        /// an arbitrary <paramref name="folderPath"/> instead of the currently loaded folder.
        /// </summary>
        private T? GetEffectiveSettingForFolder<T>(string folderPath, Func<Project, T?> selector) where T : class
        {
            if (openFolder == null) return null;
            var chain = BuildFolderChain(folderPath);
            T? result = null;
            foreach (var folder in chain)
            {
                var project = GetProjectForFolder(folder);
                if (project != null) { var v = selector(project); if (v != null) result = v; }
            }
            return result;
        }

        /// <summary>
        /// Returns merged concepts for an arbitrary <paramref name="folderPath"/>,
        /// with deeper folders overriding ancestor values for the same key.
        /// </summary>
        private Dictionary<string, string> GetEffectiveConceptsForFolder(string folderPath)
        {
            var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (openFolder == null) return merged;
            foreach (var folder in BuildFolderChain(folderPath))
            {
                var project = GetProjectForFolder(folder);
                if (project != null)
                    foreach (var kv in project.Concepts)
                        merged[kv.Key] = kv.Value;
            }
            return merged;
        }

        /// <summary>Builds the ancestry chain from root down to <paramref name="folderPath"/>.</summary>
        private List<string> BuildFolderChain(string folderPath)
        {
            var chain = new List<string>();
            var path = folderPath;
            while (path != null)
            {
                chain.Insert(0, path);
                if (string.Equals(path, openFolder, StringComparison.OrdinalIgnoreCase)) break;
                path = Path.GetDirectoryName(path);
            }
            return chain;
        }

        /// <summary>Returns the Project for a specific folder (root or subfolder), or null.</summary>
        private Project? GetProjectForFolder(string folder)
        {
            if (string.Equals(folder, openFolder, StringComparison.OrdinalIgnoreCase))
                return OpenProject;
            var sf = AvailableSubfolders?.FirstOrDefault(s =>
                string.Equals(s.FolderPath, folder, StringComparison.OrdinalIgnoreCase));
            return sf?.SubProject;
        }

        /// <summary>
        /// Applies concept key/value substitution to a prompt string.
        /// Replaces <c>{{key}}</c> tokens with their concept values.
        /// </summary>
        private static string ApplyConceptSubstitution(string prompt, Dictionary<string, string> concepts)
        {
            foreach (var kv in concepts)
                prompt = prompt.Replace("{{" + kv.Key + "}}", kv.Value, StringComparison.OrdinalIgnoreCase);
            return prompt;
        }

        /// <summary>
        /// Accumulates response strip pairs from root down to <paramref name="folderPath"/>,
        /// so parent folders can define global rules and child folders add more.
        /// </summary>
        private List<(string Open, string Close)> GetEffectiveStripPairsForFolder(string folderPath)
        {
            var result = new List<(string Open, string Close)>();
            if (openFolder == null) return result;
            foreach (var folder in BuildFolderChain(folderPath))
            {
                var project = GetProjectForFolder(folder);
                if (project != null)
                    result.AddRange(project.ResponseStripPairs);
            }
            return result;
        }

        /// <summary>
        /// Removes paired tag content (e.g. thinking blocks) from a string.
        /// For each pair, removes the open tag, everything between it and the matching
        /// close tag, and the close tag. Orphaned open or close tags are also removed.
        /// </summary>
        private static string ApplyResponseStripping(string text, List<(string Open, string Close)> pairs)
        {
            foreach (var (open, close) in pairs)
            {
                if (string.IsNullOrEmpty(open)) continue;
                // Remove paired spans first
                int start;
                while ((start = text.IndexOf(open, StringComparison.Ordinal)) >= 0)
                {
                    if (!string.IsNullOrEmpty(close))
                    {
                        var end = text.IndexOf(close, start + open.Length, StringComparison.Ordinal);
                        if (end >= 0)
                        {
                            text = text.Remove(start, end + close.Length - start);
                            continue;
                        }
                    }
                    // No matching close found — just remove the open tag
                    text = text.Remove(start, open.Length);
                }
                // Remove any orphaned close tags
                if (!string.IsNullOrEmpty(close))
                    text = text.Replace(close, string.Empty, StringComparison.Ordinal);
            }
            return text.Trim();
        }

        /// <summary>
        /// Applies response stripping to a tag list by joining, stripping, then re-splitting.
        /// This correctly handles thinking blocks that span comma boundaries.
        /// </summary>
        private static List<string> ApplyResponseStrippingToTags(List<string> tags, List<(string Open, string Close)> pairs)
        {
            if (pairs.Count == 0) return tags;
            var joined = string.Join(", ", tags);
            var stripped = ApplyResponseStripping(joined, pairs);
            return stripped.Split(',')
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();
        }

        /// <summary>
        /// Enumerates every (folderPath, image, isCurrentFolder) triple across the entire
        /// project tree. Images in the currently-loaded folder come from the in-memory
        /// <see cref="ImagesWithTags"/> collection; images in all other folders are loaded
        /// fresh from disk and must be saved explicitly after modification.
        /// </summary>
        private IEnumerable<(string Folder, ImageWithTagsViewModel Image, bool IsCurrentFolder)> GetAllProjectImages()
        {
            if (openFolder == null) yield break;

            // Currently-loaded folder — use the in-memory collection.
            if (ImagesWithTags != null && currentImagesFolder != null)
                foreach (var img in ImagesWithTags)
                    yield return (currentImagesFolder, img, true);

            // All other subfolders — load from disk on the fly.
            if (AvailableSubfolders == null) yield break;
            foreach (var sf in AvailableSubfolders)
            {
                if (string.Equals(sf.FolderPath, currentImagesFolder, StringComparison.OrdinalIgnoreCase))
                    continue;
                FolderTagSets folderTagSets;
                try { folderTagSets = new FolderTagSets(sf.FolderPath); }
                catch { continue; }
                foreach (var (imageFile, tagSet) in folderTagSets.TagsSets)
                    yield return (sf.FolderPath, new ImageWithTagsViewModel(imageFile, tagSet, ImageDirtyHandler), false);
            }
        }

        /// <summary>Saves a single image's tags to disk immediately.</summary>
        private static void SaveImageToDisk(string folder, ImageWithTagsViewModel image)
        {
            var set = new TagSet(Path.Combine(folder, image.GetTagsFileName()), image.Description, image.Tags.Select(t => t.Tag));
            set.WriteFile();
        }

        /// <summary>
        /// Gets the current folder's own project, creating a new _project.xml for it
        /// if none exists yet. Used when editing settings or toggling completion.
        /// </summary>
        private Project GetOrCreateCurrentFolderProject()
        {
            var folder = currentImagesFolder ?? openFolder
                ?? throw new InvalidOperationException("No folder is loaded.");

            if (string.Equals(folder, openFolder, StringComparison.OrdinalIgnoreCase))
            {
                if (OpenProject == null)
                {
                    var projFile = Path.Combine(folder, PROJECT_FILE_NAME);
                    OpenProject = new Project(projFile);
                    OpenProject.ProjectUpdated = UpdateProjectSettings;
                }
                return OpenProject!;
            }

            if (selectedSubfolder != null)
            {
                if (selectedSubfolder.SubProject == null)
                {
                    var projFile = Path.Combine(folder, PROJECT_FILE_NAME);
                    var newProj = new Project(projFile);
                    newProj.ProjectUpdated = UpdateProjectSettings;
                    selectedSubfolder.SubProject = newProj;
                }
                return selectedSubfolder.SubProject!;
            }

            throw new InvalidOperationException("No subfolder is selected.");
        }

        /// <summary>
        /// Merged concepts walking from root → current folder; child keys override parents.
        /// </summary>
        public Dictionary<string, string> EffectiveConcepts
        {
            get
            {
                var merged = new Dictionary<string, string>();
                foreach (var p in GetAncestryProjects())
                    if (p != null)
                        foreach (var kv in p.Concepts)
                            merged[kv.Key] = kv.Value;
                return merged;
            }
        }

        #region Callbacks and Events

        public Func<Task<IReadOnlyList<IStorageFolder>>> ShowFolderDialogCallback { get; set; }

        public Action<TagViewModel>? FocusTagCallback { get; set; }

        public Action? ExitCallback { get; set; }

        public Func<Bitmap, bool>? ImageDirtyCallback { get; set; }
        public Func<Bitmap, bool, Bitmap>? GetModifiedImageDataCallback { get; set; }
        #endregion

        public async Task CheckForAndConvertUnspportedImageFormats(string folder)
        {

            var webps = Directory.EnumerateFiles(folder, "*.webp");

            if (webps.Any())
            {
                var messageBoxStandardWindow = MessageBoxManager
                                .GetMessageBoxStandard("Unsupported Image Formats Detected",
                                                         "This tool only supports JPG and PNG image file formats, webp images can be automatically converted and the originals moved to the archive subdirectory, would you like to convert all webp images to png now?",
                                                         ButtonEnum.YesNo,
                                                         Icon.Question);
                if ((await dialogHandler.ShowDialog(messageBoxStandardWindow)) == ButtonResult.Yes)
                {
                    var currentIndicatorStatus = ShowProgressIndicator;
                    ShowProgressIndicator = true;
                    ProgressIndicatorMax = webps.Count();
                    ProgressIndicatorProgress = 0;
                    ProgressIndicatorMessage = "Converting webps...";
                    await Task.Run(() =>
                    {
                        ShowProgressIndicator = true;
                        ProgressIndicatorMax = webps.Count();
                        ProgressIndicatorProgress = 0;
                        ProgressIndicatorMessage = "Converting webps...";
                    });

                    foreach (var webp in webps)
                    {
                        await Task.Run(() =>
                        {
                            ImageConverter.ConvertImageFileToPng(webp);
                            ArchiveImage(folder, webp);
                        });
                        ProgressIndicatorProgress++;
                    }

                    ShowProgressIndicator = currentIndicatorStatus;
                }
            }
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
                var folderPickResult = await ShowFolderDialogCallback();
                if (folderPickResult.Any())
                {
                    var pickedFolderPath = folderPickResult.First().TryGetLocalPath();
                    if (pickedFolderPath != null)
                    {
                        await LoadFolder(pickedFolderPath);
                    }
                }
            }
        }

        public async Task LoadFolder(string folder)
        {
            ShowProgressIndicator = true;
            ProgressIndicatorMax = 100;
            ProgressIndicatorProgress = 0;
            ProgressIndicatorMessage = "Loading folder...";

            await CheckForAndConvertUnspportedImageFormats(folder);

            var backupPath = Path.Combine(folder, BACKUP_FOLDER_NAME);
            var projFolder = Directory.Exists(backupPath);

            // Project file now lives directly in the folder root (_project.xml).
            // Migrate from the old location inside .sdtmproj/ if needed.
            var projFile = Path.Combine(folder, PROJECT_FILE_NAME);
            var oldProjFile = Path.Combine(backupPath, PROJECT_FILE_NAME);
            if (!File.Exists(projFile) && File.Exists(oldProjFile))
            {
                File.Move(oldProjFile, projFile);
            }

            if (!projFolder)
            {
                var messageBoxStandardWindow = MessageBoxManager
                        .GetMessageBoxStandard("Create Project?",
                                                 "It appears you haven't created a project for this folder. Creating a project will back up all the current existing images and tag sets into a folder named .sdtmproj so you can restore them and will also let you set some properties on the project. Would you like to create a project now?",
                                                 ButtonEnum.YesNo,
                                                 Icon.Question);
                if ((await dialogHandler.ShowDialog(messageBoxStandardWindow)) == ButtonResult.Yes)
                {
                    projFolder = true;

                    messageBoxStandardWindow = MessageBoxManager
                                                .GetMessageBoxStandard("Rename Images?",
                                                     "You can optionally rename all the images to have an increasing index instead of the current existing pattern.  Would you like to do this now?",
                                                     ButtonEnum.YesNo,
                                                     Icon.Question);

                    var renameImages = (await dialogHandler.ShowDialog(messageBoxStandardWindow) == ButtonResult.Yes);

                    var jpegs = Directory.EnumerateFiles(folder, "*.jpg").ToList();
                    var pngs = Directory.EnumerateFiles(folder, "*.png").ToList();
                    var txts = Directory.EnumerateFiles(folder, "*.txt").ToList();

                    Directory.CreateDirectory(backupPath);

                    var project = new Project(projFile);
                    project.ProjectUpdated = UpdateProjectSettings;

                    var all = jpegs.Concat(pngs).Concat(txts).ToList();
                    if (renameImages)
                    {
                        await Task.Run(() =>
                        {
                            ProgressIndicatorMax = all.Count();
                            ProgressIndicatorProgress = 0;
                            ProgressIndicatorMessage = "Renaming images...";
                        });

                        foreach (var file in all)
                        {
                            await Task.Run(() =>
                            {
                                File.Move(file, Path.Combine(backupPath, Path.GetFileName(file)));
                                ProgressIndicatorProgress++;
                            });
                        }

                        var movedjpegs = Directory.EnumerateFiles(backupPath, "*.jpg").ToList();
                        var movedpngs = Directory.EnumerateFiles(backupPath, "*.png").ToList();
                        var movedtxts = Directory.EnumerateFiles(backupPath, "*.txt").ToList();
                        var imagesToCopy = movedjpegs.Concat(movedpngs).ToList();

                        int i = 1;

                        await Task.Run(() =>
                        {
                            ProgressIndicatorMax = imagesToCopy.Count();
                            ProgressIndicatorProgress = 0;
                            ProgressIndicatorMessage = "Copying image backups...";
                        });

                        foreach (var image in imagesToCopy)
                        {
                            await Task.Run(() =>
                            {
                                var filename = i.ToString("00000");
                                var newFileName = Path.Combine(folder, $"{filename}{Path.GetExtension(image)}");
                                File.Copy(image, newFileName);
                                var matchingTagFile = movedtxts.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == Path.GetFileNameWithoutExtension(image));
                                if (matchingTagFile != null)
                                {
                                    File.Copy(matchingTagFile, Path.Combine(folder, $"{filename}.txt"));
                                }
                                project.AddBackedUpFileMap(Path.GetFileName(image), Path.GetFileName(newFileName));
                                ProgressIndicatorProgress++;
                            });
                            ++i;
                        }
                        project.Save();
                    }
                    else
                    {
                        await Task.Run(() =>
                        {
                            ProgressIndicatorMax = all.Count();
                            ProgressIndicatorProgress = 0;
                            ProgressIndicatorMessage = "Copying image backups...";
                        });

                        foreach (var file in all)
                        {
                            await Task.Run(() =>
                            {
                                File.Copy(file, Path.Combine(backupPath, Path.GetFileName(file)));
                                ProgressIndicatorProgress++;
                            });
                        }
                    }
                }
            }

            var FolderTagSets = new FolderTagSets(folder);

            await Task.Run(() =>
            {
                ProgressIndicatorMax = FolderTagSets.TagsSets.Count();
                ProgressIndicatorProgress = 0;
                ProgressIndicatorMessage = "Loading images...";
            });

            var accum = new List<ImageWithTagsViewModel>();
            foreach (var tagSet in FolderTagSets.TagsSets)
            {
                await Task.Run(() =>
                {
                    ProgressIndicatorProgress++;
                    accum.Add(new ImageWithTagsViewModel(tagSet.ImageFile, tagSet.TagSet, ImageDirtyHandler));
                });
            }

            ImagesWithTags = accum
                .OrderBy(iwt => iwt.FirstNumberedChunk)
                .ThenBy(iwt => iwt.SecondNumberedChunk)
                .ToObservableCollection();

            foreach (var item in ImagesWithTags)
            {
                item.TagEntered += TagAdded;
                item.TagRemoved += TagRemoved;
            }

            if (projFolder)
            {
                OpenProject = new Project(projFile);
                OpenProject.ProjectUpdated += UpdateProjectSettings;
                UpdateProjectSettings();
            }

            openFolder = folder;
            currentImagesFolder = folder;

            // Build subfolder list — scan all non-hidden subdirectories recursively.
            // Empty folders are included so they can hold their own project settings.
            var subfolderEntries = new ObservableCollection<SubfolderViewModel>();
            var rootEntry = new SubfolderViewModel("(Root)", folder, OpenProject);
            subfolderEntries.Add(rootEntry);

            foreach (var sf in ScanSubfolders(folder, folder, null))
                subfolderEntries.Add(sf);

            AvailableSubfolders = subfolderEntries;

            // Set root entry as the selected subfolder (no event side-effects; backing field only)
            selectedSubfolder = rootEntry;
            OnPropertyChanged(nameof(SelectedSubfolder));

            // Load tag collections from the current folder's own project (or root as fallback)
            var tagSource = CurrentFolderProject ?? OpenProject;
            if (tagSource != null)
            {
                TagCollections = new ObservableCollection<TagCollectionViewModel>(tagSource.TagCollections.Select(c => new TagCollectionViewModel(this, c.Name, c.Tags)));
            }

            UpdateTagCounts();

            ShowProgressIndicator = false;
        }

        private async Task SwitchToSubfolder(SubfolderViewModel? subfolder)
        {
            if (openFolder == null || subfolder == null)
                return;

            var targetFolder = subfolder.FolderPath;

            ShowProgressIndicator = true;
            ProgressIndicatorMax = 100;
            ProgressIndicatorProgress = 0;
            ProgressIndicatorMessage = "Loading folder...";

            var folderTagSets = new FolderTagSets(targetFolder);

            await Task.Run(() =>
            {
                ProgressIndicatorMax = folderTagSets.TagsSets.Count();
                ProgressIndicatorProgress = 0;
                ProgressIndicatorMessage = "Loading images...";
            });

            var accum = new List<ImageWithTagsViewModel>();
            foreach (var tagSet in folderTagSets.TagsSets)
            {
                await Task.Run(() =>
                {
                    ProgressIndicatorProgress++;
                    accum.Add(new ImageWithTagsViewModel(tagSet.ImageFile, tagSet.TagSet, ImageDirtyHandler));
                });
            }

            ImagesWithTags = accum
                .OrderBy(iwt => iwt.FirstNumberedChunk)
                .ThenBy(iwt => iwt.SecondNumberedChunk)
                .ToObservableCollection();

            foreach (var item in ImagesWithTags)
            {
                item.TagEntered += TagAdded;
                item.TagRemoved += TagRemoved;
            }

            // Re-link any pending queue items that target this folder to the freshly loaded
            // ImageWithTagsViewModel instances, so their thumbnails show the pending indicator.
            BatchQueue.SyncPendingIndicatorsForFolder(targetFolder, ImagesWithTags);

            currentImagesFolder = targetFolder;
            UpdateProjectSettings();
            OnPropertyChanged(nameof(CurrentFolderProject));
            OnPropertyChanged(nameof(EffectiveConcepts));

            var tagSource = CurrentFolderProject ?? OpenProject;
            if (tagSource != null)
            {
                TagCollections = new ObservableCollection<TagCollectionViewModel>(tagSource.TagCollections.Select(c => new TagCollectionViewModel(this, c.Name, c.Tags)));
            }

            SelectedImage = ImagesWithTags.FirstOrDefault();
            UpdateTagCounts();

            ShowProgressIndicator = false;
        }

        /// <summary>
        /// Recursively scans subdirectories under <paramref name="parentFolder"/>,
        /// skipping hidden directories and the backup folder.
        /// All non-hidden subdirs are included regardless of whether they contain images,
        /// so that "container" folders (e.g. "Characters") can hold their own settings.
        /// </summary>
        private IEnumerable<SubfolderViewModel> ScanSubfolders(string rootFolder, string parentFolder, string? relativePrefix)
        {
            var dirs = Directory.GetDirectories(parentFolder)
                .Where(d =>
                {
                    var name = Path.GetFileName(d);
                    return !string.IsNullOrEmpty(name)
                        && !name.StartsWith('.')
                        && !string.Equals(name, BACKUP_FOLDER_NAME, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(name, ARCHIVE_FOLDER, StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(d => d);

            foreach (var dir in dirs)
            {
                var name = Path.GetFileName(dir);
                var displayName = relativePrefix != null ? $"{relativePrefix}/{name}" : name;
                var projFile = Path.Combine(dir, PROJECT_FILE_NAME);
                Project? subProject = File.Exists(projFile) ? new Project(projFile) : null;

                yield return new SubfolderViewModel(displayName, dir, subProject);

                foreach (var nested in ScanSubfolders(rootFolder, dir, displayName))
                    yield return nested;
            }
        }

        private void UpdateProjectSettings()
        {
            TargetImageSize = GetEffectiveSettingValue(p => p.TargetImageSize);
            currentProjectDefaults.NaturalLanguageInterrogationPrompt = GetEffectiveSetting(p => p.DefaultNaturalLanguageInterrogationPrompt);
            currentProjectDefaults.TagInterrogationPrompt = GetEffectiveSetting(p => p.DefaultTagInterrogationPrompt);
            currentProjectDefaults.InterrogationEndpointUrl = GetEffectiveSetting(p => p.DefaultInterrogationEndpointUrl);
            var ownProject = CurrentFolderProject;
            if (ownProject != null && ImagesWithTags != null)
            {
                foreach (var image in ImagesWithTags)
                    image.IsComplete = ownProject.CompletedImages.Contains(image.Filename);
            }
        }

        [RelayCommand]
        public void SaveChanges()
        {
            if (ImagesWithTags != null && currentImagesFolder != null)
            {
                foreach (var image in ImagesWithTags)
                {
                    var set = new TagSet(Path.Combine(currentImagesFolder, image.GetTagsFileName()), image.Description, image.Tags.Select(t => t.Tag));
                    set.WriteFile();

                    image.ClearTagsDirty();

                    if (image.IsImageDirty() && GetModifiedImageDataCallback != null)
                    {
                        var newImageData = GetModifiedImageDataCallback(image.ImageSource, true);
                        SaveUpdatedImage(image, newImageData);
                    }
                }

                var ownProject = CurrentFolderProject;
                if (ownProject != null)
                {
                    if (TagCollections != null)
                    {
                        ownProject.TagCollections = TagCollections.Select(tc => new TagCollection()
                        {
                            Name = tc.Name,
                            Tags = tc.Tags.Select(t => t.Tag).ToList()
                        }).ToList();
                    }
                    else
                    {
                        ownProject.TagCollections.Clear();
                    }

                    ownProject.Save();
                }
            }
        }

        [RelayCommand(CanExecute = nameof(AnyImagesLoaded))]
        public async Task ExportToFlatFolder()
        {
            // Save any pending changes for the currently loaded folder first.
            SaveChanges();

            if (ShowFolderDialogCallback == null) return;

            var folderPickResult = await ShowFolderDialogCallback();
            if (!folderPickResult.Any()) return;

            var destFolder = folderPickResult.First().TryGetLocalPath();
            if (destFolder == null) return;

            // Collect all source folders: root folder plus every subfolder.
            var sourceFolders = new List<string>();
            if (openFolder != null)
                sourceFolders.Add(openFolder);
            foreach (var subfolder in AvailableSubfolders)
                sourceFolders.Add(subfolder.FolderPath);

            // Gather every image/txt pair across all source folders.
            var pairs = new List<(string imageFile, string baseName)>();
            var imagePatterns = new[] { "*.jpg", "*.png" };
            foreach (var folder in sourceFolders)
            {
                foreach (var pattern in imagePatterns)
                {
                    foreach (var imageFile in Directory.EnumerateFiles(folder, pattern))
                    {
                        pairs.Add((imageFile, Path.GetFileName(imageFile)));
                    }
                }
            }

            // Resolve filename conflicts by prepending the parent folder name.
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var resolved = new List<(string src, string destName)>();
            foreach (var (imageFile, baseName) in pairs)
            {
                var destName = baseName;
                if (!usedNames.Add(destName))
                {
                    var prefix = Path.GetFileName(Path.GetDirectoryName(imageFile)) + "_";
                    destName = prefix + baseName;
                    usedNames.Add(destName);
                }
                resolved.Add((imageFile, destName));
            }

            ShowProgressIndicator = true;
            ProgressIndicatorMax = resolved.Count;
            ProgressIndicatorProgress = 0;
            ProgressIndicatorMessage = "Exporting to flat folder...";

            await Task.Run(() =>
            {
                foreach (var (src, destName) in resolved)
                {
                    File.Copy(src, Path.Combine(destFolder, destName), overwrite: true);

                    var srcTxt = Path.ChangeExtension(src, ".txt");
                    if (File.Exists(srcTxt))
                    {
                        var destTxtName = Path.ChangeExtension(destName, ".txt");
                        File.Copy(srcTxt, Path.Combine(destFolder, destTxtName), overwrite: true);
                    }

                    ProgressIndicatorProgress++;
                }
            });

            ShowProgressIndicator = false;
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

        public void AddTagAfterTag(TagViewModel tag)
        {
            var newTag = new TagViewModel("");
            var index = SelectedImage?.Tags.IndexOf(tag) ?? -1;
            if (index != -1)
            {
                SelectedImage?.InsertTag(index + 1, newTag);
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

        /// <summary>
        /// True when any images exist anywhere in the project — either in the currently loaded
        /// folder or in any other subfolder.  Used as CanExecute for operations that iterate
        /// across the whole project via GetAllProjectImages().
        /// </summary>
        public bool AnyImagesLoaded =>
            (ImagesWithTags?.Any() ?? false) ||
            (AvailableSubfolders?.Any(sf =>
                !string.Equals(sf.FolderPath, currentImagesFolder, StringComparison.OrdinalIgnoreCase)) ?? false);

        [RelayCommand(CanExecute = nameof(AnyImagesLoaded))]
        public async Task AddTagToEndOfAllImages()
        {
            var dialog = new TagSearchDialog();
            dialog.DialogTitle = "Add Tag to End of All Images";
            var tagResult = await dialogHandler.ShowDialog<string?>(dialog);
            if (tagResult == null) return;

            _updateTagCounts = false;
            foreach (var (folder, image, isCurrentFolder) in GetAllProjectImages())
            {
                if (!image.Tags.Any(t => t.Tag == tagResult))
                {
                    image.AddTag(new TagViewModel(tagResult));
                    if (!isCurrentFolder)
                        SaveImageToDisk(folder, image);
                }
            }
            _updateTagCounts = true;
            UpdateTagCounts();
        }

        [RelayCommand(CanExecute = nameof(AnyImagesLoaded))]
        public async Task AddTagToStartOfAllImages()
        {
            var dialog = new TagSearchDialog();
            dialog.DialogTitle = "Add Tag to Start of All Images";
            var tagResult = await dialogHandler.ShowDialog<string?>(dialog);
            if (tagResult == null) return;

            _updateTagCounts = false;
            foreach (var (folder, image, isCurrentFolder) in GetAllProjectImages())
            {
                if (!image.Tags.Any(t => t.Tag == tagResult))
                {
                    image.InsertTag(0, new TagViewModel(tagResult));
                    if (!isCurrentFolder)
                        SaveImageToDisk(folder, image);
                }
            }
            _updateTagCounts = true;
            UpdateTagCounts();
        }

        public async void ReplaceTagInAllImages(string target)
        {
            var dialog = new TagSearchDialog();
            dialog.DialogTitle = $"Replace all instances of tag {target} with new tag";
            var tagResult = await dialogHandler.ShowDialog<string?>(dialog);
            if (tagResult == null) return;

            _updateTagCounts = false;
            foreach (var (folder, image, isCurrentFolder) in GetAllProjectImages())
            {
                image.ReplaceTagIfExists(target, tagResult);
                if (!isCurrentFolder)
                    SaveImageToDisk(folder, image);
            }
            _updateTagCounts = true;
            UpdateTagCounts();
        }

        [RelayCommand(CanExecute = nameof(AnyImagesLoaded))]
        public async Task RemoveTagFromAllImages()
        {
            var dialog = new TagSearchDialog();
            dialog.DialogTitle = "Remove tag from all images";
            var tagResult = await dialogHandler.ShowDialog<string?>(dialog);
            if (tagResult != null)
                RemoveTagFromAllImages(tagResult);
        }

        public void RemoveTagFromAllImages(string tag)
        {
            _updateTagCounts = false;
            foreach (var (folder, image, isCurrentFolder) in GetAllProjectImages())
            {
                var toRemove = image.Tags.Where(t => t.Tag == tag).ToList();
                if (toRemove.Count == 0) continue;
                foreach (var tagToRemove in toRemove)
                    image.RemoveTag(tagToRemove);
                if (!isCurrentFolder)
                    SaveImageToDisk(folder, image);
            }
            _updateTagCounts = true;
            UpdateTagCounts();
        }

        [RelayCommand()]
        internal void NextImage()
        {
            if (FilteredImageSet?.Any() ?? false)
            {
                if (SelectedImage == null)
                {
                    SelectedImage = this.FilteredImageSet.First();
                }
                else
                {
                    var index = FilteredImageSet.IndexOf(SelectedImage);
                    if (index < FilteredImageSet.Count() - 1)
                    {
                        SelectedImage = FilteredImageSet[index + 1];
                    }
                }
            }
        }

        [RelayCommand()]
        internal void PreviousImage()
        {
            if (FilteredImageSet?.Any() ?? false)
            {
                if (SelectedImage != null)
                {
                    var index = FilteredImageSet.IndexOf(SelectedImage);
                    if (index > 0)
                    {
                        SelectedImage = FilteredImageSet[index - 1];
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

        private ObservableCollection<string> recentTags = new ObservableCollection<string>();
        public ObservableCollection<string> RecentTags { get => recentTags; set { recentTags = value; OnPropertyChanged(); } }

        private OrderedSetObservableCollection<TagCountLetterGroupViewModel> tagCounts = new OrderedSetObservableCollection<TagCountLetterGroupViewModel>((l, r) => l.Letter.CompareTo(r.Letter));
        public OrderedSetObservableCollection<TagCountLetterGroupViewModel> TagCounts { get => tagCounts; set { tagCounts = value; OnPropertyChanged(); } }

        // This is set to false when certain processes are running that affect large sets of tags, instead of updating the
        // counts on the fly we can just call UpdateTagCounts() to reinitialize the tag counts
        private bool _updateTagCounts = true;

        public void TagAdded(string addedTag)
        {
            if (_updateTagCounts && addedTag != "")
            {
                recentTags.Remove(addedTag);
                recentTags.Insert(0, addedTag);

                var firstLetter = Char.ToUpper(addedTag[0]);
                if (!TagCountDictionary.ContainsKey(addedTag))
                {
                    TagCountDictionary[addedTag] = 1;

                    var group = TagCounts.FirstOrDefault(tg => tg.Letter == firstLetter);
                    var vm = new TagWithCountViewModel(this)
                    {
                        Tag = addedTag,
                        Count = 1
                    };

                    if (group == null)
                    {
                        group = new TagCountLetterGroupViewModel(firstLetter);


                        TagCounts.Add(group);
                    }

                    group.TagCounts.Add(new TagWithCountViewModel(this)
                    {
                        Tag = addedTag,
                        Count = 1
                    });
                }
                else
                {
                    TagCountDictionary[addedTag]++;

                    var group = TagCounts.First(tg => tg.Letter == firstLetter);
                    var vm = group.TagCounts.First(tc => tc.Tag == addedTag);
                    vm.Count = TagCountDictionary[addedTag];
                }
            }
        }

        public void TagRemoved(string removedTag)
        {
            if (_updateTagCounts && removedTag != "")
            {
                recentTags.Remove(removedTag);
                recentTags.Insert(0, removedTag);

                if (TagCountDictionary.ContainsKey(removedTag))
                {
                    TagCountDictionary[removedTag]--;

                    var firstLetter = Char.ToUpper(removedTag[0]);
                    var group = TagCounts.First(tg => tg.Letter == firstLetter);
                    var vm = group.TagCounts.First(tc => tc.Tag == removedTag);

                    if (TagCountDictionary[removedTag] == 0)
                    {
                        TagCountDictionary.Remove(removedTag);
                        group.TagCounts.Remove(vm);
                    }
                    else
                    {
                        vm.Count = TagCountDictionary[removedTag];
                    }
                }
            }
        }

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
                OnPropertyChanged(nameof(ShowDeterminateProgressIndicator));
                OnPropertyChanged(nameof(ShowIndeterminateProgress));
            }
        }

        public bool ShowIndeterminateProgress => ProgressIndicatorMax == 0 && string.IsNullOrEmpty(ConsoleText);
        public bool ShowDeterminateProgressIndicator => ProgressIndicatorMax != 0;

        private int progressIndicatorMax = 0;
        public int ProgressIndicatorMax
        {
            get => progressIndicatorMax;
            private set
            {
                progressIndicatorMax = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowDeterminateProgressIndicator));
                OnPropertyChanged(nameof(ShowIndeterminateProgress));
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

        private string progressIndicatorMessage = "";
        public string ProgressIndicatorMessage
        {
            get => progressIndicatorMessage;
            private set
            {
                progressIndicatorMessage = value;
                OnPropertyChanged();
            }
        }
        #endregion

        private string consoleText = "";
        public string ConsoleText
        {
            get => consoleText;
            private set
            {
                consoleText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowIndeterminateProgress));
            }
        }

        private Dictionary<string, int> TagCountDictionary = new Dictionary<string, int>();


        public void UpdateTagCounts()
        {
            if (ImagesWithTags == null) return;

            // Start with in-memory tags for the currently loaded folder.
            var allTags = ImagesWithTags
                .SelectMany(i => i.Tags.Select(t => t.Tag).Where(t => t != ""))
                .ToList();

            // Add tags from every other subfolder by reading their .txt files directly
            // (no image loading needed).
            if (AvailableSubfolders != null)
            {
                foreach (var sf in AvailableSubfolders)
                {
                    if (string.Equals(sf.FolderPath, currentImagesFolder, StringComparison.OrdinalIgnoreCase))
                        continue;
                    try
                    {
                        var folderTagSets = new FolderTagSets(sf.FolderPath);
                        allTags.AddRange(folderTagSets.TagsSets.SelectMany(ts => ts.TagSet.Tags.Where(t => t != "")));
                    }
                    catch { /* skip folders that can't be read */ }
                }
            }

            TagCountDictionary = allTags
                .GroupBy(t => t)
                .OrderBy(t => t.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            UpdateTagCountsObservable();
        }

        public void UpdateTagCountsObservable()
        {
            TagCounts = TagCountDictionary.GroupBy(t => Char.ToUpper(t.Key[0]))
                                          .Select(pair => new TagCountLetterGroupViewModel(pair.Key)
                                          {
                                              TagCounts = pair.Select(innerpair => new TagWithCountViewModel(this)
                                              {
                                                  Tag = innerpair.Key,
                                                  Count = innerpair.Value
                                              }).ToOrderedSetObservableCollection((l, r) => l.Tag.CompareTo(r.Tag))
                                          }).ToOrderedSetObservableCollection((l, r) => l.Letter.CompareTo(r.Letter));
        }

        /// <summary>
        /// Async version of <see cref="UpdateTagCounts"/> — safe to call from any thread.
        /// Snapshots UI collections on the UI thread, reads subfolder tag files on a
        /// background thread, then updates the observable on the UI thread, so the caller
        /// never blocks the UI.
        /// </summary>
        public async Task UpdateTagCountsAsync()
        {
            if (ImagesWithTags == null) return;

            // Snapshot the in-memory collections on the UI thread (fast, no I/O).
            var (allTags, subfolderPaths) = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var tags = ImagesWithTags
                    .SelectMany(i => i.Tags.Select(t => t.Tag).Where(t => t != ""))
                    .ToList();
                var paths = AvailableSubfolders?
                    .Where(sf => !string.Equals(sf.FolderPath, currentImagesFolder, StringComparison.OrdinalIgnoreCase))
                    .Select(sf => sf.FolderPath)
                    .ToList();
                return (tags, paths);
            });

            // Read subfolder tag files on a background thread to avoid blocking the UI.
            if (subfolderPaths != null && subfolderPaths.Count > 0)
            {
                var subfolderTags = await Task.Run(() =>
                {
                    var result = new List<string>();
                    foreach (var path in subfolderPaths)
                    {
                        try
                        {
                            var folderTagSets = new FolderTagSets(path);
                            result.AddRange(folderTagSets.TagsSets.SelectMany(ts => ts.TagSet.Tags.Where(t => t != "")));
                        }
                        catch { /* skip folders that can't be read */ }
                    }
                    return result;
                });
                allTags.AddRange(subfolderTags);
            }

            var newDictionary = allTags
                .GroupBy(t => t)
                .OrderBy(t => t.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            // Push the updated counts back to the UI thread.
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TagCountDictionary = newDictionary;
                UpdateTagCountsObservable();
            });
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

        public void ArchiveImage(string targetFolder, string targetFile, string? tagsFilename = null)
        {
            var destDirectory = Path.Combine(targetFolder, ARCHIVE_FOLDER);
            if (!Directory.Exists(destDirectory))
            {
                Directory.CreateDirectory(destDirectory);
            }

            var destFile = Path.Combine(destDirectory, targetFile);
            var destFileWithoutExtension = Path.Combine(destDirectory, Path.GetFileNameWithoutExtension(targetFile));
            var i = 0;
            while (File.Exists(destFile))
            {
                destFileWithoutExtension = Path.Combine(destDirectory, $"{Path.GetFileNameWithoutExtension(targetFile)}_{i.ToString("00")}");
                destFile = $"{destFileWithoutExtension}{Path.GetExtension(targetFile)}";
                ++i;
            }

            File.Move(Path.Combine(targetFolder, targetFile), destFile);

            if (tagsFilename != null)
            {
                var tagFile = Path.Combine(targetFolder, tagsFilename);
                if (File.Exists(tagFile))
                {
                    File.Move(tagFile, Path.Combine(destDirectory, $"{destFileWithoutExtension}.txt"));
                }
            }

        }

        [RelayCommand]
        public async Task ArchiveSelectedImage()
        {
            if (SelectedImage != null && ImagesWithTags != null)
            {
                var messageBoxStandardWindow = MessageBoxManager
                            .GetMessageBoxStandard("Archive Image?",
                                                         "Really archive selected image? It will be moved to a subdirectory named archive.",
                                                         ButtonEnum.YesNo,
                                                         Icon.Warning);

                var result = await dialogHandler.ShowDialog(messageBoxStandardWindow);

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

                    ArchiveImage(currentImagesFolder!, imageToDelete.Filename, imageToDelete.GetTagsFileName());
                }
            }
        }

        [RelayCommand]
        public async Task RunImgToImg(Bitmap image)
        {
            if (SelectedImage != null)
            {
                var dialog = new ImageTouchupDialog(settings);

                dialog.Tags = SelectedImage.Tags.Select(t => t.Tag).ToList();
                dialog.Image = image;
                dialog.OpenProject = this.OpenProject;
                await dialogHandler.ShowWindowAsDialog(dialog);

                if (dialog.Success)
                {
                    SelectedImage.ImageSource = dialog.Image;
                    SelectedImage.ImageSource.Save(Path.Combine(this.currentImagesFolder!, SelectedImage.Filename));
                }
            }
        }

        [RelayCommand]
        public async void ApplicationSettings()
        {
            var dialog = new SettingsDialog(settings);

            await dialogHandler.ShowWindowAsDialog(dialog);

            UpdateImageAspectRatioSets();
        }

        [RelayCommand]
        public async Task ProjectSettings()
        {
            if (openFolder == null) return;
            var proj = GetOrCreateCurrentFolderProject();
            var dialog = new ProjectSettingsDialog();
            dialog.Project = proj;
            await dialogHandler.ShowWindowAsDialog(dialog);
        }

        [RelayCommand]
        public async Task Exit()
        {
            if (await CheckCanExit())
            {
                ExitCallback?.Invoke();
            }
        }

        [RelayCommand]
        public async Task ClearTags()
        {
            if (SelectedImage != null && SelectedImage.Tags.Any())
            {
                var dialog = MessageBoxManager
                                    .GetMessageBoxStandard("Delete all tags",
                                                                 "This will delete all tags for the current image, are you sure?",
                                                                 ButtonEnum.YesNo,
                                                                 Icon.Warning);

                var result = await dialogHandler.ShowDialog(dialog);

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
                    var dialog = MessageBoxManager
                                    .GetMessageBoxStandard("Unsaved Changes",
                                                                 "You have unsaved changes, do you wish to save them now?",
                                                                 ButtonEnum.YesNoCancel,
                                                                 Icon.Warning);

                    var result = (await dialogHandler.ShowDialog(dialog));
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

        public async Task InterrogateAndApplyToSelectedImage(Bitmap bitmap)
        {
            try
            {
                var dialog = new InterrogationDialog();
                var vm = viewModelFactory.CreateViewModel<InterrogationDialogViewModel>();
                dialog.DataContext = vm;

                await dialogHandler.ShowWindowAsDialog(dialog);
                if (vm.Success)
                {
                    //Cache the selected image in case it's changed during async operation
                    var selectedImage = SelectedImage;
                    if (selectedImage != null)
                    {
                        ShowProgressIndicator = true;
                        ProgressIndicatorMessage = "Interrogating image...";

                        var imageData = bitmap.ToByteArray();

                        var folder = currentImagesFolder ?? openFolder ?? "";
                        var concepts = GetEffectiveConceptsForFolder(folder);
                        var stripPairs = GetEffectiveStripPairsForFolder(folder);

                        if (vm.SelectedNaturalLanguageSettingsViewModel != null)
                        {
                            var nlVm = vm.SelectedNaturalLanguageSettingsViewModel;
                            using var NLinterrogator = nlVm.CreateInterrogationContext();
                            ProgressIndicatorMax = 0;
                            ProgressIndicatorMessage = "Executing natural language interrogation...";
                            ConsoleText = $"Initializing...{Environment.NewLine}";
                            await NLinterrogator.InitializeOperation(message => ProgressIndicatorMessage = message, AddConsoleText);

                            Func<byte[], Action<string>, Action<string>, Task<string>> nlOp;
                            if (nlVm is IFolderAwareInterrogationViewModel<string> folderAwareNl)
                            {
                                var rawPrompt = GetEffectiveSettingForFolder(folder, p => p.DefaultNaturalLanguageInterrogationPrompt);
                                var effectivePrompt = rawPrompt != null ? ApplyConceptSubstitution(rawPrompt, concepts) : null;
                                var effectiveUrl = GetEffectiveSettingForFolder(folder, p => p.DefaultInterrogationEndpointUrl);
                                nlOp = folderAwareNl.GetFolderInterrogateOperation(effectivePrompt, effectiveUrl);
                            }
                            else
                            {
                                nlOp = NLinterrogator.InterrogateOperation;
                            }

                            var nlResult = await nlOp(imageData, message => ProgressIndicatorMessage = message, AddConsoleText);
                            selectedImage.Description = ApplyResponseStripping(nlResult, stripPairs);
                        }

                        if (vm.SelectedTagSettingsViewModel != null)
                        {
                            var tagVm = vm.SelectedTagSettingsViewModel;
                            using var taginterrogator = tagVm.CreateInterrogationContext();
                            ProgressIndicatorMax = 0;
                            ProgressIndicatorMessage = "Executing tag interrogation...";
                            ConsoleText = $"Initializing...{Environment.NewLine}";
                            await taginterrogator.InitializeOperation(message => ProgressIndicatorMessage = message, AddConsoleText);

                            Func<byte[], Action<string>, Action<string>, Task<List<string>>> tagOp;
                            if (tagVm is IFolderAwareInterrogationViewModel<List<string>> folderAwareTag)
                            {
                                var rawPrompt = GetEffectiveSettingForFolder(folder, p => p.DefaultTagInterrogationPrompt);
                                var effectivePrompt = rawPrompt != null ? ApplyConceptSubstitution(rawPrompt, concepts) : null;
                                var effectiveUrl = GetEffectiveSettingForFolder(folder, p => p.DefaultInterrogationEndpointUrl);
                                tagOp = folderAwareTag.GetFolderInterrogateOperation(effectivePrompt, effectiveUrl);
                            }
                            else
                            {
                                tagOp = taginterrogator.InterrogateOperation;
                            }

                            var tags = await tagOp(imageData, message => ProgressIndicatorMessage = message, AddConsoleText);
                            var strippedTags = ApplyResponseStrippingToTags(tags, stripPairs);
                            foreach (var tag in strippedTags)
                                selectedImage.AddTagIfNotExists(new TagViewModel(tag));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var messageBoxStandardWindow = MessageBoxManager
                                    .GetMessageBoxStandard("Interrogate Failed",
                                                 $"Failed to interrogate the image. The error returned was: {ex.Message}",
                                                 ButtonEnum.Ok,
                                                 Icon.Warning);
                await dialogHandler.ShowDialog(messageBoxStandardWindow);
            }
            finally
            {
                ShowProgressIndicator = false;
            }
        }

        #region Image Interrogation
        public async Task ReviewRemoveBackground(Bitmap bitmap)
        {
            ShowProgressIndicator = true;
            ProgressIndicatorMax = 0;
            ProgressIndicatorMessage = "Initializing Python Utilities...";
            ConsoleText = $"Initializing...{Environment.NewLine}";

            //Cache the selected image in case it's changed during async operation
            var selectedImage = SelectedImage;
            if (selectedImage != null)
            {
                using var utilities = new PythonUtilities();
                try
                {
                    var method = await ShowBackgroundRemovalDialog();

                    if (method != null)
                    {
                        await utilities.Initialize(message => ProgressIndicatorMessage = message, AddConsoleText);

                        var bytes = method == "RemBG" ? await utilities.RunRemBG(selectedImage.ImageSource.ToByteArray(), AddConsoleText) : await utilities.RunInsypreTransparentBG(selectedImage.ImageSource.ToByteArray(), AddConsoleText);

                        using (var mstream = new MemoryStream(bytes))
                        {
                            var imageResult = new Bitmap(mstream);

                            var viewer = new ImageReviewDialog();
                            viewer.Title = "Review image with removed background";
                            viewer.Images = new ObservableCollection<ImageReviewViewModel>() { new ImageReviewViewModel(imageResult) };
                            viewer.ReviewMode = ImageReviewDialogMode.SingleSelect;
                            await dialogHandler.ShowWindowAsDialog(viewer);

                            if (viewer.Success)
                            {
                                selectedImage.ImageSource = imageResult;
                                selectedImage.ImageSource.Save(Path.Combine(this.currentImagesFolder!, selectedImage.Filename));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var messageBoxStandardWindow = MessageBoxManager
                            .GetMessageBoxStandard("Rembg Failed",
                                                         $"Failed to execute rembg. Error message: {ex.Message}",
                                                         ButtonEnum.Ok,
                                                         Icon.Warning);

                    await dialogHandler.ShowDialog(messageBoxStandardWindow);
                }
            }

            ShowProgressIndicator = false;
        }

        public Task<TResult> Interrogate<TResult>(ConfiguredInterrogationContext<TResult> interrogator, byte[] imageData)
        {
            ProgressIndicatorMax = 0;
            ProgressIndicatorMessage = "Executing image interrogation..";
            ConsoleText = $"Initializing...{Environment.NewLine}";
            return interrogator.InterrogateOperation(imageData, message => ProgressIndicatorMessage = message, AddConsoleText);
        }

        public async Task<(string? description, IEnumerable<string>? tags)> Interrogate(ConfiguredInterrogationContext<string>? naturalLanguageInterrogator, ConfiguredInterrogationContext<List<string>>? tagInterrrogator, Bitmap image)
        {
            ProgressIndicatorMax = 0;

            string? description = null;
            if (naturalLanguageInterrogator != null)
            {
                ProgressIndicatorMessage = "Executing image interrogation..";
                ConsoleText = $"Initializing...{Environment.NewLine}";
                description = await naturalLanguageInterrogator.InterrogateOperation(image.ToByteArray(), message => ProgressIndicatorMessage = message, AddConsoleText);
            }
            List<string> tags = null;
            if (tagInterrrogator != null)
            {
                ProgressIndicatorMessage = "Executing image interrogation...";
                ProgressIndicatorMax = 0;
                ConsoleText = $"Initializing...{Environment.NewLine}";
                tags = await tagInterrrogator.InterrogateOperation(image.ToByteArray(), message => ProgressIndicatorMessage = message, AddConsoleText);
            }
            return (description: description, tags: tags);
        }

        [RelayCommand(CanExecute = nameof(AnyImagesLoaded))]
        public async Task InterrogateAllImages()
        {

            var dialog = new InterrogationDialog();
            var vm = viewModelFactory.CreateViewModel<InterrogationDialogViewModel>();
            dialog.DataContext = vm;

            await dialogHandler.ShowWindowAsDialog(dialog);
            if (!vm.Success)
                return;

            var nlVm = vm.SelectedNaturalLanguageSettingsViewModel;
            var tagVm = vm.SelectedTagSettingsViewModel;
            if (nlVm == null && tagVm == null)
                return;

            // Pre-compute per-folder settings now (on the UI thread), then build queue items.
            var allImages = GetAllProjectImages().ToList();
            var items = new List<BatchQueueItem>();

            // Shared interrogation contexts, lazily initialized inside the first item that runs.
            // Closures below capture these references so all items in this batch share the same
            // initialized models, while each item can be retried independently.
            ConfiguredInterrogationContext<string>? nlCtx = null;
            ConfiguredInterrogationContext<List<string>>? tagCtx = null;
            bool nlInitialized = false;
            bool tagInitialized = false;

            foreach (var (folder, image, isCurrentFolder) in allImages)
            {
                var concepts = GetEffectiveConceptsForFolder(folder);
                var stripPairs = GetEffectiveStripPairsForFolder(folder);

                string? nlPrompt = null;
                string? tagPrompt = null;
                string? endpointUrl = null;

                if (nlVm != null)
                {
                    var raw = GetEffectiveSettingForFolder(folder, p => p.DefaultNaturalLanguageInterrogationPrompt);
                    nlPrompt = raw != null ? ApplyConceptSubstitution(raw, concepts) : null;
                    endpointUrl = GetEffectiveSettingForFolder(folder, p => p.DefaultInterrogationEndpointUrl);
                }
                if (tagVm != null)
                {
                    var raw = GetEffectiveSettingForFolder(folder, p => p.DefaultTagInterrogationPrompt);
                    tagPrompt = raw != null ? ApplyConceptSubstitution(raw, concepts) : null;
                    endpointUrl ??= GetEffectiveSettingForFolder(folder, p => p.DefaultInterrogationEndpointUrl);
                }

                // Capture loop variables for the closure
                var capturedFolder = folder;
                var capturedImage = image;
                var capturedIsCurrentFolder = isCurrentFolder;
                var capturedStripPairs = stripPairs;
                var capturedNlPrompt = nlPrompt;
                var capturedTagPrompt = tagPrompt;
                var capturedUrl = endpointUrl;

                string opLabel = (nlVm != null && tagVm != null) ? "NL + Tag interrogate"
                    : (nlVm != null ? "NL interrogate" : "Tag interrogate");

                if (isCurrentFolder)
                    image.SetHasPendingOperation(true, opLabel);

                items.Add(new BatchQueueItem(
                    BatchQueue,
                    isCurrentFolder ? image : null,
                    image.Filename,
                    capturedFolder,
                    $"{opLabel}: {image.Filename}",
                    async () =>
                    {
                        // Compute results on whatever thread the async operations resume on.
                        // Mutations to ObservableObject properties and ObservableCollection
                        // must happen on the UI thread, so we collect results here and apply
                        // them in a single Dispatcher.UIThread.InvokeAsync at the end.
                        string? computedDescription = null;
                        List<string>? computedTags = null;

                        if (nlVm != null)
                        {
                            if (!nlInitialized)
                            {
                                nlCtx?.Dispose();
                                nlCtx = nlVm.CreateInterrogationContext();
                                await nlCtx.InitializeOperation(_ => { }, _ => { });
                                nlInitialized = true;
                            }

                            Func<byte[], Action<string>, Action<string>, Task<string>> nlOp =
                                nlVm is IFolderAwareInterrogationViewModel<string> faVm
                                    ? faVm.GetFolderInterrogateOperation(capturedNlPrompt, capturedUrl)
                                    : nlCtx!.InterrogateOperation;

                            var imageData = capturedImage.ImageSource.ToByteArray();
                            var nlResult = await nlOp(imageData, _ => { }, _ => { });
                            computedDescription = ApplyResponseStripping(nlResult, capturedStripPairs);
                        }

                        if (tagVm != null)
                        {
                            if (!tagInitialized)
                            {
                                tagCtx?.Dispose();
                                tagCtx = tagVm.CreateInterrogationContext();
                                await tagCtx.InitializeOperation(_ => { }, _ => { });
                                tagInitialized = true;
                            }

                            Func<byte[], Action<string>, Action<string>, Task<List<string>>> tagOp =
                                tagVm is IFolderAwareInterrogationViewModel<List<string>> faVm
                                    ? faVm.GetFolderInterrogateOperation(capturedTagPrompt, capturedUrl)
                                    : tagCtx!.InterrogateOperation;

                            var imageData = capturedImage.ImageSource.ToByteArray();
                            var tags = await tagOp(imageData, _ => { }, _ => { });
                            computedTags = ApplyResponseStrippingToTags(tags, capturedStripPairs);
                        }

                        // Stage 1: Apply UI mutations on the UI thread.
                        // _updateTagCounts is suppressed so that each AddTagIfNotExists call
                        // does not trigger an incremental TagCounts rebuild per tag; we do a
                        // single async rebuild in stage 3 instead.
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (computedDescription != null)
                                capturedImage.Description = computedDescription;
                            if (computedTags != null)
                            {
                                _updateTagCounts = false;
                                try
                                {
                                    foreach (var tag in computedTags)
                                        capturedImage.AddTagIfNotExists(new TagViewModel(tag));
                                }
                                finally
                                {
                                    _updateTagCounts = true;
                                }
                            }
                        });

                        // Stage 2: Persist to disk off the UI thread (InvokeAsync returned us
                        // to the background thread).
                        SaveImageToDisk(capturedFolder, capturedImage);

                        // Stage 3: Rebuild tag counts — snapshots UI collections on UI thread,
                        // reads subfolder files on a background thread, then pushes the new
                        // dictionary to the observable on the UI thread.
                        await UpdateTagCountsAsync();
                    }
                ));
            }

            BatchQueue.EnqueueRange(items);
        }

        [RelayCommand(CanExecute = nameof(AnyImagesLoaded))]
        public async Task RemoveMetaTags()
        {
            foreach (var tag in MetaTags.Tags)
            {
                RemoveTagFromAllImages(tag);
            }
        }

        #endregion

        public void AddNewImage(Bitmap image, ImageWithTagsViewModel baseImage, string? description = null, IEnumerable<string>? tags = null)
        {
            var index = ImagesWithTags.IndexOf(baseImage);
            var withoutExtension = Path.GetFileNameWithoutExtension(baseImage.Filename);
            if (baseImage.FirstNumberedChunk != -1)
            {
                withoutExtension = baseImage.FirstNumberedChunk.ToString("00000");
            }

            int i = 0;
            if (baseImage.SecondNumberedChunk != -1)
            {
                i = baseImage.SecondNumberedChunk;
            }

            var newFilename = $"{withoutExtension}__{i:00}";
            while (ImagesWithTags.Any(i => newFilename == Path.GetFileNameWithoutExtension(i.Filename)))
            {
                newFilename = $"{withoutExtension}__{++i:00}";
            }
            newFilename = Path.Combine(currentImagesFolder!, $"{newFilename}.png");
            image.Save(newFilename);
            var newImageViewModel = new ImageWithTagsViewModel(image, newFilename, ImageDirtyHandler, description, tags);
            newImageViewModel.TagEntered += this.TagAdded;
            newImageViewModel.TagRemoved += this.TagRemoved;

            if (description != null || tags != null)
            {
                var set = new TagSet(Path.Combine(currentImagesFolder!, newImageViewModel.GetTagsFileName()), description, tags);
                set.WriteFile();
            }

            ImagesWithTags.Insert(index + 1, newImageViewModel);
        }

        public void UpdateImageListForCompletionStatusChanging()
        {

        }

        public void ImageCropped(Bitmap image)
        {
            AddNewImage(image, SelectedImage, SelectedImage.Description, SelectedImage.Tags.Select(t => t.Tag));
        }

        public async Task ExtractAndReviewComicPanels(Bitmap baseImage, RenderTargetBitmap? paint)
        {
            var panels = await comicPanelExtractorService.ExtractComicPanels(baseImage, paint);
            if (panels != null)
            {
                ImageReviewDialog dialog = new ImageReviewDialog();
                dialog.Images = panels.Select(p => new ImageReviewViewModel(p))
                                      .ToObservableCollection();

                dialog.ReviewMode = ImageReviewDialogMode.MultiSelect;
                dialog.Title = "Select Images to Keep";

                await dialogHandler.ShowWindowAsDialog(dialog);

                if (dialog.Success)
                {
                    foreach (var image in dialog.SelectedImages)
                    {
                        AddNewImage(image, SelectedImage);
                    }
                }
            }
        }

        public Task SaveCurrentImage(Bitmap image)
        {
            if (currentImagesFolder != null && SelectedImage != null)
            {
                SaveUpdatedImage(SelectedImage, image);
            }

            return Task.CompletedTask;
        }

        public Task SaveUpdatedImage(ImageWithTagsViewModel targetImageWithTags, Bitmap image)
        {
            if (currentImagesFolder != null)
            {
                if (Path.GetExtension(targetImageWithTags.Filename) != ".png")
                {
                    image.Save(Path.Combine(currentImagesFolder!, targetImageWithTags.Filename));
                    targetImageWithTags.Filename = $"{Path.GetFileNameWithoutExtension(targetImageWithTags.Filename)}.png";
                }
                image.Save(Path.Combine(currentImagesFolder!, targetImageWithTags.Filename));
                targetImageWithTags.ImageSource = image;
                targetImageWithTags.UpdateThumbnail();
            }

            return Task.CompletedTask;
        }

        public async Task ExpandImage(Bitmap image)
        {
            var dialog = new ExpandImageDialog();
            var effectiveSize = GetEffectiveSettingValue(p => p.TargetImageSize);
            if (effectiveSize.HasValue && effectiveSize.Value.Width > 0 && effectiveSize.Value.Height > 0)
            {
                dialog.ComputeExpansionNeededForTargetAspectRatio(image.PixelSize.Width, image.PixelSize.Height, effectiveSize.Value.Width, effectiveSize.Value.Height);
            }
            await dialogHandler.ShowWindowAsDialog(dialog);
            if (dialog.Success)
            {
                var finalSize = new PixelSize(image.PixelSize.Width + dialog.ExpandLeft + dialog.ExpandRight, image.PixelSize.Height + dialog.ExpandUp + dialog.ExpandDown);
                var imageRegion = new Rect(dialog.ExpandLeft, dialog.ExpandUp, image.PixelSize.Width, image.PixelSize.Height);
                var newImage = new RenderTargetBitmap(finalSize);
                using (var drawingContext = newImage.CreateDrawingContext())
                {
                    using (drawingContext.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
                    {
                        drawingContext.FillRectangle(new SolidColorBrush(new Color(255, 255, 255, 255)), new Rect(0, 0, finalSize.Width, finalSize.Height));
                        drawingContext.DrawImage(image, new Rect(0, 0, image.PixelSize.Width, image.PixelSize.Height), imageRegion);
                    }
                }

                AddNewImage(newImage, SelectedImage, SelectedImage.Description, SelectedImage.Tags.Select(t => t.Tag));
            }
        }

        public bool ImageDirtyHandler(Bitmap image)
        {
            return ImageDirtyCallback?.Invoke(image) ?? false;
        }

        private ImageFilterMode currentImageFilterMode;

        public ImageFilterMode CurrentImageFilterMode
        {
            get { return currentImageFilterMode; }
            set
            {
                if (SetProperty(ref currentImageFilterMode, value))
                {
                    RebuildFilteredImageSet();
                }
            }
        }

        public void AddConsoleText(string text)
        {
            Dispatcher.UIThread.InvokeAsync(() => ConsoleText += text + Environment.NewLine);
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

        public Func<Bitmap?> GetImageBoxMask { get; internal set; }
        public Action<Bitmap, Bitmap> SetImageBoxMask { get; internal set; }

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

            await dialogHandler.ShowWindowAsDialog(dialog);

            if (dialog.Success)
            {
                UpdateTagPrioritySets();
            }
        }

        [RelayCommand(CanExecute = nameof(ImagesLoaded))]
        public async Task ApplyTagPrioritySetToAllImages()
        {
            if (tagPrioritySets != null && ImagesWithTags != null)
            {
                var dialog = new DropdownSelectDialog();
                dialog.DropdownItems = tagPrioritySets.ToDropdownSelectItems(tps => tps.Name);
                var dialogResult = await dialogHandler.ShowDialog<DropdownSelectItem?>(dialog);
                var result = dialogResult as DropdownSelectItem<TagPrioritySetButtonViewModel>;
                if (result != null)
                {
                    foreach (var image in ImagesWithTags)
                    {
                        image.ApplyTagOrdering(t => result.Value.PrioritySet.GetTagPriority(t));
                    }
                }
            }
        }

        [RelayCommand]
        public void SetImageFilter(ImageFilterMode mode)
        {
            CurrentImageFilterMode = mode;
        }

        [RelayCommand]
        public void ToggleImageComplete()
        {
            if (SelectedImage != null && openFolder != null)
            {
                SelectedImage.IsComplete = !SelectedImage.IsComplete;
                var proj = GetOrCreateCurrentFolderProject();
                proj.SetImageCompletionStatus(SelectedImage.Filename, SelectedImage.IsComplete);
                proj.Save();

                if (CurrentImageFilterMode != ImageFilterMode.None && FilteredImageSet != null)
                {
                    FilteredImageSet.Remove(SelectedImage);
                }
            }
        }

        public void UpdateTagPrioritySets()
        {
            try
            {
                if (Directory.Exists(TAG_PRIORITY_SETS_FOLDER))
                {
                    if (Directory.Exists(TAG_PRIORITY_SETS_FOLDER))
                    {
                        var txts = Directory.EnumerateFiles(TAG_PRIORITY_SETS_FOLDER, "*.xml").ToList();
                        TagPrioritySets = txts.Select(filename =>
                                                        new TagPrioritySetButtonViewModel(
                                                                Path.GetFileNameWithoutExtension(filename),
                                                                new TagCategorySet(filename)))
                                          .ToObservableCollection();
                    }
                }
            }
            catch (Exception ex)
            {
                var messageBoxStandardWindow = MessageBoxManager
                            .GetMessageBoxStandard("Error",
                                                         $"Failed to load tag priority sets. Error message: {ex.Message}",
                                                         ButtonEnum.Ok,
                                                         Icon.Warning);
                dialogHandler.ShowDialog(messageBoxStandardWindow);
            }

        }

        public Bitmap ConvertImageAlphaToColor(Bitmap sourceImage, Color color)
        {
            var newImage = new RenderTargetBitmap(sourceImage.PixelSize);
            using (var drawingContext = newImage.CreateDrawingContext())
            {
                using (drawingContext.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
                {
                    drawingContext.FillRectangle(new SolidColorBrush(color), new Rect(0, 0, sourceImage.PixelSize.Width, sourceImage.PixelSize.Height));
                    drawingContext.DrawImage(sourceImage, new Rect(0, 0, sourceImage.PixelSize.Width, sourceImage.PixelSize.Height));
                }
            }
            return newImage;
        }

        [RelayCommand(CanExecute = nameof(ImagesLoaded))]
        public async Task ConvertAllImageAlphasToColor()
        {
            if (currentImagesFolder == null || ImagesWithTags == null) return;

            var dialog = new ColorPickerDialog();
            await dialogHandler.ShowDialog<Color?>(dialog);
            if (!dialog.Success) return;

            var selectedColor = dialog.SelectedColor;
            var folder = currentImagesFolder;

            var items = new List<BatchQueueItem>();
            foreach (var image in ImagesWithTags)
            {
                var capturedImage = image;
                image.SetHasPendingOperation(true, "Convert alpha to color");
                items.Add(new BatchQueueItem(
                    BatchQueue,
                    image,
                    image.Filename,
                    folder,
                    $"Convert alpha: {image.Filename}",
                    async () =>
                    {
                        // ConvertImageAlphaToColor needs the UI thread
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            var newImage = ConvertImageAlphaToColor(capturedImage.ImageSource, selectedColor);
                            capturedImage.ImageSource = newImage;
                            capturedImage.ImageSource.Save(Path.Combine(folder, capturedImage.Filename));
                        });
                    }
                ));
            }

            BatchQueue.EnqueueRange(items);
        }

        [RelayCommand(CanExecute = nameof(ImagesLoaded))]
        public Task ExtractAllPanels()
        {
            if (currentImagesFolder == null || ImagesWithTags == null) return Task.CompletedTask;

            var folder = currentImagesFolder;
            var items = new List<BatchQueueItem>();
            foreach (var image in ImagesWithTags.ToList())
            {
                var capturedImage = image;
                image.SetHasPendingOperation(true, "Extract comic panels");
                items.Add(new BatchQueueItem(
                    BatchQueue,
                    image,
                    image.Filename,
                    folder,
                    $"Extract panels: {image.Filename}",
                    async () =>
                    {
                        var panels = await comicPanelExtractorService.ExtractComicPanels(capturedImage.ImageSource);
                        if (panels == null) return;
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            foreach (var panel in panels)
                            {
                                AddNewImage(panel, capturedImage);
                                panel.Save(capturedImage.Filename);
                            }
                        });
                    }
                ));
            }

            BatchQueue.EnqueueRange(items);
            return Task.CompletedTask;
        }

        public async Task ReviewConvertAlpha(Bitmap bitmap)
        {
            //Cache the selected image in case it's changed during async operation
            var selectedImage = SelectedImage;

            var dialog = new ColorPickerDialog();
            await dialogHandler.ShowDialog<Color?>(dialog);
            if (dialog.Success)
            {
                var imageResult = ConvertImageAlphaToColor(bitmap, dialog.SelectedColor);
                var viewer = new ImageReviewDialog();
                viewer.Title = "Review converted image";
                viewer.Images = new ObservableCollection<ImageReviewViewModel>() { new ImageReviewViewModel(imageResult) };
                viewer.ReviewMode = ImageReviewDialogMode.SingleSelect;
                await dialogHandler.ShowWindowAsDialog(viewer);

                if (viewer.Success)
                {
                    selectedImage.ImageSource = imageResult;
                    selectedImage.ImageSource.Save(Path.Combine(this.currentImagesFolder!, selectedImage.Filename));
                }
            }
        }


        [RelayCommand]
        public void CopyTags()
        {
            if (SelectedImage != null)
            {
                CopiedTags = new List<string>(SelectedImage.Tags.Select(t => t.Tag));
            }
        }

        [RelayCommand]
        public void PasteTags()
        {
            if (SelectedImage != null && CopiedTags != null)
            {
                foreach (var tag in CopiedTags)
                {
                    SelectedImage.AddTagIfNotExists(new TagViewModel(tag));
                }
            }
        }

        [RelayCommand]
        public async Task ShowStandaloneImageEditor()
        {
            var editor = new ImageTouchupDialog(settings);
            editor.IsStandalone = true;
            await dialogHandler.ShowWindowAsDialog(editor);
        }
        #endregion

        [RelayCommand]
        public async Task RemoveFromSelectedImageWithLama()
        {
            ShowProgressIndicator = true;
            ProgressIndicatorMax = 0;
            ProgressIndicatorMessage = "Initializing Python Utilities...";
            ConsoleText = $"Initializing...{Environment.NewLine}";

            using var utilities = new PythonUtilities();
            try
            {
                await utilities.Initialize(message => ProgressIndicatorMessage = message, AddConsoleText);

                var mask = GetImageBoxMask?.Invoke();
                if (mask != null)
                {
                    var bytes = await utilities.RunLama(SelectedImage.ImageSource.ToByteArray(), mask.ToByteArray(), AddConsoleText);

                    using (var mstream = new MemoryStream(bytes))
                    {
                        var imageResult = new Bitmap(mstream);

                        var viewer = new ImageReviewDialog();
                        viewer.Title = "Review image after lama removal";
                        viewer.Images = new ObservableCollection<ImageReviewViewModel>() { new ImageReviewViewModel(imageResult) };
                        viewer.ReviewMode = ImageReviewDialogMode.SingleSelect;
                        await dialogHandler.ShowWindowAsDialog(viewer);

                        if (viewer.Success)
                        {
                            selectedImage.ImageSource = imageResult;
                            selectedImage.ImageSource.Save(Path.Combine(this.currentImagesFolder!, selectedImage.Filename));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var messageBoxStandardWindow = MessageBoxManager
                        .GetMessageBoxStandard("Lama Remover Failed",
                                                     $"Failed to execute lama model. Error message: {ex.Message}",
                                                     ButtonEnum.Ok,
                                                     Icon.Warning);

                await dialogHandler.ShowDialog(messageBoxStandardWindow);
            }
            ShowProgressIndicator = false;
        }

        [RelayCommand]
        public async Task GenerateImageMaskWithYolo()
        {
            var dialog = new YOLOModelSelectorDialog();
            dialog.DataContext = viewModelFactory.CreateViewModel<YOLOModelSelectorDialogViewModel>();

            var dialogResult = await dialogHandler.ShowDialog<(string, float, int)?>(dialog);

            if (dialogResult != null)
            {
                var (selectedYoloModelPath, threshold, expandMask) = dialogResult.Value;
                ShowProgressIndicator = true;
                ProgressIndicatorMax = 0;
                ProgressIndicatorMessage = "Initializing Python Utilities...";
                ConsoleText = $"Initializing...{Environment.NewLine}";

                using var utilities = new PythonUtilities();
                try
                {
                    await utilities.Initialize(message => ProgressIndicatorMessage = message, AddConsoleText);

                    var bytes = await utilities.GenerateYoloMask(selectedYoloModelPath, selectedImage.ImageSource.ToByteArray(), threshold, AddConsoleText);

                    if (bytes != null)
                    {
                        var imageResult = bytes.ToBitmap();
                        imageResult = expandMask != 0 ? imageResult.ExpandMask(expandMask) : imageResult;
                        var viewer = new ImageReviewDialog();
                        viewer.Title = "Review generated mask";
                        viewer.Images = new ObservableCollection<ImageReviewViewModel>() { new ImageReviewViewModel(imageResult) };
                        viewer.ReviewMode = ImageReviewDialogMode.SingleSelect;
                        await dialogHandler.ShowWindowAsDialog(viewer);

                        if (viewer.Success)
                        {
                            SetImageBoxMask(SelectedImage.ImageSource, imageResult);
                        }
                    }
                    else
                    {
                        var messageBoxStandardWindow = MessageBoxManager
                           .GetMessageBoxStandard("No Masks Detected",
                                                        $"YOLO model had no detections.",
                                                        ButtonEnum.Ok,
                                                        Icon.Info);

                        await dialogHandler.ShowDialog(messageBoxStandardWindow);
                    }
                }
                catch (Exception ex)
                {
                    var messageBoxStandardWindow = MessageBoxManager
                            .GetMessageBoxStandard("YOLO Mask Generation Failed",
                                                         $"Failed to generate mask. Error message: {ex.Message}",
                                                         ButtonEnum.Ok,
                                                         Icon.Warning);

                    await dialogHandler.ShowDialog(messageBoxStandardWindow);
                }
            }
            ShowProgressIndicator = false;
        }

        [RelayCommand]
        public async Task GenerateMaskThenRemoveFromAllImages()
        {
            if (currentImagesFolder == null || ImagesWithTags == null) return;

            var dialog = new YOLOModelSelectorDialog();
            dialog.DataContext = viewModelFactory.CreateViewModel<YOLOModelSelectorDialogViewModel>();

            var dialogResult = await dialogHandler.ShowDialog<(string, float, int)?>(dialog);
            if (dialogResult == null) return;

            var (selectedYoloModelPath, threshold, expandMask) = dialogResult.Value;
            var folder = currentImagesFolder;

            // Shared Python utilities, lazily initialized inside the first queue item.
            PythonUtilities? utilities = null;
            bool utilitiesInitialized = false;

            var items = new List<BatchQueueItem>();
            foreach (var image in ImagesWithTags.ToList())
            {
                var capturedImage = image;
                image.SetHasPendingOperation(true, "Generate mask + remove");
                items.Add(new BatchQueueItem(
                    BatchQueue,
                    image,
                    image.Filename,
                    folder,
                    $"Mask + remove: {image.Filename}",
                    async () =>
                    {
                        if (!utilitiesInitialized)
                        {
                            utilities?.Dispose();
                            utilities = new PythonUtilities();
                            await utilities.Initialize(_ => { }, _ => { });
                            utilitiesInitialized = true;
                        }

                        var bytes = await utilities!.GenerateYoloMask(
                            selectedYoloModelPath,
                            capturedImage.ImageSource.ToByteArray(),
                            threshold,
                            _ => { });

                        if (bytes != null)
                        {
                            bytes = expandMask != 0
                                ? bytes.ToBitmap().ExpandMask(expandMask).ToByteArray()
                                : bytes;
                            var result = await utilities.RunLama(capturedImage.ImageSource.ToByteArray(), bytes, _ => { });
                            var resultBitmap = result.ToBitmap();
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                ArchiveImage(folder, capturedImage.Filename, capturedImage.GetTagsFileName());
                                capturedImage.ImageSource = resultBitmap;
                                capturedImage.UpdateThumbnail();
                                capturedImage.ImageSource.Save(Path.Combine(folder, capturedImage.Filename));
                            });
                        }
                    }
                ));
            }

            BatchQueue.EnqueueRange(items);
        }


        private async Task<string?> ShowBackgroundRemovalDialog()
        {
            var dialog = new DropdownSelectDialog();
            dialog.Title = "Select Model";
            dialog.Width = 300;
            dialog.DropdownItems = new List<DropdownSelectItem>() {
                new DropdownSelectItem<string>("RemBG", "RemBG"),
                new DropdownSelectItem<string>("Inspyre Transparent Background", "Transparent-Background"),
            };

            var dialogResult = await dialogHandler.ShowDialog<DropdownSelectItem?>(dialog);
            var result = dialogResult as DropdownSelectItem<string>;

            return result?.Value;
        }

        [RelayCommand]
        public async Task RemoveBackgroundFromAllImages()
        {
            if (currentImagesFolder == null || ImagesWithTags == null) return;

            var method = await ShowBackgroundRemovalDialog();
            if (method == null) return;

            var folder = currentImagesFolder;

            // Shared Python utilities, lazily initialized in the first queue item.
            PythonUtilities? utilities = null;
            bool utilitiesInitialized = false;

            var items = new List<BatchQueueItem>();
            foreach (var image in ImagesWithTags.ToList())
            {
                var capturedImage = image;
                image.SetHasPendingOperation(true, "Remove background");
                items.Add(new BatchQueueItem(
                    BatchQueue,
                    image,
                    image.Filename,
                    folder,
                    $"Remove background: {image.Filename}",
                    async () =>
                    {
                        if (!utilitiesInitialized)
                        {
                            utilities?.Dispose();
                            utilities = new PythonUtilities();
                            await utilities.Initialize(_ => { }, _ => { });
                            utilitiesInitialized = true;
                        }

                        var bytes = method == "RemBG"
                            ? await utilities!.RunRemBG(capturedImage.ImageSource.ToByteArray(), _ => { })
                            : await utilities!.RunInsypreTransparentBG(capturedImage.ImageSource.ToByteArray(), _ => { });

                        if (bytes == null) return;
                        var resultBitmap = bytes.ToBitmap();
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            ArchiveImage(folder, capturedImage.Filename, capturedImage.GetTagsFileName());
                            capturedImage.ImageSource = resultBitmap;
                            capturedImage.UpdateThumbnail();
                            capturedImage.ImageSource.Save(Path.Combine(folder, capturedImage.Filename));
                        });
                    }
                ));
            }

            BatchQueue.EnqueueRange(items);
        }
    }
}