using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using StableDiffusionTagManager.Services;
using StableDiffusionTagManager.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;

namespace StableDiffusionTagManager
{
    public static class ServiceCollectionExtensions
    {
        public static void AddCommonServices(this IServiceCollection collection)
        {
            collection.Scan(collection => collection
                .FromAssemblyOf<Program>()
                .AddClasses(classes => classes.AssignableTo<ViewModelBase>())
                .AsSelf()
                .WithTransientLifetime());

            collection.Scan(collection => collection
                .FromAssemblyOf<Program>()
                .AddClasses(classes => classes.WithAttribute<ServiceAttribute>())
                .AsSelf()
                .WithTransientLifetime());

            collection.Scan(collection => collection
                .FromAssemblyOf<Program>()
                .AddClasses(classes => classes.AssignableTo<ITaggerViewModelFactory>())
                .AsImplementedInterfaces()
                .WithTransientLifetime());

            collection.Scan(collection => collection
               .FromAssemblyOf<Program>()
               .AddClasses(classes => classes.AssignableTo<INaturalLanguageInterrogatorViewModelFactory>())
               .AsImplementedInterfaces()
               .WithTransientLifetime());

            collection.AddTransient<ViewModelFactory>();
        }
    }
}
