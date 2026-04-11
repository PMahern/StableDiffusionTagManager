using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace ImageUtil.Interrogation
{
    public enum RemoteEndpointType
    {
        Ollama,
        KoboldCpp
    }

    public class RemoteInterrogatorArgs
    {
        public string EndpointUrl { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "";
        public string Prompt { get; set; } = "Describe this image in detail.";
        public RemoteEndpointType EndpointType { get; set; } = RemoteEndpointType.Ollama;
    }

    public class RemoteInterrogator : INaturalLanguageInterrogator<RemoteInterrogatorArgs>, ITagInterrogator<RemoteInterrogatorArgs>
    {
        private static readonly HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        private bool disposed = false;

        public Task Initialize(Action<string> updateCallBack, Action<string> consoleCallBack)
        {
            updateCallBack("Remote interrogator ready.");
            return Task.CompletedTask;
        }

        public async Task<string> CaptionImage(RemoteInterrogatorArgs args, byte[] imageData, Action<string> consoleCallBack)
        {
            var base64Image = Convert.ToBase64String(imageData);
            return await QueryRemote(args, base64Image, consoleCallBack);
        }

        public async Task<List<string>> TagImage(RemoteInterrogatorArgs args, byte[] imageData, Action<string> consoleCallBack)
        {
            var base64Image = Convert.ToBase64String(imageData);
            var result = await QueryRemote(args, base64Image, consoleCallBack);
            return result.Split(", ")
                         .Select(t => t.Trim().ToLower())
                         .Where(t => !string.IsNullOrWhiteSpace(t))
                         .ToList();
        }

        private Task<string> QueryRemote(RemoteInterrogatorArgs args, string base64Image, Action<string> consoleCallBack)
        {
            return args.EndpointType == RemoteEndpointType.Ollama
                ? QueryOllama(args, base64Image, consoleCallBack)
                : QueryKoboldCpp(args, base64Image, consoleCallBack);
        }

        private async Task<string> QueryOllama(RemoteInterrogatorArgs args, string base64Image, Action<string> consoleCallBack)
        {
            var url = args.EndpointUrl.TrimEnd('/') + "/api/generate";
            consoleCallBack($"Sending request to Ollama at {url}...");

            var request = new
            {
                model = args.Model,
                prompt = args.Prompt,
                images = new[] { base64Image },
                stream = false
            };

            var response = await httpClient.PostAsJsonAsync(url, request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("response").GetString() ?? string.Empty;
        }

        private async Task<string> QueryKoboldCpp(RemoteInterrogatorArgs args, string base64Image, Action<string> consoleCallBack)
        {
            var url = args.EndpointUrl.TrimEnd('/') + "/api/v1/generate";
            consoleCallBack($"Sending request to KoboldCpp at {url}...");

            var request = new
            {
                prompt = args.Prompt,
                max_length = 512,
                images = new[] { base64Image }
            };

            var response = await httpClient.PostAsJsonAsync(url, request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("results")[0].GetProperty("text").GetString() ?? string.Empty;
        }

        public void Dispose()
        {
            disposed = true;
        }

        public bool Disposed => disposed;
    }
}
