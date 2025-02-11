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
using StableDiffusionTagManager.Controls;
using Avalonia.Input;

namespace StableDiffusionTagManager.Views
{
    public partial class TagSearchDialog : Window, IDialogWithResultAsync<string?>
    {
        public TagSearchDialog()
        {
            InitializeComponent();

            Title = "SearchTags";
            this.DataContext = this;
            Opened += TagSearchDialog_Opened;
        }

        private void TagSearchDialog_Opened(object? sender, EventArgs e)
        {
            TagAutoComplete.Focus();
        }

        private bool success = false;

        public async Task<string?> ShowWithResult(Window parent)
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

        private void KeyDownHandler(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.success = true;
                Close();
            }
            if(e.Key == Key.Escape) {
                this.success = false;
                Close();
            }
        }

        public void AutoCompleteKeyDown(object sender, KeyEventArgs e)
        {
            KeyDownHandler(e);
        }

        public void DialogKeyDown(object sender, KeyEventArgs e)
        {
            KeyDownHandler(e);
        }
    }
}
