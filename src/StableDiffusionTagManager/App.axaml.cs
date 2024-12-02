using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using StableDiffusionTagManager.Models;
using StableDiffusionTagManager.ViewModels;
using StableDiffusionTagManager.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

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

                // Register all the services needed for the application to run
                var collection = new ServiceCollection();
                collection.AddCommonServices();
                collection.AddSingleton(new Settings(SETTINGS_FILE));
                // Creates a ServiceProvider containing services from the provided IServiceCollection
                var services = collection.BuildServiceProvider();
                var factory = services.GetRequiredService<ViewModelFactory>();
                var mainVM = factory.CreateViewModel<MainWindowViewModel>();

                desktop.MainWindow = window;

                window.DataContext = mainVM;
                window.Closed += (sender, args) => desktop.Shutdown();

                if (desktop.Args != null && desktop.Args.Any())
                {
                    var path = Path.GetFullPath(desktop.Args[0]);
                    window.Opened += (sender, args) => Dispatcher.UIThread.InvokeAsync(() => mainVM.LoadFolder(path));
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}