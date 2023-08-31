using Avalonia.Controls;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia;
using UVtools.AvaloniaControls;
using CommunityToolkit.Mvvm.Input;

namespace StableDiffusionTagManager.Views
{
    public partial class TagSearchDialog : Window
    {
        public TagSearchDialog()
        {
            InitializeComponent();

            Title = "SearchTags";
            this.DataContext = this;
            //This is a hack but just calling Focus here doesn't do anything unless the app loses focus and gains it back... very strange
            TagAutoComplete.AttachedToVisualTree += (s, e) => Dispatcher.UIThread.Post(() => TagAutoComplete.Focus());     
        }

        public void SetSearchFunc(Func<string, CancellationToken, Task<IEnumerable<object>>> searchFunc)
        {
            this.searchFunc = searchFunc;
        }        

        private bool success = false;

        private Func<string, CancellationToken, Task<IEnumerable<object>>>? searchFunc;

        public Task<IEnumerable<object>> SearchTags(string text, CancellationToken token)
        {
            if(this.searchFunc != null)
            {
                return searchFunc(text, token);
            }
            
            return Task.FromResult(Enumerable.Empty<object>());
        }

        public async Task<string?> ShowAsync(Window parent)
        {
            await ShowDialog(parent);

            if (this.success && !string.IsNullOrEmpty(TagAutoComplete.Text))
            {
                return TagAutoComplete.Text;
            }
            else
            {
                return null;
            }
        }

        public void Cancel_Clicked(object sender, RoutedEventArgs e)
        {
            this.success = false;
            Close();
        }

        public void Ok_Clicked(object sender, RoutedEventArgs e)
        {
            this.success = true;
            Close();
        }

        public static readonly StyledProperty<string> DialogTitleProperty =
            AvaloniaProperty.Register<TagSearchDialog, string>(nameof(DialogTitle), "Lookup Tag");

        public string DialogTitle
        {
            get => GetValue(DialogTitleProperty);
            set => SetValue(DialogTitleProperty, value);
        }

        [RelayCommand]
        public void HeaderClose()
        {
            this.success = false;
            Close();
        }
    }
}
