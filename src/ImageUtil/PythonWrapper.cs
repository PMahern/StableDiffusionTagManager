using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ImageUtil
{
    public static class PythonWrapper
    {
        public static async Task<string> RunPythonScript(string scriptPath, string arguments, byte[] data, string workingdirectory, bool isvenv = false)
        {
            var sb = new StringBuilder();

            using (Process p = new Process())
            {
                ProcessStartInfo info = new ProcessStartInfo();

                var pythonExe = "python";
                var absoluteWorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, workingdirectory);
                if (isvenv)
                {
                    pythonExe = Path.Join(absoluteWorkingDirectory, (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                                    Path.Join("venv", "Scripts", "python.exe") :
                                    Path.Join("venv", "bin", "python")));
                }

                info.FileName = pythonExe;
                info.Arguments = $"{scriptPath} {arguments}";
                info.RedirectStandardInput = true;
                info.UseShellExecute = false;
                info.CreateNoWindow = true;

                info.WorkingDirectory = absoluteWorkingDirectory;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;

                p.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null && !string.IsNullOrWhiteSpace(args.Data))
                        sb.AppendLine(args.Data);
                };

                p.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null && !string.IsNullOrWhiteSpace(args.Data))
                        sb.AppendLine(args.Data);
                };

                p.StartInfo = info; 
                p.Start();

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                if (data != null)
                {
                    using (Stream stdin = p.StandardInput.BaseStream)
                    {
                        stdin.Write(data, 0, data.Length);
                        stdin.Flush();
                        stdin.Close();
                    }
                }

                await p.WaitForExitAsync();

                if (p.ExitCode != 0)
                {
                    throw new Exception($"Python script failed with exit code {p.ExitCode}. Full output is {sb.ToString()}");
                }
            }

            return sb.ToString();
        }
    }
}
