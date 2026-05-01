using CommunityToolkit.Mvvm.ComponentModel;
using ImageUtil.Interrogation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class RemoteTagInterrogationViewModel : InterrogatorViewModel<List<string>>, IFolderAwareInterrogationViewModel<List<string>>
    {
        public static IReadOnlyList<string> EndpointTypes { get; } = new[]
        {
            nameof(RemoteEndpointType.Ollama),
            nameof(RemoteEndpointType.KoboldCpp),
            nameof(RemoteEndpointType.OpenAI)
        };

        [ObservableProperty]
        private string endpointUrl = "http://localhost:11434";

        [ObservableProperty]
        private string model = "llava:13b";

        [ObservableProperty]
        private string prompt = "List the tags that describe this image as a comma-separated list. Use Danbooru-style tags.";

        [ObservableProperty]
        private string selectedEndpointType = nameof(RemoteEndpointType.Ollama);

        [ObservableProperty]
        private string apiKey = "";

        [ObservableProperty]
        private bool isOpenAiEndpoint = false;

        partial void OnSelectedEndpointTypeChanged(string value)
        {
            IsOpenAiEndpoint = value == nameof(RemoteEndpointType.OpenAI);
            EndpointUrl = value switch
            {
                nameof(RemoteEndpointType.KoboldCpp) => "http://localhost:5001",
                nameof(RemoteEndpointType.OpenAI) => "https://api.openai.com",
                _ => "http://localhost:11434"
            };
        }

        public override bool IsValid =>
            !string.IsNullOrWhiteSpace(EndpointUrl) && !string.IsNullOrWhiteSpace(Prompt);

        public Func<byte[], Action<string>, Action<string>, Task<List<string>>> GetFolderInterrogateOperation(
            string? effectivePrompt, string? effectiveEndpointUrl)
        {
            var interrogator = new RemoteInterrogator();
            var args = BuildArgs(
                !string.IsNullOrWhiteSpace(effectivePrompt) ? effectivePrompt : Prompt,
                !string.IsNullOrWhiteSpace(effectiveEndpointUrl) ? effectiveEndpointUrl : EndpointUrl);
            return (imageData, update, console) => interrogator.TagImage(args, imageData, console);
        }

        private RemoteInterrogatorArgs BuildArgs(string prompt, string endpointUrl) => new RemoteInterrogatorArgs
        {
            EndpointUrl = endpointUrl,
            Model = Model,
            Prompt = prompt,
            ApiKey = ApiKey,
            EndpointType = SelectedEndpointType switch
            {
                nameof(RemoteEndpointType.KoboldCpp) => RemoteEndpointType.KoboldCpp,
                nameof(RemoteEndpointType.OpenAI) => RemoteEndpointType.OpenAI,
                _ => RemoteEndpointType.Ollama
            }
        };

        public override ConfiguredInterrogationContext<List<string>> CreateInterrogationContext()
        {
            var interrogator = new RemoteInterrogator();
            var args = BuildArgs(Prompt, EndpointUrl);
            return new ConfiguredInterrogationContext<List<string>>(
                interrogator,
                interrogator.Initialize,
                (imageData, updateCallback, consoleCallback) => interrogator.TagImage(args, imageData, consoleCallback));
        }
    }
}
