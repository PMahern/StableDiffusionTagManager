using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ImageUtil
{
    public class PythonImageEngine : IDisposable
    {
        private Process pythonProcess;
        private bool disposed = false;
        private StringBuilder sb = new StringBuilder();
        private bool insideResult = false;

        public PythonImageEngine(string scriptPath, string arguments, string workingdirectory, bool isvenv = false)
        {
            ProcessStartInfo info = new ProcessStartInfo();

            var pythonExe = "python";
            var absoluteWorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, workingdirectory);
            if (isvenv)
            {
                pythonExe = Path.Join(absoluteWorkingDirectory, "venv", "bin", "python");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var pythonExePath = Path.Join(absoluteWorkingDirectory, "venv", "Scripts");
                    pythonExe = Path.Join(pythonExePath, "python.exe");
                    if(!File.Exists(pythonExe))
                        pythonExe = Path.Join(pythonExePath, "python311.exe");
                }
            }

            info.FileName = pythonExe;
            info.Arguments = $"{scriptPath} {arguments}";
            info.RedirectStandardInput = true;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;

            info.WorkingDirectory = absoluteWorkingDirectory;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;

            pythonProcess = new Process();
            pythonProcess.StartInfo = info;

            pythonProcess.Start();
            
        }

        public async Task SendString(string data)
        {
            if (!this.disposed)
            {
                try
                {
                    await pythonProcess.StandardInput.WriteLineAsync(data); // Send data to the Python script
                    await pythonProcess.StandardInput.FlushAsync();
                }
                catch
                {
                    throw new Exception($"Failed to submit image to python. Error output was {pythonProcess.StandardError.ReadToEnd()}");
                }
            }
            else
            {
                throw new ObjectDisposedException("PythonImageEngine");
            }
        }

        public async Task SendImage(byte[] imageData, Action<string>? consoleOut = null)
        {
            if (!this.disposed)
            {
                try
                {
                    string base64Data = Convert.ToBase64String(imageData); // Convert to base64 for easier transfer
                    await pythonProcess.StandardInput.WriteLineAsync(base64Data); // Send data to the Python script
                    await pythonProcess.StandardInput.FlushAsync();
                }
                catch
                {
                    throw new Exception($"Failed to submit image to python. Error output was {pythonProcess.StandardError.ReadToEnd()}");
                }
            }
            else
            {
                throw new ObjectDisposedException("PythonImageEngine");
            }
        }

        public async Task<string> WaitForGenerationResultString(Action<string>? consoleOut = null)
        {
            StringBuilder sb = new StringBuilder();
            string startPhrase = "GENERATION START";
            string endPhrase = "GENERATION END";

            while (true)
            {
                var output = await pythonProcess.StandardOutput.ReadLineAsync();
                if (output == null)
                {
                    throw new Exception($"Python process terminated unexpectedly. Error output was {pythonProcess.StandardError.ReadToEnd()}");
                }

                if (output.Contains(startPhrase))
                {
                    insideResult = true;
                }
                else if (output.Contains(endPhrase))
                {
                    var result =  sb.ToString();
                    sb.Clear();
                    insideResult = false;
                    return result;
                }
                else
                    {
                    if (insideResult)
                        sb.AppendLine(output);
                    else
                        consoleOut?.Invoke(output);
                }
            }
        }

        public async Task<string> WaitForGenerationResultImage(Action<string>? consoleOut = null)
        {
            string startPhrase = "GENERATION START";
            string endPhrase = "GENERATION END";
            string noResultPhrase = "NO RESULTS";
            string base64Data = "";
            while (true)
            {
                var output = await pythonProcess.StandardOutput.ReadLineAsync();
                if (output == null)
                {
                    throw new Exception($"Python process terminated unexpectedly. Error output was {pythonProcess.StandardError.ReadToEnd()}");
                }
                else if (output.Contains(noResultPhrase))
                {
                    return null;
                }
                if (output.Contains(startPhrase))
                {
                    insideResult = true;
                }
                else if (output.Contains(endPhrase))
                {
                    insideResult = false;
                    return base64Data;
                }
                else
                {
                    if (insideResult)
                        base64Data += output;
                    else
                        consoleOut?.Invoke(output);
                }
            }
        }

        public void Dispose()
        {
            this.disposed = true;
            pythonProcess.Kill();
        }
    }
}
