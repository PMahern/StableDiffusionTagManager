using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using StableDiffusionTagManager.Models;
using StableDiffusionTagManager.ViewModels;
using StableDiffusionTagManager.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StableDiffusionTagManager
{
    public partial class App : Application
    {
        private static readonly string TEMP_DIRECTORY = "tmp";
        private static readonly string TAGS_PATH = "tags.csv";
        private static readonly string SETTINGS_FILE = "sdtmsettings.xml";

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static Settings Settings { get; set; } = new Settings(SETTINGS_FILE);

        public static string GetAppDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string GetTempFileDirectory()
        {
            var path = Path.Combine(GetAppDirectory(), TEMP_DIRECTORY);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        private List<TagCountModel> _tagDictionary = new List<TagCountModel>();
        private Task? tagsLoadTask;

        public void LoadTagDictionary()
        {
            if (File.Exists(TAGS_PATH))
            {
                _tagDictionary = File.ReadAllLines(TAGS_PATH)
                                    .Select(line =>
                                    {
                                        var pair = line.Split(',');
                                        return new TagCountModel
                                        {
                                            Tag = pair[0],
                                            Count = int.Parse(pair[1])
                                        };
                                    }).ToList();
            }
        }

        public async Task<IEnumerable<object>> SearchTags(string text, CancellationToken token)
        {

            var desktop = ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            await tagsLoadTask.WaitAsync(TimeSpan.FromMilliseconds(-1));
            if (desktop != null)
            {
                var mainWindow = desktop.MainWindow;
                var mainWindowVm = mainWindow.DataContext as MainWindowViewModel;
                if(mainWindowVm != null)
                {
                    var mainWindowTags = mainWindowVm.ImagesWithTags?.SelectMany(iwt => iwt.Tags.Where(a => a.Tag.StartsWith(text)).Select(a => a.Tag))
                                       .GroupBy(tag => tag)
                                       .OrderByDescending(t => t.Count())
                                       .Select(t => t.Key);

                    var tagDictionaryTags = _tagDictionary.Where(bt => bt.Tag.StartsWith(text)).Select(bt => bt.Tag);
                    
                    if (mainWindowTags != null)
                    {
                        return mainWindowTags.Union(tagDictionaryTags);
                    }  
                    
                    return tagDictionaryTags;
                }
            }

            return Enumerable.Empty<object>();   
        }

        public override void OnFrameworkInitializationCompleted()
        {
            tagsLoadTask = Task.Run(() => LoadTagDictionary());
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                //ExpressionObserver.DataValidators.RemoveAll(x => x is DataAnnotationsValidationPlugin);
                MainWindow window = new();
                desktop.MainWindow = window;
                var mainVM = new MainWindowViewModel();
                
                window.DataContext = mainVM;
                window.Closed += (sender, args) => desktop.Shutdown();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}