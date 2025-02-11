using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.Controls;
using StableDiffusionTagManager.Views;
using System.Threading.Tasks;

namespace StableDiffusionTagManager;

public partial class TextInputDialog : Window, IDialogWithResultAsync<string?>
{
    public TextInputDialog()
    {
        InitializeComponent();

        this.DataContext = this;
        Opened += TextInputDialog_Opened;
    }

    private bool success = false;

    private void TextInputDialog_Opened(object? sender, System.EventArgs e)
    {
        UserInputField.Focus();
    }

    public async Task<string?> ShowWithResult(Window parent)
    {
        await ShowDialog(parent);

        if (this.success && !string.IsNullOrEmpty(UserInputField.Text))
        {
            return UserInputField.Text;
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
        AvaloniaProperty.Register<TagSearchDialog, string>(nameof(DialogTitle), "Enter Text");

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
        if (e.Key == Key.Escape)
        {
            this.success = false;
            Close();
        }
    }

    public void TextInputKeyDown(object sender, KeyEventArgs e)
    {
        KeyDownHandler(e);
    }

    public void DialogKeyDown(object sender, KeyEventArgs e)
    {
        KeyDownHandler(e);
    }
}