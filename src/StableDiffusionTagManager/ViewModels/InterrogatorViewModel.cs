using System;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.ViewModels
{
    public abstract class InterrogatorViewModel<T> : ValidatedViewModel
    {
        public abstract Task<T> Interrogate(byte[] imageData, Action<string> updateCallBack, Action<string> consoleCallBack);
    }
}
