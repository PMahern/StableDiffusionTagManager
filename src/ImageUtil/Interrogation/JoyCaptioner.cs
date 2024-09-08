using System.Runtime.InteropServices;

namespace ImageUtil
{
    public class JoyCaptioner : INaturalLanguageInterrogator
    {
        private readonly string model;
        private PythonImageEngine pythonImageEngine;
        private bool initialized = false;
        private bool disposed = false;

        public async Task Initialize(Action<string> updateCallBack, Action<string> consoleCallBack)
        {
            if (!initialized)
            {
                string repoUrl = "https://huggingface.co/spaces/fancyfeast/joy-caption-pre-alpha";
                var absoluteWorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Join("taggers", "joycaption"));

                await Utility.CloneRepo(repoUrl, absoluteWorkingDirectory, updateCallBack);

                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "python", "joycaption", "interrogate.py"), Path.Join(absoluteWorkingDirectory, "interrogate.py"), true);

                var additionalPackages = new List<string> { "Pillow", "spaces", "protobuf", "bitsandbytes" };

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    additionalPackages.Add("torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124");
                }

                await Utility.CreateVenv(absoluteWorkingDirectory, updateCallBack, consoleCallBack, "requirements.txt", additionalPackages);

                pythonImageEngine = new PythonImageEngine("interrogate.py", "", Path.Join("taggers", "joycaption"), true, consoleCallBack);
            }
            initialized = true;
        }

        public async Task<string> CaptionImage(string prompt, byte[] imageData)
        {
            if (!initialized)
                throw new InvalidOperationException("Tried to access an uninitialized Joy Captioner.");

            if (disposed)
                throw new ObjectDisposedException("Tried to access a disposed Joy Captioner.");

            await pythonImageEngine.SendString(prompt);

            return await pythonImageEngine.SendImage(imageData);
        }

        public void Dispose()
        {
            disposed = true;
            pythonImageEngine.Dispose();
        }
    }
}
