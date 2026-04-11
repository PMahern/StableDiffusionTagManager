using Avalonia;
using SdWebUiApi;
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
        private const string TAG_COLLECTIONS_ELEMENT = "TagCollections";
        private const string TAG_COLLECTION_ELEMENT = "TagCollection";
        private const string TAG_CATEGORIES_ELEMENT = "TagCategories";
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
        private const string CONCEPTS_ELEMENT = "Concepts";
        private const string CONCEPT_ELEMENT = "Concept";
        private const string CONCEPT_KEY_ATTRIBUTE = "Key";
        private const string CONCEPT_VALUE_ATTRIBUTE = "Value";
        private const string DEFAULT_NL_INTERROGATION_PROMPT_ATTRIBUTE = "DefaultNaturalLanguageInterrogationPrompt";
        private const string DEFAULT_TAG_INTERROGATION_PROMPT_ATTRIBUTE = "DefaultTagInterrogationPrompt";
        private const string DEFAULT_INTERROGATION_ENDPOINT_URL_ATTRIBUTE = "DefaultInterrogationEndpointUrl";
        private const string RESPONSE_STRIP_PAIRS_ELEMENT = "ResponseStripPairs";
        private const string RESPONSE_STRIP_PAIR_ELEMENT = "StripPair";
        private const string STRIP_PAIR_OPEN_ATTRIBUTE = "Open";
        private const string STRIP_PAIR_CLOSE_ATTRIBUTE = "Close";

        private string? defaultPromptPrefix = null;
        private string? defaultNegativePrompt = null;
        private decimal? defaultDenoiseStrength = 0.5M;
        private string? activationKeyword = null;
        private PixelSize? targetImageSize = null;
        private string? defaultNaturalLanguageInterrogationPrompt = null;
        private string? defaultTagInterrogationPrompt = null;
        private string? defaultInterrogationEndpointUrl = null;
        private List<(string Open, string Close)> responseStripPairs = new List<(string, string)>();
        private List<TagCollection> tagCollections = new List<TagCollection>();
        private List<(string oldfile, string newfile)> backedUpFileMaps = new List<(string, string)>();
        private List<string> completedImages = new List<string>();
        private Dictionary<string, string> concepts = new Dictionary<string, string>();

        public Action? ProjectUpdated { get; set; }

        public Project(string filename) : base(filename, ROOT_ELEMENT_NAME) { }

        protected override void LoadSettings(XDocument doc)
        {
            LoadTargetImageSize(doc);
            LoadBackedUpFileMaps(doc);
            LoadTagCollections(doc);
            LoadDefaultPromptSettings(doc);
            LoadCompletedImages(doc);
            LoadConcepts(doc);
            LoadResponseStripPairs(doc);
        }

        protected override void AddSettings(XDocument doc)
        {
            SaveDefaultPromptSettings(doc);
            SaveTargetImageSize(doc);
            SaveBackedUpFileMaps(doc);
            SaveTagCollections(doc);
            SaveCompletedImages(doc);
            SaveConcepts(doc);
            SaveResponseStripPairs(doc);
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

        public string? DefaultNaturalLanguageInterrogationPrompt
        {
            get => defaultNaturalLanguageInterrogationPrompt;
            set
            {
                defaultNaturalLanguageInterrogationPrompt = value;
                ProjectUpdated?.Invoke();
            }
        }

        public string? DefaultTagInterrogationPrompt
        {
            get => defaultTagInterrogationPrompt;
            set
            {
                defaultTagInterrogationPrompt = value;
                ProjectUpdated?.Invoke();
            }
        }

        public string? DefaultInterrogationEndpointUrl
        {
            get => defaultInterrogationEndpointUrl;
            set
            {
                defaultInterrogationEndpointUrl = value;
                ProjectUpdated?.Invoke();
            }
        }

        public List<(string Open, string Close)> ResponseStripPairs
        {
            get => responseStripPairs;
            set
            {
                responseStripPairs = value;
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

        public Dictionary<string, string> Concepts
        {
            get => concepts;
            set
            {
                concepts = value;
                ProjectUpdated?.Invoke();
            }
        }

        public void LoadDefaultPromptSettings(XDocument doc)
        {
            DefaultPromptPrefix = LoadSetting(doc, DEFAULT_PROMPT_PREFIX_ATTRIBUTE);
            DefaultNegativePrompt = LoadSetting(doc, DEFAULT_NEGATIVE_PROMPT_ATTRIBUTE);
            DefaultDenoiseStrength = LoadSetting<decimal>(doc, DEFAULT_DENOISE_STRENGTH_ATTRIBUTE);
            ActivationKeyword = LoadSetting(doc, ACTIVATION_KEYWORD_ATTRIBUTE);
            DefaultNaturalLanguageInterrogationPrompt = LoadSetting(doc, DEFAULT_NL_INTERROGATION_PROMPT_ATTRIBUTE);
            DefaultTagInterrogationPrompt = LoadSetting(doc, DEFAULT_TAG_INTERROGATION_PROMPT_ATTRIBUTE);
            DefaultInterrogationEndpointUrl = LoadSetting(doc, DEFAULT_INTERROGATION_ENDPOINT_URL_ATTRIBUTE);
        }

        public void SaveDefaultPromptSettings(XDocument doc)
        {
            SaveSetting(doc, DEFAULT_PROMPT_PREFIX_ATTRIBUTE, DefaultPromptPrefix);
            SaveSetting(doc, DEFAULT_NEGATIVE_PROMPT_ATTRIBUTE, DefaultNegativePrompt);
            SaveSetting(doc, DEFAULT_DENOISE_STRENGTH_ATTRIBUTE, DefaultDenoiseStrength);
            SaveSetting(doc, ACTIVATION_KEYWORD_ATTRIBUTE, ActivationKeyword);
            SaveSetting(doc, DEFAULT_NL_INTERROGATION_PROMPT_ATTRIBUTE, DefaultNaturalLanguageInterrogationPrompt);
            SaveSetting(doc, DEFAULT_TAG_INTERROGATION_PROMPT_ATTRIBUTE, DefaultTagInterrogationPrompt);
            SaveSetting(doc, DEFAULT_INTERROGATION_ENDPOINT_URL_ATTRIBUTE, DefaultInterrogationEndpointUrl);
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
                var tagCollections = root.Element(TAG_COLLECTIONS_ELEMENT);
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
            var collectionsElement = new XElement(TAG_COLLECTIONS_ELEMENT);
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

        public void LoadResponseStripPairs(XDocument doc)
        {
            var root = doc.Root;
            if (root == null) return;
            var el = root.Element(RESPONSE_STRIP_PAIRS_ELEMENT);
            if (el == null) return;
            ResponseStripPairs = el.Elements(RESPONSE_STRIP_PAIR_ELEMENT)
                .Select(e => (
                    e.Attribute(STRIP_PAIR_OPEN_ATTRIBUTE)?.Value ?? string.Empty,
                    e.Attribute(STRIP_PAIR_CLOSE_ATTRIBUTE)?.Value ?? string.Empty))
                .Where(p => !string.IsNullOrEmpty(p.Item1))
                .ToList();
        }

        public void SaveResponseStripPairs(XDocument doc)
        {
            var el = new XElement(RESPONSE_STRIP_PAIRS_ELEMENT);
            foreach (var (open, close) in responseStripPairs)
            {
                var pair = new XElement(RESPONSE_STRIP_PAIR_ELEMENT);
                pair.SetAttributeValue(STRIP_PAIR_OPEN_ATTRIBUTE, open);
                pair.SetAttributeValue(STRIP_PAIR_CLOSE_ATTRIBUTE, close);
                el.Add(pair);
            }
            doc.Root?.Add(el);
        }

        public void LoadConcepts(XDocument doc)
        {
            var root = doc.Root;
            if (root != null)
            {
                var conceptsElement = root.Element(CONCEPTS_ELEMENT);
                if (conceptsElement != null)
                {
                    Concepts = conceptsElement.Elements(CONCEPT_ELEMENT)
                        .Where(e => e.Attribute(CONCEPT_KEY_ATTRIBUTE)?.Value != null)
                        .ToDictionary(
                            e => e.Attribute(CONCEPT_KEY_ATTRIBUTE)!.Value,
                            e => e.Attribute(CONCEPT_VALUE_ATTRIBUTE)?.Value ?? string.Empty
                        );
                }
            }
        }

        public void SaveConcepts(XDocument doc)
        {
            var conceptsElement = new XElement(CONCEPTS_ELEMENT);
            foreach (var concept in concepts)
            {
                var conceptElement = new XElement(CONCEPT_ELEMENT);
                conceptElement.SetAttributeValue(CONCEPT_KEY_ATTRIBUTE, concept.Key);
                conceptElement.SetAttributeValue(CONCEPT_VALUE_ATTRIBUTE, concept.Value);
                conceptsElement.Add(conceptElement);
            }

            doc.Root?.Add(conceptsElement);
        }
    }
}
