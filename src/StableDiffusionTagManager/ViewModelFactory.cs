using Microsoft.Extensions.DependencyInjection;
using StableDiffusionTagManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StableDiffusionTagManager
{
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
