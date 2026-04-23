using ImageUtil.Interrogation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace ImageUtil.Segmentation
{
    public class LlmSegmentationArgs
    {
        public string EndpointUrl { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "";
        public string Prompt { get; set; } = "detect all panels, output only ```json";
        public RemoteEndpointType EndpointType { get; set; } = RemoteEndpointType.Ollama;
        public string ApiKey { get; set; } = "";
        public int MaxImageDimension { get; set; } = 1024;
    }

    public class PolygonInfo
    {
        public IReadOnlyList<(double X, double Y)> Points { get; set; } = Array.Empty<(double, double)>();
    }

    public class LlmImageSegmentor
    {
        private static readonly HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

        public async Task<IEnumerable<PolygonInfo>> GetImageSegments(string imagePath, LlmSegmentationArgs args, int imageWidth, int imageHeight)
        {
            var imageBytes = await File.ReadAllBytesAsync(imagePath);
            var base64Image = Convert.ToBase64String(imageBytes);

            string responseText = args.EndpointType switch
            {
                RemoteEndpointType.KoboldCpp => await QueryKoboldCpp(args, base64Image),
                RemoteEndpointType.OpenAI => await QueryOpenAI(args, base64Image),
                _ => await QueryOllama(args, base64Image)
            };

            // Compute the dimensions the LLM server actually sees after its internal rescaling
            int effectiveWidth = imageWidth;
            int effectiveHeight = imageHeight;
            if (args.MaxImageDimension > 0 && (imageWidth > args.MaxImageDimension || imageHeight > args.MaxImageDimension))
            {
                double scale = Math.Min((double)args.MaxImageDimension / imageWidth, (double)args.MaxImageDimension / imageHeight);
                effectiveWidth = (int)(imageWidth * scale);
                effectiveHeight = (int)(imageHeight * scale);
            }

            return ParseSegments(responseText, imageWidth, imageHeight, effectiveWidth, effectiveHeight);
        }

        private async Task<string> QueryOllama(LlmSegmentationArgs args, string base64Image)
        {
            var url = args.EndpointUrl.TrimEnd('/') + "/api/generate";
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

        private async Task<string> QueryKoboldCpp(LlmSegmentationArgs args, string base64Image)
        {
            var url = args.EndpointUrl.TrimEnd('/') + "/api/v1/generate";
            var request = new
            {
                prompt = args.Prompt,
                max_length = 2048,
                images = new[] { base64Image }
            };
            var response = await httpClient.PostAsJsonAsync(url, request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("results")[0].GetProperty("text").GetString() ?? string.Empty;
        }

        private async Task<string> QueryOpenAI(LlmSegmentationArgs args, string base64Image)
        {
            var url = args.EndpointUrl.TrimEnd('/') + "/v1/chat/completions";
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            if (!string.IsNullOrWhiteSpace(args.ApiKey))
                requestMessage.Headers.Add("Authorization", $"Bearer {args.ApiKey}");

            var requestBody = new
            {
                model = args.Model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "image_url", image_url = new { url = $"data:image/png;base64,{base64Image}" } },
                            new { type = "text", text = args.Prompt }
                        }
                    }
                }
            };

            requestMessage.Content = JsonContent.Create(requestBody);
            var response = await httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        }

        private static IEnumerable<PolygonInfo> ParseSegments(string responseText, int imageWidth, int imageHeight, int effectiveWidth, int effectiveHeight)
        {
            // Extract the content of the first ```json ... ``` code block if present,
            // otherwise fall back to the raw response text.
            var codeBlockMatch = Regex.Match(responseText, @"```json\s*([\s\S]*?)```");
            var jsonText = codeBlockMatch.Success ? codeBlockMatch.Groups[1].Value.Trim() : responseText.Trim();

            using var doc = JsonDocument.Parse(jsonText);

            var rawPolygons = doc.RootElement.EnumerateArray()
                .Select(e =>
                {
                    // Gemini-style box_2d: [ymin, xmin, ymax, xmax] — convert to 4-point rectangle
                    if (e.TryGetProperty("box_2d", out var boxEl))
                    {
                        var c = boxEl.EnumerateArray().ToArray();
                        double ymin = c[0].GetDouble(), xmin = c[1].GetDouble();
                        double ymax = c[2].GetDouble(), xmax = c[3].GetDouble();
                        return new List<(double, double)> { (xmin, ymin), (xmax, ymin), (xmax, ymax), (xmin, ymax) };
                    }

                    // x/y/width/height bounding box — convert to 4-point rectangle
                    if (e.TryGetProperty("x", out _))
                    {
                        double x = e.GetProperty("x").GetDouble();
                        double y = e.GetProperty("y").GetDouble();
                        double w = e.GetProperty("width").GetDouble();
                        double h = e.GetProperty("height").GetDouble();
                        return new List<(double, double)> { (x, y), (x + w, y), (x + w, y + h), (x, y + h) };
                    }

                    // Polygon format: {points: [[x,y], ...]} or {points: [{x,y}, ...]}
                    return e.GetProperty("points").EnumerateArray()
                        .Select(pt =>
                        {
                            if (pt.ValueKind == JsonValueKind.Array)
                            {
                                var arr = pt.EnumerateArray().ToArray();
                                return (arr[0].GetDouble(), arr[1].GetDouble());
                            }
                            return (pt.GetProperty("x").GetDouble(), pt.GetProperty("y").GetDouble());
                        })
                        .ToList();
                })
                .ToList();

            double maxCoord = rawPolygons.SelectMany(pts => pts.SelectMany(p => new[] { p.Item1, p.Item2 }))
                                         .DefaultIfEmpty(0).Max();

            return rawPolygons.Select(pts => new PolygonInfo
            {
                Points = maxCoord <= 1.0
                    // Ratios: multiply directly by image dimensions
                    ? (IReadOnlyList<(double, double)>)pts.Select(p => (p.Item1 * imageWidth, p.Item2 * imageHeight)).ToList()
                    : maxCoord <= 1000.0
                        // 0-1000 scale: divide by 1000 then multiply by image dimensions
                        ? pts.Select(p => (p.Item1 * imageWidth / 1000.0, p.Item2 * imageHeight / 1000.0)).ToList()
                        // Pixel coordinates: scale from effective (server-resized) space to original
                        : pts.Select(p => (p.Item1 * imageWidth / effectiveWidth, p.Item2 * imageHeight / effectiveHeight)).ToList()
            });
        }
    }
}
