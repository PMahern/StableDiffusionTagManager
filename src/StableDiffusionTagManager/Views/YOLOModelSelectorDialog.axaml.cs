using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using StableDiffusionTagManager.Controls;
using StableDiffusionTagManager.ViewModels;
using System.Threading.Tasks;

namespace StableDiffusionTagManager;

public partial class YOLOModelSelectorDialog : Window, IDialogWithResultAsync<(string modelPath, float threshold, int expandMask)?>
{
    public bool Success { get; set; } = false;
    public (string modelPath, float threshold, int expandMask)? Result { get; set; }
    public async Task<(string modelPath, float threshold, int expandMask)?> ShowWithResult(Window parent)
    {
        await ShowDialog(parent);
        return Success ? (selectorViewModel?.SelectedModel.Filename, selectorViewModel.Threshold, selectorViewModel.ExpandMask)  : null;
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