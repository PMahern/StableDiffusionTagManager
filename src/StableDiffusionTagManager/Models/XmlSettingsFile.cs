using System;
using System.IO;
using System.Xml.Linq;

namespace StableDiffusionTagManager.Models
{
    public abstract class XmlSettingsFile
    {
        private string filename;
        private readonly string rootelementname;

        public XmlSettingsFile(string filename, string rootelementname)
        {
            this.filename = filename;
            this.rootelementname = rootelementname;
            Load();
        }

        public void Load()
        {
            if (File.Exists(filename))
            {
                var doc = XDocument.Load(filename);

                LoadSettings(doc);
            }
        }

        public void Save()
        {
            XDocument doc = new XDocument();

            doc.Add(new XElement(rootelementname));
            AddSettings(doc);
            doc.Save(filename);
        }

        protected abstract void AddSettings(XDocument doc);
        protected abstract void LoadSettings(XDocument doc);

        protected string? LoadSetting(XDocument doc, string name)
        {
            return doc.Root?.Attribute(name)?.Value;
        }

        protected Nullable<TargetType> LoadSetting<TargetType>(XDocument doc, string name)
            where TargetType : struct
        {
            var value = doc.Root?.Attribute(name)?.Value;
            if (value != null)
            {
                return (TargetType?)Convert.ChangeType(value, typeof(TargetType));
            }

            return null;
        }

        protected void SaveSetting(XDocument doc, string name, string? value)
        {
            if (value != null)
            {
                doc.Root?.SetAttributeValue(name, value);
            }
        }

        protected void SaveSetting<TargetType>(XDocument doc, string name, TargetType value)
        {
            doc.Root?.SetAttributeValue(name, value?.ToString());
        }
    }
}
