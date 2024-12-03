
using System.Runtime.InteropServices;

namespace ImageUtil.Interrogation
{
    public class CogVLM2Interrogator : INaturalLanguageInterrogator<string>
    {
        private readonly string model;
        private PythonImageEngine pythonImageEngine;
        private bool initialized = false;
        private bool disposed = false;

        public async Task Initialize(Action<string> updateCallBack, Action<string> consoleCallBack)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                throw new Exception("Attempted to initialize CogVLM2 interrogator on non linux platform. CogVLM2 is only supported on Linux.");
            }

            if (!initialized)
            {
                var relativeWorkingDirectory = Path.Join("taggers", "cogvlm2");
                var absoluteWorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativeWorkingDirectory);
                var assetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Join("Assets", "python", "cogvlm2"));

                Directory.CreateDirectory(absoluteWorkingDirectory);

                File.Copy(Path.Combine(assetDirectory, "interrogate.py"), Path.Join(absoluteWorkingDirectory, "interrogate.py"), true);
                File.Copy(Path.Combine(assetDirectory, "requirements.txt"), Path.Join(absoluteWorkingDirectory, "requirements.txt"), true);

                await Utility.CreateVenv(absoluteWorkingDirectory, updateCallBack, consoleCallBack, "requirements.txt");

                pythonImageEngine = new PythonImageEngine("interrogate.py", "", relativeWorkingDirectory, true);
            }
            initialized = true;
        }

        public async Task<string> CaptionImage(string prompt, byte[] imageData, Action<string> consoleCallBack)
        {
            if (!initialized)
                throw new InvalidOperationException("Tried to access an uninitialized CogVLM2Interrogator.");

            if (disposed)
                throw new ObjectDisposedException("Tried to access a disposed CogVLM2Interrogator.");
            await pythonImageEngine.SendString(prompt);

            await pythonImageEngine.SendImage(imageData, consoleCallBack);
            return await pythonImageEngine.WaitForGenerationResultString(consoleCallBack);
        }

        public void Dispose()
        {
            disposed = true;
            pythonImageEngine.Dispose();
        }
    }
}

