using Avalonia;
using System.Collections.Generic;
using System.Xml.Linq;

namespace StableDiffusionTagManager.Models
{
    public class Settings : XmlSettingsFile
    {
        private const string ROOT_ELEMENT_NAME = "Settings";
        private const string STABLE_DIFFUSION_WEBUI_ADDRESS_ATTRIBUTE = "WebuiAddress";
        private const string PYTHON_PATH_ATTRIBUTE = "PythonPath";

        public Settings(string filename) : base(filename, ROOT_ELEMENT_NAME) { }

        protected override void LoadSettings(XDocument doc)
        {
            WebUiAddress = LoadSetting(doc, STABLE_DIFFUSION_WEBUI_ADDRESS_ATTRIBUTE, "http://localhost:7860");
            PythonPath = LoadSetting(doc, PYTHON_PATH_ATTRIBUTE, "python");
        }

        protected override void AddSettings(XDocument doc)
        {
            SaveSetting(doc, STABLE_DIFFUSION_WEBUI_ADDRESS_ATTRIBUTE, WebUiAddress);
            SaveSetting(doc, PYTHON_PATH_ATTRIBUTE, PythonPath);
        }

        public string? WebUiAddress { get; set; } = "http://localhost:7860";

        public string? PythonPath { get; set; } = "python";
    }
}
