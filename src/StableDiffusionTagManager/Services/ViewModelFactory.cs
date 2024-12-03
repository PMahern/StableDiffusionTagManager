using Microsoft.Extensions.DependencyInjection;
using StableDiffusionTagManager.Attributes;
using StableDiffusionTagManager.ViewModels;
using System;

namespace StableDiffusionTagManager.Services
{
    [Service]
    public class ViewModelFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ViewModelFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T CreateViewModel<T>() where T : ViewModelBase
        {
            return _serviceProvider.GetRequiredService<T>();
        }
    }
}
