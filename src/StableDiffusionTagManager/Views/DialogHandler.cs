using Avalonia.Controls;
using MessageBox.Avalonia.BaseWindows.Base;
using StableDiffusionTagManager.Controls;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.Views
{
    public class DialogHandler
    {
        private MainWindow window;

        public DialogHandler(MainWindow window)
        {
            this.window = window;
        }

        public Task<TResultType> ShowDialog<TResultType>(IMsBoxWindow<TResultType> msgBox)
        {
            return msgBox.ShowDialog(window);
        }

        public TResultType ShowDialog<TResultType>(IDialogWithResult<TResultType> dialogWithResult)
        {
            return dialogWithResult.ShowWithResult(window);
        }

        public Task<TResultType> ShowDialog<TResultType>(IDialogWithResultAsync<TResultType> dialogWithResult)
        {
            return dialogWithResult.ShowWithResult(window);
        }

        public Task<TResultType> ShowDialog<TResultType>(Window dialog)
        {
            return dialog.ShowDialog<TResultType>(window);
        }

        public Task ShowDialog(Window dialog)
        {
            return dialog.ShowDialog(window);
        }
    }
}
