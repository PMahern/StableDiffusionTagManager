using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaEdit.Utils;
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

        public ImageWithTagsViewModel(string imageFile, TagSet tagSet, Func<Bitmap, bool> imageDirtyCallback)
        {
            using var stream = File.OpenRead(imageFile);
            this.imageDirtyCallback = imageDirtyCallback;
            thumbnail = Bitmap.DecodeToHeight(stream, THUMBNAIL_SIZE);
            imageSource = new Bitmap(imageFile);

            filename = Path.GetFileName(imageFile);

            tags = new ObservableCollection<TagViewModel>(tagSet.Tags.Select(t => new TagViewModel(t)
            {
                TagChanged = TagChangedHandler
            }));
            tags.CollectionChanged += SetTagsDirty;
            IsFromCrop = false;

        }

        public ImageWithTagsViewModel(Bitmap image, string newFileName, Func<Bitmap, bool> imageDirtyCallback, IEnumerable<string>? tags = null)
        {
            imageSource = image;
            thumbnail = GenerateThumbnail();
            this.imageDirtyCallback = imageDirtyCallback;

            filename = filename = Path.GetFileName(newFileName);
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

        public bool IsFromCrop { get; }

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

        internal void ApplyTagOrdering<TKey>(Func<string, TKey> orderBy)
        {
            var newOrder = this.tags.OrderBy(t => orderBy(t.Tag)).ToList();
            this.tags.Clear();
            this.tags.AddRange(newOrder);
        }
    }
}
