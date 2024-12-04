using ImageUtil.Interrogation;
using System;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.ViewModels
{
    public abstract class InterrogatorViewModel<T> : ValidatedViewModel
    {
        public abstract ConfiguredInterrogationContext<T> CreateInterrogationContext();
    }
}
