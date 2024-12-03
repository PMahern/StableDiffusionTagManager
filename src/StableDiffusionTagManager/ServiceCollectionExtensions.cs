using Microsoft.Extensions.DependencyInjection;
using StableDiffusionTagManager.Attributes;
using StableDiffusionTagManager.Services;
using StableDiffusionTagManager.ViewModels;
using System.Linq;
using System.Runtime.InteropServices;

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
                .AddClasses(classes => classes.AssignableTo<ITaggerViewModelFactory>().WithoutAttribute<SupportedPlatformsAttribute>())
                .AsImplementedInterfaces()
                .WithTransientLifetime());

            collection.Scan(collection => collection
                .FromAssemblyOf<Program>()
                .AddClasses(classes => classes.AssignableTo<ITaggerViewModelFactory>()
                                              .WithAttribute<SupportedPlatformsAttribute>(attr => attr.Platforms.Any(platform => RuntimeInformation.IsOSPlatform(OSPlatform.Create(platform))))
                            )
                .AsImplementedInterfaces()
                .WithTransientLifetime());

            collection.Scan(collection => collection
               .FromAssemblyOf<Program>()
               .AddClasses(classes => classes.AssignableTo<INaturalLanguageInterrogatorViewModelFactory>().WithoutAttribute<SupportedPlatformsAttribute>())
               .AsImplementedInterfaces()
               .WithTransientLifetime());

            collection.Scan(collection => collection
               .FromAssemblyOf<Program>()
               .AddClasses(classes => classes.AssignableTo<INaturalLanguageInterrogatorViewModelFactory>()
                                              .WithAttribute<SupportedPlatformsAttribute>(attr => attr.Platforms.Any(platform => RuntimeInformation.IsOSPlatform(OSPlatform.Create(platform))))
                            )
               .AsImplementedInterfaces()
               .WithTransientLifetime());

            collection.AddTransient<ViewModelFactory>();
        }
    }
}
