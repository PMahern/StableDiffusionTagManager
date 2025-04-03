using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using StableDiffusionTagManager.ViewModels;
using System;
using System.Collections.ObjectModel;

namespace StableDiffusionTagManager.Views
{
    public partial class ImageAspectRatioSet : ViewModelBase
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private ObservableCollection<ImageResolution> resolutions = new();

        private ImageResolution? FindClosest(double aspect)
        {
            ImageResolution closestResolution = null;
            double closestDifference = double.MaxValue;

            foreach (var resolution in Resolutions)
            {
                double resolutionAspect = resolution.ImageAspectRatio;
                double difference = Math.Abs(resolutionAspect - aspect);

                if (difference < closestDifference)
                {
                    closestDifference = difference;
                    closestResolution = resolution;
                }
            }

            return closestResolution;
        }
        public double GetClosestAspectRatio(double aspect)
        {
            return FindClosest(aspect)?.ImageAspectRatio ?? 0.0;
        }

        internal PixelSize? GetClosesResolution(double aspect)
        {
            var closest = FindClosest(aspect);

            if (closest != null)
            {
                return new PixelSize(closest.X, closest.Y);
            }

            return null;
        }
    }
}
