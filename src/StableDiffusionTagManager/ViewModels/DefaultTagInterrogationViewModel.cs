using CommunityToolkit.Mvvm.ComponentModel;
using ImageUtil;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class DefaultTagInterrogationViewModel : InterrogatorViewModel<List<string>>
    {
        public DefaultTagInterrogationViewModel(Func<ITagInterrogator> interrogatorFactory)
        {
            this.interrogatorFactory = interrogatorFactory;
        }

        public override async Task<List<string>> Interrogate(byte[] imageData, Action<string> updateCallBack, Action<string> consoleCallBack)
        {
            var interrogator = interrogatorFactory.Invoke();
            await interrogator.Initialize(updateCallBack, consoleCallBack);
            return await interrogator.TagImage(imageData, Threshold, consoleCallBack);
        }

        [ObservableProperty]
        public float threshold = 0.5f;
        private readonly Func<ITagInterrogator> interrogatorFactory;

        public override bool IsValid => Threshold >= 0 && Threshold <= 1;
    }
}
