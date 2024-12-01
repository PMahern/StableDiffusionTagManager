using System.Runtime.InteropServices;

namespace ImageUtil.Interrogation
{
    public class JoyCaptionAlphaTwo : INaturalLanguageInterrogator, ITagInterrogator
    {
        private readonly string model;
        private PythonImageEngine pythonImageEngine;
        private bool initialized = false;
        private bool disposed = false;

        public async Task Initialize(Action<string> updateCallBack, Action<string> consoleCallBack)
        {
            if (!initialized)
            {
                string repoUrl = "https://huggingface.co/spaces/fancyfeast/joy-caption-alpha-two";
                var absoluteWorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Join("taggers", "joycaptionalphatwo"));

                await Utility.CloneRepo(repoUrl, absoluteWorkingDirectory, updateCallBack);

                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "python", "joycaptionalphatwo", "interrogate.py"), Path.Join(absoluteWorkingDirectory, "interrogate.py"), true);

                var additionalPackages = new List<string> { "Pillow", "spaces", "protobuf", "bitsandbytes" };

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    additionalPackages.Add("torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124");
                }

                await Utility.CreateVenv(absoluteWorkingDirectory, updateCallBack, consoleCallBack, "requirements.txt", additionalPackages);


                // Search and replace in .yaml files
                var yamljsonFiles = Directory.GetFiles(absoluteWorkingDirectory, "*.yaml", SearchOption.AllDirectories)
                                            .Concat(Directory.GetFiles(absoluteWorkingDirectory, "*.json", SearchOption.AllDirectories)).ToArray();
                foreach (string file in yamljsonFiles)
                {
                    Utility.FindAndReplace(file, "meta-llama/Meta-Llama-3.1-8B", "unsloth/Meta-Llama-3.1-8B-bnb-4bit");
                }

                pythonImageEngine = new PythonImageEngine("interrogate.py", "", Path.Join("taggers", "joycaptionalphatwo"), true);
            }
            initialized = true;
        }

        public async Task<string> CaptionImage(string prompt, byte[] imageData, Action<string> consoleCallBack)
        {
            if (!initialized)
                throw new InvalidOperationException("Tried to access an uninitialized Joy Captioner Alpha One.");

            if (disposed)
                throw new ObjectDisposedException("Tried to access a disposed Joy Captioner Alpha One.");

            await pythonImageEngine.SendString("descriptive");

            await pythonImageEngine.SendImage(imageData, consoleCallBack);
            return await pythonImageEngine.WaitForGenerationResultString(consoleCallBack);
        }

        public async Task<List<string>> TagImage(byte[] imageData, float threshold, Action<string> consoleCallBack)
        {
            await pythonImageEngine.SendString("rng-tags");

            await pythonImageEngine.SendImage(imageData, consoleCallBack);
            var results = await pythonImageEngine.WaitForGenerationResultString(consoleCallBack);
            return results.Split(", ").ToList();
        }

        public void Dispose()
        {
            disposed = true;
            pythonImageEngine.Dispose();
        }

        public bool Disposed => disposed;
    }
}
