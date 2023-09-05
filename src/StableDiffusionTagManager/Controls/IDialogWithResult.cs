using Avalonia.Controls;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.Controls
{
    public interface IDialogWithResult<TResult>
    {
        public TResult ShowWithResult(Window parent);
    }

    public interface IDialogWithResultAsync<TResult>
    {
        public Task<TResult> ShowWithResult(Window parent);
    }
}
