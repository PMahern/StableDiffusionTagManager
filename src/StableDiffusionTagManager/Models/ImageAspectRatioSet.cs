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

        public double GetClosestAspectRatio(double aspect)
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

            return closestResolution?.ImageAspectRatio ?? 0.0;
        }
    }
}
