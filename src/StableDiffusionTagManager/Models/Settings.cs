using Avalonia;
using ImageUtil;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace StableDiffusionTagManager.Models
{
    public class Settings : XmlSettingsFile
    {
        private const string ROOT_ELEMENT_NAME = "Settings";
        private const string STABLE_DIFFUSION_WEBUI_ADDRESS_ATTRIBUTE = "WebuiAddress";
        private const string PYTHON_PATH_ATTRIBUTE = "PythonPath";
        private const string IMAGE_ASPECT_RATIOS = "ImageAspectRatios";
        private const string IMAGE_ASPECT_RATIO_SET_ELEMENT = "ImageAspectRatioSet";
        private const string IMAGE_ASPECT_RATIO_SET_NAME_ATTRIBUTE = "Name";
        private const string IMAGE_RESOLUTION_ELEMENT = "Resolution";
        private const string IMAGE_RESOLUTION_X_ATTRIBUTE = "X";
        private const string IMAGE_RESOLUTION_Y_ATTRIBUTE = "Y";

        public Settings(string filename) : base(filename, ROOT_ELEMENT_NAME) { }

        protected override void LoadSettings(XDocument doc)
        {
            WebUiAddress = LoadSetting(doc, STABLE_DIFFUSION_WEBUI_ADDRESS_ATTRIBUTE, "http://localhost:7860");
            PythonPath = LoadSetting(doc, PYTHON_PATH_ATTRIBUTE, "python");
            LoadImageAspectRatios(doc);
        }

        private void LoadImageAspectRatios(XDocument doc)
        {
            var root = doc.Root;
            if (root != null)
            {
                var imageAspectRatioSetsElement = root.Element(IMAGE_ASPECT_RATIOS);
                if (imageAspectRatioSetsElement != null)
                {
                    ImageAspectRatioSets.Clear();
                    foreach (var imageAspectRatioSetElement in imageAspectRatioSetsElement.Elements(IMAGE_ASPECT_RATIO_SET_ELEMENT))
                    {
                        var name = imageAspectRatioSetElement.Attribute(IMAGE_ASPECT_RATIO_SET_NAME_ATTRIBUTE)?.Value;
                        if (name != null)
                        {
                            var aspectRatios = new List<(int, int)>();
                            foreach (var resolutionElement in imageAspectRatioSetElement.Elements(IMAGE_RESOLUTION_ELEMENT))
                            {
                                var x = int.Parse(resolutionElement.Attribute(IMAGE_RESOLUTION_X_ATTRIBUTE)?.Value ?? "0");
                                var y = int.Parse(resolutionElement.Attribute(IMAGE_RESOLUTION_Y_ATTRIBUTE)?.Value ?? "0");
                                aspectRatios.Add((x, y));
                            }
                            ImageAspectRatioSets.Add((name, aspectRatios));
                        }
                    }
                }
            }
        }

        private void SaveImageAspectRatios(XDocument doc)
        {
            var root = doc.Root;
            if (root != null)
            {
                var imageAspectRatioSets = new XElement(IMAGE_ASPECT_RATIOS);
                foreach (var imageAspectRatioSet in ImageAspectRatioSets)
                {
                    var imageAspectRatioSetElement = new XElement(IMAGE_ASPECT_RATIO_SET_ELEMENT);
                    imageAspectRatioSetElement.SetAttributeValue(IMAGE_ASPECT_RATIO_SET_NAME_ATTRIBUTE, imageAspectRatioSet.Item1);
                    foreach(var ar in  imageAspectRatioSet.Item2)
                    {
                        var imageResolutionElement = new XElement(IMAGE_RESOLUTION_ELEMENT);
                        imageResolutionElement.SetAttributeValue(IMAGE_RESOLUTION_X_ATTRIBUTE, ar.Item1);
                        imageResolutionElement.SetAttributeValue(IMAGE_RESOLUTION_Y_ATTRIBUTE, ar.Item2);
                        imageAspectRatioSetElement.Add(imageResolutionElement);
                    }
                    imageAspectRatioSets.Add(imageAspectRatioSetElement);
                }
                root.Add(imageAspectRatioSets);
            }
        }

        protected override void AddSettings(XDocument doc)
        {
            SaveSetting(doc, STABLE_DIFFUSION_WEBUI_ADDRESS_ATTRIBUTE, WebUiAddress);
            SaveSetting(doc, PYTHON_PATH_ATTRIBUTE, Utility.PythonPath);
            SaveImageAspectRatios(doc);
        }

        public string? WebUiAddress { get; set; } = "http://localhost:7860";

        public string? PythonPath { get => Utility.PythonPath; set => Utility.PythonPath = value; }

        public List<(string, List<(int, int)>)> ImageAspectRatioSets { get; set; } = new List<(string, List<(int, int)>)>
        {
            ("SDXL", new List<(int, int)>()
            {
                (1024, 1024),
                (1152, 896),
                (896, 1152),
                (1216, 832),
                (832, 1216),
                (1344, 768),
                (768, 1344),
                (1536, 640),
                (640, 1536)
            })
        };
    }
}
