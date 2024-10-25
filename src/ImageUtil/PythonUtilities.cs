using System.Runtime.InteropServices;

namespace ImageUtil
{
    public class PythonUtilities : IDisposable
    {
        private PythonImageEngine? pythonImageEngine;
        private bool initialized = false;
        private bool disposed = false;

        public async Task Initialize(Action<string> updateCallBack, Action<string> consoleCallBack)
        {
            if (!initialized)
            {
                string repoUrl = "https://huggingface.co/spaces/fancyfeast/joy-caption-alpha-one";
                var absoluteWorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Join("python", "utilities"));
                var assetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Join("Assets", "python", "utilities"));

                Directory.CreateDirectory(absoluteWorkingDirectory);

                File.Copy(Path.Combine(assetDirectory, "main.py"), Path.Join(absoluteWorkingDirectory, "main.py"), true);
                File.Copy(Path.Combine(assetDirectory, "requirements.txt"), Path.Join(absoluteWorkingDirectory, "requirements.txt"), true);

                await Utility.CreateVenv(absoluteWorkingDirectory, updateCallBack, consoleCallBack, "requirements.txt", null);

                pythonImageEngine = new PythonImageEngine("main.py", "", Path.Join("python", "utilities"), true);
            }
            initialized = true;
        }

        public async Task<byte[]> RunLama(byte[] imageData, byte[] maskData, Action<string> consoleCallBack)
        {
            if (!initialized)
                throw new InvalidOperationException("Tried to access an uninitialized Python Utilities Engine.");

            if (disposed)
                throw new ObjectDisposedException("Tried to access a disposed Python Utilities Engine.");

            await pythonImageEngine.SendString("lama");
            await pythonImageEngine.SendImage(imageData, consoleCallBack);
            await pythonImageEngine.SendImage(maskData, consoleCallBack);
            var res = await pythonImageEngine.WaitForGenerationResultImage(consoleCallBack);
            return Convert.FromBase64String(res);
        }

        public async Task<byte[]> GenerateYoloMask(string modelPath, byte[] imageData, Action<string> consoleCallBack)
        {
            if (!initialized)
                throw new InvalidOperationException("Tried to access an uninitialized Python Utilities Engine.");

            if (disposed)
                throw new ObjectDisposedException("Tried to access a disposed Python Utilities Engine.");

            await pythonImageEngine.SendString("yolo");
            await pythonImageEngine.SendString(modelPath);
            await pythonImageEngine.SendImage(imageData, consoleCallBack);
            var res = await pythonImageEngine.WaitForGenerationResultImage(consoleCallBack);
            return Convert.FromBase64String(res);
        }

        public void Dispose()
        {
            if (!disposed)
                pythonImageEngine?.Dispose();
            disposed = true;
        }

    }
}
