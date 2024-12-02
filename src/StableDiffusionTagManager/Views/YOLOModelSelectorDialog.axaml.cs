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
        var viewModel = DataContext as YOLOModelSelectorDialogViewModel;
        if(viewModel != null && viewModel.SelectorViewModel.SelectedModel != null)
        {
            return Success ? (viewModel.SelectorViewModel.SelectedModel.Filename, viewModel.SelectorViewModel.Threshold, viewModel.SelectorViewModel.ExpandMask) : null;
        }
        return null;
    }

    public YOLOModelSelectorDialog()
    {
        InitializeComponent();
    }

    private void GenerateClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var viewModel = DataContext as YOLOModelSelectorDialogViewModel;
        if (viewModel != null && viewModel.SelectorViewModel?.SelectedModel != null)
        {
            Success = true;
            Close();
        }
    }

    private void CancelClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}