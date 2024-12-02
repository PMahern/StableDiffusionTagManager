using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StableDiffusionTagManager.ViewModels
{
    public class YOLOModelSelectorItem
    {
        public YOLOModelSelectorItem(string filename)
        {
            Filename = filename;
            Name = Path.GetFileName(filename);
        }

        public string Filename { get; set; }
        public string Name { get; set; }
    }

    public partial class YOLOModelSelectorViewModel : ViewModelBase
    {
        [ObservableProperty]
        List<YOLOModelSelectorItem> models;
        [ObservableProperty]
        private YOLOModelSelectorItem? selectedModel;
        [ObservableProperty]
        private float threshold = 0.5f;
        [ObservableProperty]
        public int expandMask = 0;

        public YOLOModelSelectorViewModel()
        {
            string yoloModelDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yolomodels");

            if(Directory.Exists(yoloModelDirectory))
            { 
                models = Directory.GetFiles(yoloModelDirectory, "*.pt").Select(file => new YOLOModelSelectorItem(file)).ToList();
                SelectedModel = models.FirstOrDefault();
            }
        }
    }
}
