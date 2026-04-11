using CommunityToolkit.Mvvm.ComponentModel;
using ImageUtil.Interrogation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class RemoteNaturalLanguageInterrogationViewModel : InterrogatorViewModel<string>, IFolderAwareInterrogationViewModel<string>
    {
        public static IReadOnlyList<string> EndpointTypes { get; } = new[]
        {
            nameof(RemoteEndpointType.Ollama),
            nameof(RemoteEndpointType.KoboldCpp)
        };

        [ObservableProperty]
        private string endpointUrl = "http://localhost:11434";

        [ObservableProperty]
        private string model = "llava:13b";

        [ObservableProperty]
        private string prompt = "Describe this image in detail.";

        [ObservableProperty]
        private string selectedEndpointType = nameof(RemoteEndpointType.Ollama);

        public override bool IsValid =>
            !string.IsNullOrWhiteSpace(EndpointUrl) && !string.IsNullOrWhiteSpace(Prompt);

        public Func<byte[], Action<string>, Action<string>, Task<string>> GetFolderInterrogateOperation(
            string? effectivePrompt, string? effectiveEndpointUrl)
        {
            var interrogator = new RemoteInterrogator();
            var args = BuildArgs(
                !string.IsNullOrWhiteSpace(effectivePrompt) ? effectivePrompt : Prompt,
                !string.IsNullOrWhiteSpace(effectiveEndpointUrl) ? effectiveEndpointUrl : EndpointUrl);
            return (imageData, update, console) => interrogator.CaptionImage(args, imageData, console);
        }

        private RemoteInterrogatorArgs BuildArgs(string prompt, string endpointUrl) => new RemoteInterrogatorArgs
        {
            EndpointUrl = endpointUrl,
            Model = Model,
            Prompt = prompt,
            EndpointType = SelectedEndpointType == nameof(RemoteEndpointType.KoboldCpp)
                ? RemoteEndpointType.KoboldCpp
                : RemoteEndpointType.Ollama
        };

        public override ConfiguredInterrogationContext<string> CreateInterrogationContext()
        {
            var interrogator = new RemoteInterrogator();
            var args = BuildArgs(Prompt, EndpointUrl);
            return new ConfiguredInterrogationContext<string>(
                interrogator,
                interrogator.Initialize,
                (imageData, updateCallback, consoleCallback) => interrogator.CaptionImage(args, imageData, consoleCallback));
        }
    }
}
