using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageUtil.Interrogation;
using ImageUtil.Segmentation;
using System;
using System.Collections.Generic;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class ImageSegmentationViewModel : ViewModelBase
    {
        public event EventHandler? RequestClose;

        public static IReadOnlyList<string> EndpointTypes { get; } = new[]
        {
            nameof(RemoteEndpointType.Ollama),
            nameof(RemoteEndpointType.KoboldCpp),
            nameof(RemoteEndpointType.OpenAI)
        };

        [ObservableProperty]
        private int selectedTabIndex = 0;

        [ObservableProperty]
        private string selectedEndpointType = nameof(RemoteEndpointType.Ollama);

        [ObservableProperty]
        private string endpointUrl = "http://localhost:11434";

        [ObservableProperty]
        private string model = "llava:13b";

        [ObservableProperty]
        private string prompt = "detect all panels, output only ```json";

        [ObservableProperty]
        private string apiKey = "";

        [ObservableProperty]
        private int maxImageDimension = 1024;

        partial void OnSelectedEndpointTypeChanged(string value)
        {
            EndpointUrl = value switch
            {
                nameof(RemoteEndpointType.KoboldCpp) => "http://localhost:5001",
                nameof(RemoteEndpointType.OpenAI) => "https://api.openai.com",
                _ => "http://localhost:11434"
            };
        }

        public bool UseLlm => SelectedTabIndex == 1;

        public bool Success { get; set; } = false;

        public LlmSegmentationArgs GetLlmArgs() => new LlmSegmentationArgs
        {
            EndpointUrl = EndpointUrl,
            Model = Model,
            Prompt = Prompt,
            ApiKey = ApiKey,
            MaxImageDimension = MaxImageDimension,
            EndpointType = SelectedEndpointType switch
            {
                nameof(RemoteEndpointType.KoboldCpp) => RemoteEndpointType.KoboldCpp,
                nameof(RemoteEndpointType.OpenAI) => RemoteEndpointType.OpenAI,
                _ => RemoteEndpointType.Ollama
            }
        };

        [RelayCommand]
        public void Ok()
        {
            Success = true;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        public void Cancel()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
