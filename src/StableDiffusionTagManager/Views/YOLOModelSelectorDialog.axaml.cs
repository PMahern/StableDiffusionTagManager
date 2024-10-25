using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.Controls;
using StableDiffusionTagManager.ViewModels;
using System.Threading.Tasks;

namespace StableDiffusionTagManager;

public partial class YOLOModelSelectorDialog : Window, IDialogWithResultAsync<string?>
{
    public bool Success { get; set; } = false;
    public string? Result { get; set; }
    public async Task<string?> ShowWithResult(Window parent)
    {
        await ShowDialog(parent);
        return Success ? selectorViewModel?.SelectedModel?.Filename : null;
    }

    private YOLOModelSelectorViewModel selectorViewModel;
    public YOLOModelSelectorViewModel SelectorViewModel
    {
        get => selectorViewModel;
    }

    public YOLOModelSelectorDialog()
    {
        this.DataContext = this;
        selectorViewModel = new YOLOModelSelectorViewModel();

        InitializeComponent();
    }

    [RelayCommand]
    public void Generate()
    {
        if (SelectorViewModel?.SelectedModel != null)
        {
            Success = true;
            Close();
        }
    }

    [RelayCommand]
    public void Cancel()
    {
        Close();
    }
}