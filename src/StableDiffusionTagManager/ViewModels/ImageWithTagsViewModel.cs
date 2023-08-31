using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SixLabors.ImageSharp;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using TagUtil;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class ImageWithTagsViewModel : ObservableObject
    {
        const int THUMBNAIL_SIZE = 200;

        public ImageWithTagsViewModel(string imageFile, TagSet tagSet)
        {
            using var stream = File.OpenRead(imageFile);
            thumbnail = Bitmap.DecodeToHeight(stream, THUMBNAIL_SIZE);
            imageSource = new Bitmap(imageFile);

            filename = Path.GetFileName(imageFile);
            tags = new ObservableCollection<TagViewModel>(tagSet.Tags.Select(t => new TagViewModel(t) { TagChanged = (oldtag, newtag) => TagChanged?.Invoke(oldtag, newtag) })); ;
            IsFromCrop = false;
        }

        public Action<string, string>? TagChanged;
        public Action<string>? TagRemoved;

        public ImageWithTagsViewModel(Bitmap image, string newFileName)
        {
            imageSource = image;
            if (image.PixelSize.Width > THUMBNAIL_SIZE || image.PixelSize.Height > THUMBNAIL_SIZE)
            {
                var aspectRatio = (float)image.PixelSize.Width / (float)image.PixelSize.Height;
                var pixelSize = aspectRatio > 1.0 ? new PixelSize(THUMBNAIL_SIZE, (int)((float)THUMBNAIL_SIZE / aspectRatio)) : new PixelSize((int)((float)THUMBNAIL_SIZE * aspectRatio), THUMBNAIL_SIZE);
                var renderBitmap = new RenderTargetBitmap(pixelSize);
                using var dc = renderBitmap.CreateDrawingContext(null);
                using var drawingContext = new DrawingContext(dc, false);
                drawingContext.DrawImage(image, new Rect(0, 0, image.PixelSize.Width, image.PixelSize.Height), new Rect(0, 0, pixelSize.Width, pixelSize.Height));
                thumbnail = renderBitmap;
            }
            else
                thumbnail = image;


            filename = filename = Path.GetFileName(newFileName);
            tags = new ObservableCollection<TagViewModel>();
        }

        public bool IsFromCrop { get; }

        private Bitmap imageSource;
        public Bitmap ImageSource { get => imageSource; set { imageSource = value; OnPropertyChanged(); } }

        private Bitmap thumbnail;
        public Bitmap Thumbnail { get => thumbnail; set { thumbnail = value; OnPropertyChanged(); } }

        private string filename;
        public string Filename { get => filename; set => SetProperty(ref filename, value); }

        private ObservableCollection<TagViewModel> tags;

        public ObservableCollection<TagViewModel> Tags
        {
            get { return tags; }
            set { tags = value; OnPropertyChanged(); }
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
    }
}
