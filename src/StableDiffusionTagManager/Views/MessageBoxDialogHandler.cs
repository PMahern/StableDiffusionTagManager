using Avalonia.Controls;
using MessageBox.Avalonia.BaseWindows.Base;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.Views
{
    public class MessageBoxDialogHandler
    {
        private MainWindow window;

        public MessageBoxDialogHandler(MainWindow window)
        {
            this.window = window;
        }

        public Task<TResultType> ShowDialog<TResultType>(IMsBoxWindow<TResultType> msgBox)
        {
            return msgBox.ShowDialog(window);
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
