using Avalonia;
using SdWebUpApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace StableDiffusionTagManager.Models
{
    public class Project : XmlSettingsFile
    {
        private const string ROOT_ELEMENT_NAME = "Project";
        private const string BACKED_UP_IMAGES_SETTING = "BackedUpImages";
        private const string BACKED_UP_IMAGE_ELEMENT = "BackedUpImage";
        private const string BACKED_UP_IMAGE_OLD_ATTRIBUTE = "OldImage";
        private const string BACKED_UP_IMAGE_NEW_ATTRIBUTE = "NewImage";
        private const string TAG_COLLECTIONS_ELEMENTS = "TagCollections";
        private const string TAG_COLLECTION_ELEMENT = "TagCollection";
        private const string TAG_COLLECTION_NAME_ATTRIBUTE = "Name";
        private const string TAG_ELEMENT = "Tag";
        private const string TARGET_IMAGE_WIDTH_ATTRIBUTE = "TargetImageWidth";
        private const string TARGET_IMAGE_HEIGHT_ATTRIBUTE = "TargetImageWidth";
        private const string DEFAULT_PROMPT_PREFIX_ATTRIBUTE = "DefaultPromptPrefix";
        private const string DEFAULT_NEGATIVE_PROMPT_ATTRIBUTE = "DefaultNegativePrompt";
        private const string DEFAULT_DENOISE_STRENGTH_ATTRIBUTE = "DefaultDenoiseStrength";
        private const string ACTIVATION_KEYWORD_ATTRIBUTE = "ActivationKeyword";
        private const string COMPLETED_IMAGES_ELEMENT = "CompletedImages";
        private const string COMPLETED_IMAGE_ELEMENT = "Image";

        private string? defaultPromptPrefix = null;
        private string? defaultNegativePrompt = null;
        private decimal? defaultDenoiseStrength = 0.5M;
        private string? activationKeyword = null;
        private PixelSize? targetImageSize = null;
        private InterrogateMethod interrogateMethod = InterrogateMethod.DeepDanBooru;
        private List<TagCollection> tagCollections = new List<TagCollection>();
        private List<(string oldfile, string newfile)> backedUpFileMaps = new List<(string, string)>();
        private List<string> completedImages = new List<string>();

        public Action? ProjectUpdated { get; set; }

        public Project(string filename) : base(filename, ROOT_ELEMENT_NAME) { }

        protected override void LoadSettings(XDocument doc)
        {
            LoadTargetImageSize(doc);
            LoadBackedUpFileMaps(doc);
            LoadTagCollections(doc);
            LoadDefaultPromptSettings(doc);
            LoadCompletedImages(doc);
        }

        protected override void AddSettings(XDocument doc)
        {
            SaveDefaultPromptSettings(doc);
            SaveTargetImageSize(doc);
            SaveBackedUpFileMaps(doc);
            SaveTagCollections(doc);
            SaveCompletedImages(doc);
        }

        public string? DefaultPromptPrefix
        {
            get => defaultPromptPrefix;
            set
            {
                defaultPromptPrefix = value;
                ProjectUpdated?.Invoke();
            }
        }
        public string? DefaultNegativePrompt
        {
            get => defaultNegativePrompt;
            set
            {
                defaultNegativePrompt = value;
                ProjectUpdated?.Invoke();
            }
        }
        public decimal? DefaultDenoiseStrength
        {
            get => defaultDenoiseStrength;
            set
            {
                defaultDenoiseStrength = value;
                ProjectUpdated?.Invoke();
            }
        }
        public string? ActivationKeyword
        {
            get => activationKeyword;
            set
            {
                activationKeyword = value;
                ProjectUpdated?.Invoke();
            }
        }

        public PixelSize? TargetImageSize
        {
            get => targetImageSize;
            set
            {
                targetImageSize = value;
                ProjectUpdated?.Invoke();
            }
        }
        public InterrogateMethod InterrogateMethod
        {
            get => interrogateMethod; 
            set
            {
                interrogateMethod = value;
                ProjectUpdated?.Invoke();
            }
        }
        public List<TagCollection> TagCollections
        {
            get => tagCollections; 
            set
            {
                tagCollections = value;
                ProjectUpdated?.Invoke();
            }
        }
        public List<(string oldfile, string newfile)> BackedUpFileMaps
        {
            get => backedUpFileMaps; 
            set
            {
                backedUpFileMaps = value;
                ProjectUpdated?.Invoke();
            }
        }

        public List<string> CompletedImages
        {
            get => completedImages;
            set
            {
                completedImages = value;
                ProjectUpdated?.Invoke();
            }
        }


        public void LoadDefaultPromptSettings(XDocument doc)
        {
            DefaultPromptPrefix = LoadSetting(doc, DEFAULT_PROMPT_PREFIX_ATTRIBUTE);
            DefaultNegativePrompt = LoadSetting(doc, DEFAULT_NEGATIVE_PROMPT_ATTRIBUTE);
            DefaultDenoiseStrength = LoadSetting<decimal>(doc, DEFAULT_DENOISE_STRENGTH_ATTRIBUTE);
            ActivationKeyword = LoadSetting(doc, ACTIVATION_KEYWORD_ATTRIBUTE);
        }

        public void SaveDefaultPromptSettings(XDocument doc)
        {
            SaveSetting(doc, DEFAULT_PROMPT_PREFIX_ATTRIBUTE, DefaultPromptPrefix);
            SaveSetting(doc, DEFAULT_NEGATIVE_PROMPT_ATTRIBUTE, DefaultNegativePrompt);
            SaveSetting(doc, DEFAULT_DENOISE_STRENGTH_ATTRIBUTE, DefaultDenoiseStrength);
            SaveSetting(doc, ACTIVATION_KEYWORD_ATTRIBUTE, ActivationKeyword);
        }


        public void LoadTargetImageSize(XDocument doc)
        {
            var width = LoadSetting<int>(doc, TARGET_IMAGE_WIDTH_ATTRIBUTE);
            var height = LoadSetting<int>(doc, TARGET_IMAGE_HEIGHT_ATTRIBUTE);

            if (width.HasValue && height.HasValue)
            {
                TargetImageSize = new PixelSize(width.Value, height.Value);
            }
        }

        public void SaveTargetImageSize(XDocument doc)
        {
            if (TargetImageSize != null)
            {
                SaveSetting(doc, TARGET_IMAGE_WIDTH_ATTRIBUTE, TargetImageSize.Value.Width);
                SaveSetting(doc, TARGET_IMAGE_HEIGHT_ATTRIBUTE, TargetImageSize.Value.Height);
            }
        }

        public void AddBackedUpFileMap(string oldFileName, string newFileName)
        {
            this.BackedUpFileMaps.Add((oldFileName, newFileName));
        }

        public void SaveBackedUpFileMaps(XDocument doc)
        {
            var root = doc.Root;
            if (root != null)
            {
                var backedUpImages = new XElement(BACKED_UP_IMAGES_SETTING);
                foreach (var imageMap in BackedUpFileMaps)
                {
                    var imageElement = new XElement(BACKED_UP_IMAGE_ELEMENT);
                    imageElement.SetAttributeValue(BACKED_UP_IMAGE_OLD_ATTRIBUTE, imageMap.oldfile);
                    imageElement.SetAttributeValue(BACKED_UP_IMAGE_NEW_ATTRIBUTE, imageMap.newfile);
                    backedUpImages.Add(imageElement);
                }

                root.Add(backedUpImages);
            }
        }

        public void LoadBackedUpFileMaps(XDocument doc)
        {
            var root = doc.Root;
            if (root != null)
            {
                var backedUpImages = root.Element(BACKED_UP_IMAGES_SETTING);
                if (backedUpImages != null)
                {
                    BackedUpFileMaps = backedUpImages.Elements(BACKED_UP_IMAGE_ELEMENT)
                                                     .Select(e => (e.Attribute(BACKED_UP_IMAGE_OLD_ATTRIBUTE).Value, e.Attribute(BACKED_UP_IMAGE_NEW_ATTRIBUTE).Value))
                                                     .ToList();
                }
            }
        }

        public void LoadTagCollections(XDocument doc)
        {
            var root = doc.Root;
            if (root != null)
            {
                var tagCollections = root.Element(TAG_COLLECTIONS_ELEMENTS);
                if (tagCollections != null)
                {
                    var collections = tagCollections.Elements(TAG_COLLECTION_ELEMENT).ToList();

                    this.TagCollections = collections.Select(c => new TagCollection
                    {
                        Name = c.Attribute(TAG_COLLECTION_NAME_ATTRIBUTE).Value,
                        Tags = c.Elements(TAG_ELEMENT).Select(e => e.Value).ToList()
                    }).ToList();
                }
            }
        }

        public void SaveTagCollections(XDocument doc)
        {
            var collectionsElement = new XElement(TAG_COLLECTIONS_ELEMENTS);
            foreach (var collection in TagCollections)
            {
                var collectionElement = new XElement(TAG_COLLECTION_ELEMENT);
                collectionElement.SetAttributeValue(TAG_COLLECTION_NAME_ATTRIBUTE, collection.Name);
                foreach (var tag in collection.Tags)
                {
                    collectionElement.Add(new XElement(TAG_ELEMENT) { Value = tag });
                }

                collectionsElement.Add(collectionElement);
            }

            doc.Root?.Add(collectionsElement);
        }

        public void SetImageCompletionStatus(string filename, bool completionStatus)
        {
            if(completionStatus)
            {
                this.completedImages.Add(filename);
            } else {
                this.completedImages.Remove(filename);
            }
        }

        public void LoadCompletedImages(XDocument doc)
        {
            var root = doc.Root;
            if (root != null)
            {
                var completedImages = root.Element(COMPLETED_IMAGES_ELEMENT);
                if (completedImages != null)
                {
                    var images = completedImages.Elements(COMPLETED_IMAGE_ELEMENT).ToList();

                    this.CompletedImages = images.Select(i => i.Value).ToList();
                }
            }
        }

        public void SaveCompletedImages(XDocument doc)
        {
            var imagesElement = new XElement(COMPLETED_IMAGES_ELEMENT);
            foreach (var completedImage in completedImages)
            {
                var imageElement = new XElement(COMPLETED_IMAGE_ELEMENT);
                imageElement.SetValue(completedImage);
                imagesElement.Add(imageElement);
            }

            doc.Root?.Add(imagesElement);
        }
    }
}
