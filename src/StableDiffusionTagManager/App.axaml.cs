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

namespace StableDiffusionTagManager
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static Settings Settings { get; set; } = new Settings("sdtmsettings.xml");
        public static string GetAppDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                ExpressionObserver.DataValidators.RemoveAll(x => x is DataAnnotationsValidationPlugin);
                MainWindow window = new();
                desktop.MainWindow = window;
                var mainVM = new MainWindowViewModel()
                {
                    ShowFolderDialogCallback = () => new OpenFolderDialog().ShowAsync(desktop.MainWindow),
                    FocusTagCallback = (tag) =>
                    {
                        window.FocusTagAutoComplete(tag);
                    }
                };

                
                window.DataContext = mainVM;
                mainVM.ShowAddTagToAllDialogCallback = (title) =>
                {
                    var dialog = new TagSearchDialog();

                    if (title != null)
                    {
                        dialog.Title = title;
                    }
                    dialog.SetSearchFunc(mainVM.SearchTags);
                    return dialog.ShowAsync(desktop.MainWindow);
                };

                window.Closed += (sender, args) => desktop.Shutdown();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}