using LibGit2Sharp;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ImageUtil
{
    public class JoyCaptioner
    {
        public JoyCaptioner() { }

        public async Task Initialize(Action<string> updateCallBack)
        {
            string repoUrl = "https://huggingface.co/spaces/fancyfeast/joy-caption-pre-alpha";
            var absoluteWorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Join("taggers", "joycaption"));

            if (!Directory.Exists(absoluteWorkingDirectory))
            {
                updateCallBack("Cloning joy-caption repository...");

                await Task.Run(() =>
                {
                    Repository.Clone(repoUrl, absoluteWorkingDirectory, new CloneOptions
                    {
                        Checkout = true,
                    });
                });

                updateCallBack("Downloading large files...");

                string lfsurl = $"{repoUrl}.git/info/lfs/objects/batch";

                await Utility.ProcessLFSFilesInFolder(absoluteWorkingDirectory, lfsurl);

                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "python", "joycaption", "interrogate.py"), Path.Join(absoluteWorkingDirectory, "interrogate.py"), true);

                //Replace the use of the meta hosted llama model to one that is publically available
                Utility.FindAndReplace(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "taggers", "joycaption", "app.py"), "meta-llama/Meta-Llama-3.1-8B", "unsloth/Meta-Llama-3.1-8B-bnb-4bit");
            }

            string venvPathPath = Path.Join(absoluteWorkingDirectory, "venv");

            if (!Directory.Exists(venvPathPath))
            {
                updateCallBack("Creating virtual environment and downloading model dependencies, this can take a while...");

                Process p = new Process();
                ProcessStartInfo info = new ProcessStartInfo();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    info.FileName = "cmd.exe";
                    info.Arguments = "/k \"python -m venv venv && venv\\Scripts\\activate.bat && pip install -r requirements.txt && pip install Pillow && pip install spaces && pip install protobuf && pip install bitsandbytes && exit\"";
                }
                else {
                    info.FileName = "/bin/bash";
                    info.Arguments = "-c \"python -m venv venv && source venv/bin/activate && pip install -r requirements.txt && pip install Pillow && pip install spaces && pip install protobuf && pip install bitsandbytes && exit\"";
                }
                
                info.RedirectStandardInput = true;
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                info.WorkingDirectory = absoluteWorkingDirectory;

                p.StartInfo = info;
                p.Start();

                await p.WaitForExitAsync();
            }
        }

        public async Task<string> CaptionImage(byte[] imageData) {
            var output = await PythonWrapper.RunPythonScript(Path.Join("interrogate.py"), "", imageData, Path.Join("taggers", "joycaption"), true);
            const string targetPhrase = "GENERATION RESULT";

            // Find the position of the target phrase in the input text
            int index = output.IndexOf(targetPhrase, StringComparison.OrdinalIgnoreCase);

            if (index == -1)
            {
                // Target phrase not found
                return string.Empty;
            }

            // Calculate the start position of the text following the target phrase
            int startIndex = index + targetPhrase.Length;

            // Extract and return the text following the target phrase
            // We use Trim to remove any leading white spaces
            return output.Substring(startIndex).Trim();
        }
    }
}
