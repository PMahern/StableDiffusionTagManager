using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace ImageUtil
{
    internal class SWTagger : ITagInterrogator
    {
        private readonly string model;
        private PythonImageEngine pythonImageEngine;
        private bool initialized = false;
        private bool disposed = false;

        public SWTagger(string model)
        {
            this.model = model;
        }

        public void Dispose()
        {
            disposed = true;
            pythonImageEngine?.Dispose();
        }

        public async Task Initialize(Action<string> updateCallBack, Action<string> consoleCallBack)
        {
            if (!initialized)
            {
                string repoUrl = "https://huggingface.co/spaces/SmilingWolf/wd-tagger";
                var absoluteWorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Join("taggers", "sw"));

                await Utility.CloneRepo(repoUrl, absoluteWorkingDirectory, updateCallBack);

                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "python", "sw", "interrogate.py"), Path.Join(absoluteWorkingDirectory, "interrogate.py"), true);

                var additionalPackages = new List<string> { "gradio" };
                await Utility.CreateVenv(absoluteWorkingDirectory, updateCallBack, consoleCallBack, "requirements.txt", additionalPackages);

                pythonImageEngine = new PythonImageEngine("interrogate.py", $"{model} 0.5", Path.Join("taggers", "sw"), true);
            }
            initialized = true;
        }

        public async Task<List<string>> TagImage(byte[] imageData, float threshold, Action<string> consoleCallBack)
        {
            if (!initialized)
                throw new InvalidOperationException("Tried to access an unitialized SW Tagger.");

            if (disposed)
                throw new ObjectDisposedException("Tried to access a disposed SW Tagger.");

            await pythonImageEngine.SendImage(imageData, consoleCallBack);
            var output = await pythonImageEngine.WaitForGenerationResultString(consoleCallBack);
            // We use Trim to remove any leading white spaces
            var match = Regex.Match(output, @"^\('([^']*)',");

            if (match.Success)
            {
                string listPart = match.Groups[1].Value;
                return new List<string>(listPart.Split(new[] { ", " }, StringSplitOptions.None));
            }
            else
            {
                return new List<string>();
            }
        }
    }
}
