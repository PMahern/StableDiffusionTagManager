namespace StableDiffusionTagManager.Services
{
    public interface ICurrentProjectDefaults
    {
        string? NaturalLanguageInterrogationPrompt { get; set; }
        string? TagInterrogationPrompt { get; set; }
        string? InterrogationEndpointUrl { get; set; }
    }

    public class CurrentProjectDefaults : ICurrentProjectDefaults
    {
        public string? NaturalLanguageInterrogationPrompt { get; set; }
        public string? TagInterrogationPrompt { get; set; }
        public string? InterrogationEndpointUrl { get; set; }
    }
}
