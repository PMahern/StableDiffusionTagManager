using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SixLabors.ImageSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using TagUtil;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class ImageWithTagsViewModel : ObservableObject
    {
        const int THUMBNAIL_SIZE = 200;
        private readonly Func<Bitmap, bool> imageDirtyCallback;
        private bool tagsDirty = false;
        private readonly int firstNumberedChunk = -1;
        private readonly int secondNumberedChunk = -1;
        private bool isNumbered = false;

        private ImageWithTagsViewModel(string imageFile, Func<Bitmap, bool> imageDirtyCallback)
        {
            this.imageDirtyCallback = imageDirtyCallback;
            filename = Path.GetFileName(imageFile);
            var withoutExtension = Path.GetFileNameWithoutExtension(filename);
            var chunks = withoutExtension.Split("__");
            if (chunks.Count() > 0)
            {
                int.TryParse(chunks[0], out firstNumberedChunk);
            }

            if (chunks.Count() > 1)
            {
                int.TryParse(chunks[0], out secondNumberedChunk);
            }
        }

        public ImageWithTagsViewModel(string imageFile, TagSet tagSet, Func<Bitmap, bool> imageDirtyCallback) : this(imageFile, imageDirtyCallback)
        {
            using var stream = File.OpenRead(imageFile);
            thumbnail = Bitmap.DecodeToHeight(stream, THUMBNAIL_SIZE);
            imageSource = new Bitmap(imageFile);
            tags = new ObservableCollection<TagViewModel>(tagSet.Tags.Select(t => new TagViewModel(t)
            {
                TagChanged = TagChangedHandler
            }));
            tags.CollectionChanged += SetTagsDirty;
        }

        public ImageWithTagsViewModel(Bitmap image, string newFileName, Func<Bitmap, bool> imageDirtyCallback, IEnumerable<string>? tags = null) : this(newFileName, imageDirtyCallback)
        {
            imageSource = image;
            thumbnail = GenerateThumbnail();

            if (tags != null)
            {
                this.tags = new ObservableCollection<TagViewModel>(tags.Select(t => new TagViewModel(t)
                {
                    TagChanged = TagChangedHandler
                }));
            }
            else
            {
                this.tags = new ObservableCollection<TagViewModel>();
            }
            this.tags.CollectionChanged += SetTagsDirty;
        }

        private void SetTagsDirty(object? sender, NotifyCollectionChangedEventArgs e)
        {
            tagsDirty = true;
        }

        public Action<string, string>? TagChanged;
        public Action<string>? TagRemoved;

        private Bitmap imageSource;
        public Bitmap ImageSource { get => imageSource; set { imageSource = value; OnPropertyChanged(); } }

        private Bitmap thumbnail;
        public Bitmap Thumbnail { get => thumbnail; set { thumbnail = value; OnPropertyChanged(); } }

        private string filename;
        public string Filename { get => filename; set => SetProperty(ref filename, value); }

        private ObservableCollection<TagViewModel> tags;

        public ReadOnlyObservableCollection<TagViewModel> Tags
        {
            get { return new ReadOnlyObservableCollection<TagViewModel>(tags); }
        }

        public int SecondNumberedChunk => secondNumberedChunk;

        public int FirstNumberedChunk => firstNumberedChunk;

        //Tag handling
        private TagViewModel? _tagBeingDragged = null;

        public void BeginTagDrag(TagViewModel tagViewModel)
        {
            tagViewModel.IsBeingDragged = false;
            _tagBeingDragged = tagViewModel;
        }

        public void TagDrop(TagViewModel dropTarget)
        {
            var dragIndex = tags.IndexOf(_tagBeingDragged);
            var dropIndex = tags.IndexOf(dropTarget);

            tags.RemoveAt(dragIndex);
            tags.Insert(dropIndex, _tagBeingDragged);

            _tagBeingDragged.IsBeingDragged = false;
            _tagBeingDragged = null;
        }

        [RelayCommand]
        public void RemoveTag(TagViewModel tag)
        {
            this.tags.Remove(tag);
            TagRemoved?.Invoke(tag.Tag);
        }

        public string GetTagsFileName()
        {
            return $"{System.IO.Path.GetFileNameWithoutExtension(Filename)}.txt";
        }

        public Bitmap GenerateThumbnail()
        {
            if (imageSource.PixelSize.Width > THUMBNAIL_SIZE || imageSource.PixelSize.Height > THUMBNAIL_SIZE)
            {
                var aspectRatio = (float)imageSource.PixelSize.Width / (float)imageSource.PixelSize.Height;
                var pixelSize = aspectRatio > 1.0 ? new PixelSize(THUMBNAIL_SIZE, (int)((float)THUMBNAIL_SIZE / aspectRatio)) : new PixelSize((int)((float)THUMBNAIL_SIZE * aspectRatio), THUMBNAIL_SIZE);
                var renderBitmap = new RenderTargetBitmap(pixelSize);
                using var dc = renderBitmap.CreateDrawingContext(null);
                using var drawingContext = new DrawingContext(dc, false);
                drawingContext.DrawImage(imageSource, new Rect(0, 0, imageSource.PixelSize.Width, imageSource.PixelSize.Height), new Rect(0, 0, pixelSize.Width, pixelSize.Height));
                return renderBitmap;
            }
            else
                return imageSource;
        }

        public void UpdateThumbnail()
        {
            Thumbnail = GenerateThumbnail();
        }

        public bool IsImageDirty()
        {
            return imageDirtyCallback(ImageSource);
        }

        public void ClearTagsDirty()
        {
            tagsDirty = false;
        }

        public bool AreTagsDirty()
        {
            return tagsDirty;
        }

        internal void ClearTags()
        {
            foreach (var tag in tags)
            {
                tag.TagChanged -= TagChangedHandler;
            }

            tags.Clear();
        }

        private void TagChangedHandler(string arg1, string arg2)
        {
            tagsDirty = true;
        }

        internal void InsertTag(int index, TagViewModel newTag)
        {
            newTag.TagChanged += TagChangedHandler;
            tags.Insert(index, newTag);
        }

        internal void AddTag(TagViewModel newTag)
        {
            newTag.TagChanged += TagChangedHandler;
            tags.Add(newTag);
        }

        internal void RemoveTagAt(int index)
        {
            var tag = tags[index];
            tag.TagChanged -= TagChangedHandler;
            tags.RemoveAt(index);
        }
    }
}
