﻿using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SixLabors.ImageSharp;
using System;
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
        private string? imageFile = null;
        private string? description = null; // Added field for storing the English description of the image

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
            this.imageFile = imageFile;
            Description = tagSet.Description;
            tags = new ObservableCollection<TagViewModel>(tagSet.Tags.Select(t => new TagViewModel(t)
            {
                TagChanged = TagChangedHandler
            }));
            tags.CollectionChanged += SetTagsDirty;
        }

        public ImageWithTagsViewModel(Bitmap image, string newFileName, Func<Bitmap, bool> imageDirtyCallback, string? description, IEnumerable<string>? tags = null) : this(newFileName, imageDirtyCallback)
        {
            imageSource = image;

            this.description = description;
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

        public Action<string>? TagRemoved;
        public Action<string>? TagEntered;

        private Bitmap? imageSource;
        public Bitmap ImageSource
        {
            get
            {
                if (imageSource == null && this.imageFile != null)
                {
                    using var stream = File.OpenRead(imageFile);
                    imageSource = new Bitmap(imageFile);
                }
                if (imageSource != null)
                {
                    return imageSource;
                }
                throw new Exception("No bitmap was generated for a target image.");
            }
            set
            {
                imageSource = value;
                OnPropertyChanged();
            }
        }

        private Bitmap? thumbnail = null;
        public Bitmap Thumbnail
        {
            get
            {
                if (thumbnail == null)
                {
                    if (imageSource != null)
                    {
                        thumbnail = GenerateThumbnail();
                    }
                    else if (this.imageFile != null)
                    {
                        using var stream = File.OpenRead(imageFile);
                        thumbnail = Bitmap.DecodeToHeight(stream, THUMBNAIL_SIZE);
                    }
                }
                if (thumbnail != null)
                {
                    return thumbnail;
                }
                throw new Exception("No thumbnail was generated for a target image.");
            }
        }

        private string filename;
        public string Filename { get => filename; set => SetProperty(ref filename, value); }

        private ObservableCollection<TagViewModel> tags;

        public ReadOnlyObservableCollection<TagViewModel> Tags
        {
            get { return new ReadOnlyObservableCollection<TagViewModel>(tags); }
        }


        public Action? CompletionStatusChanged;

        private bool isComplete;
        public bool IsComplete
        {
            get => isComplete;
            set
            {
                if (SetProperty(ref isComplete, value))
                {
                    CompletionStatusChanged?.Invoke();
                }
            }
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

        public void RemoveTag(string tag)
        {
            var findResult = this.tags.FirstOrDefault(t => t.Tag == tag);
            if (findResult != null)
            {
                this.tags.Remove(findResult);
                TagRemoved?.Invoke(findResult.Tag);
            }
        }

        public string GetTagsFileName()
        {
            return $"{Path.GetFileNameWithoutExtension(Filename)}.txt";
        }

        public Bitmap GenerateThumbnail()
        {
            if (imageSource.PixelSize.Width > THUMBNAIL_SIZE || imageSource.PixelSize.Height > THUMBNAIL_SIZE)
            {
                var aspectRatio = (float)imageSource.PixelSize.Width / (float)imageSource.PixelSize.Height;
                var pixelSize = aspectRatio > 1.0 ? new PixelSize(THUMBNAIL_SIZE, (int)((float)THUMBNAIL_SIZE / aspectRatio)) : new PixelSize((int)((float)THUMBNAIL_SIZE * aspectRatio), THUMBNAIL_SIZE);
                var renderBitmap = new RenderTargetBitmap(pixelSize);
                using var dc = renderBitmap.CreateDrawingContext();
                dc.DrawImage(imageSource, new Rect(0, 0, imageSource.PixelSize.Width, imageSource.PixelSize.Height), new Rect(0, 0, pixelSize.Width, pixelSize.Height));
                return renderBitmap;
            }
            else
                return imageSource;
        }

        public void UpdateThumbnail()
        {
            thumbnail = GenerateThumbnail();
            OnPropertyChanged(nameof(Thumbnail));
        }

        public bool IsImageDirty()
        {
            return imageSource != null && imageDirtyCallback(imageSource);
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
            TagEntered?.Invoke(arg2);
        }

        internal void InsertTag(int index, TagViewModel newTag)
        {
            newTag.TagChanged += TagChangedHandler;
            tags.Insert(index, newTag);
            TagEntered?.Invoke(newTag.Tag);
        }

        internal void AddTag(TagViewModel newTag)
        {
            if (!Tags.Any(t => t.Tag == newTag.Tag))
            {
                newTag.TagChanged += TagChangedHandler;
                tags.Add(newTag);
                TagEntered?.Invoke(newTag.Tag);
            }
        }

        internal void RemoveTagAt(int index)
        {
            var tag = tags[index];
            tag.TagChanged -= TagChangedHandler;
            tags.RemoveAt(index);
            TagRemoved?.Invoke(tag.Tag);
        }

        internal void AddTagIfNotExists(TagViewModel tagViewModel)
        {
            if (!Tags.Any(t => t.Tag == tagViewModel.Tag))
            {
                AddTag(tagViewModel);
                TagEntered?.Invoke(tagViewModel.Tag);
            }
        }

        [RelayCommand]
        public void AddTagIfNotExists(string tag)
        {
            this.AddTagIfNotExists(new TagViewModel(tag));
        }

        internal void ApplyTagOrdering<TKey>(Func<string, TKey> orderBy)
        {
            var newOrder = tags.OrderBy(t => orderBy(t.Tag)).ToList();
            tags.Clear();
            foreach (var tag in newOrder)
            {
                tags.Add(tag);
            }
        }

        internal void ReplaceTagIfExists(string target, string toReplaceWith)
        {
            // Remove the target tag, if we find the replacement tag remove it as well, we want to put the new tag in the old tags location
            var found = Tags.FirstOrDefault(t => t.Tag == target);
            if (found != null)
            {
                RemoveTag(toReplaceWith);
                found.Tag = toReplaceWith;
            }
        }

        public string? Description
        {
            get => description;
            set
            {
                if (SetProperty(ref description, value))
                {
                    tagsDirty = true;
                }
            }
        }
    }
}
