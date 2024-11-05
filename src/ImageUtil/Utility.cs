using LibGit2Sharp;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace ImageUtil
{
    public static class Utility
    {
        public static string PythonPath = "python";

        /// <summary>
        /// Finds and replaces occurrences of a string in a file.
        /// </summary>
        /// <param name="filePath">Path to the file to process.</param>
        /// <param name="searchString">The string to search for.</param>
        /// <param name="replaceString">The string to replace with.</param>
        /// <returns>True if replacements were made, otherwise false.</returns>
        public static void FindAndReplace(string filePath, string searchString, string replaceString)
        {
            string? fileContents = null;
            //Using explicit streams, was seeing some IOExceptions with ReadAllText/WriteAllText
            using (var sr = new StreamReader(filePath))
            {
                fileContents = sr.ReadToEnd();
            }
            if (fileContents == null)
                throw new Exception("Could not find file at path: " + filePath);

            if (fileContents != null)
            {
                var results = fileContents.Replace(searchString, replaceString);
                using (var sw = new StreamWriter(filePath, false))
                {
                    sw.Write(results);
                }
            }

        }

        /// <summary>
        /// Clone the repot at the given url into the given directory. 
        /// </summary>
        /// <param name="repoUrl"></param>
        /// <param name="absoluteWorkingDirectory"></param>
        /// <param name="updateCallBack"></param>
        /// <returns>True if the repo was clone, false if it already existed.</returns>
        public async static Task<bool> CloneRepo(string repoUrl, string absoluteWorkingDirectory, Action<string> updateCallBack)
        {
            if (!Directory.Exists(absoluteWorkingDirectory))
            {
                updateCallBack("Cloning repository...");

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

                return true;
            }

            return false;
        }

        public static async Task ProcessLFSFilesInFolder(string folderPath, string lfsurl)
        {
            foreach (string filePath in Directory.GetFiles(folderPath))
            {
                try
                {
                    await DownloadIfLFSLink(filePath, lfsurl);
                }
                catch (Exception e)
                {
                }

            }

            foreach (string subFolderPath in Directory.GetDirectories(folderPath).Where(path => !path.Contains(".git")))
            {
                try
                {
                    await ProcessLFSFilesInFolder(subFolderPath, lfsurl);
                }
                catch (Exception e)
                {
                }
            }
        }
        public static async Task CheckPythonVersionAsync()
        {
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "python";
                    process.StartInfo.Arguments = "--version";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string errorOutput = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    // Check which output contains the version information
                    string versionOutput = !string.IsNullOrWhiteSpace(output) ? output : errorOutput;

                    if (!string.IsNullOrWhiteSpace(versionOutput))
                    {
                        // Extract the first two version numbers using regex
                        var match = Regex.Match(versionOutput, @"\b(\d+\.\d+)");
                        if (match.Success)
                        {
                            if (match.Value != "3.11")
                            {
                                throw new Exception("Python version is not 3.11. Please install Python 3.11.");
                            }
                        }
                        else
                        {
                            throw new Exception("Could not parse the Python version.");
                        }
                    }
                    else
                    {
                        throw new Exception("Python is not installed or could not be detected.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while checking the Python version: " + ex.Message);
            }
        }


        public async static Task<bool> CreateVenv(string absoluteWorkingDirectory, Action<string> updateCallBack, Action<string> consoleCallback, string? requirementsFile = null, IEnumerable<string>? additionalDependencies = null)
        {
            string venvPathPath = Path.Join(absoluteWorkingDirectory, "venv");

            await CheckPythonVersionAsync();

            if (!Directory.Exists(venvPathPath))
            {
                updateCallBack("Creating virtual environment and downloading model dependencies...");

                Process p = new Process();
                ProcessStartInfo info = new ProcessStartInfo();

                info.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash"; ;
                var arguments = new StringBuilder();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    arguments.Append($"/k \"{PythonPath} -m venv venv && venv\\Scripts\\activate.bat");
                }
                else
                {
                    arguments.Append($"-c \"{PythonPath} -m venv venv && source venv/bin/activate");
                }

                if (requirementsFile != null)
                {
                    arguments.Append($" && pip install -r {requirementsFile}");
                }

                if (additionalDependencies != null)
                {
                    foreach (var dependency in additionalDependencies)
                    {
                        arguments.Append($" && pip install {dependency}");
                    }
                }

                arguments.Append(" && exit\"");
                info.Arguments = arguments.ToString();

                info.RedirectStandardOutput = true;
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                info.WorkingDirectory = absoluteWorkingDirectory;

                p.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null && !string.IsNullOrWhiteSpace(args.Data))
                        consoleCallback(args.Data);
                };

                p.StartInfo = info;
                p.Start();

                p.BeginOutputReadLine();

                await p.WaitForExitAsync();

                return true;
            }

            return false;
        }

        public static async Task DownloadIfLFSLink(string filePath, string lfsurl)
        {
            // Read the first line of the file
            var lines = File.ReadLines(filePath).GetEnumerator();
            lines.MoveNext();
            var firstLine = lines.Current;

            // Check if the first line matches the LFS link pattern
            if (firstLine != null && firstLine.StartsWith("version https://git-lfs.github.com/spec/v1"))
            {
                lines.MoveNext();
                string[] lineParts = lines.Current.Split(' ');

                // Extract the hash algorithm and ID from the first line
                string hashAlgo = lineParts[1].Split(':')[0];
                string id = lineParts[1].Split(':')[1];

                lines.MoveNext();
                lineParts = lines.Current.Split(' ');
                long size = long.Parse(lineParts[1]);

                lines.Dispose();

                // Create the JSON payload
                var payload = new
                {
                    operation = "download",
                    transfers = new[] { "basic" },
                    objects = new[]
                    {
                        new
                        {
                            oid = id,
                            size = size
                        }
                    },
                    hash_algo = hashAlgo
                };

                // Convert the payload to JSON string
                string jsonPayload = JsonConvert.SerializeObject(payload);

                // Send the JSON payload to the LFS server
                using (var httpClient = new HttpClient())
                {
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/vnd.git-lfs+json");
                    var response = await httpClient.PostAsync(lfsurl, content);

                    // Read the JSON response
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    // Process the JSON response as needed
                    var responseObject = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                    string downloadUrl = responseObject.objects[0].actions.download.href;

                    // Download the data from the URL and write it directly to the file
                    using (var webClient = new HttpClient())
                    {
                        using (var stream = await webClient.GetStreamAsync(downloadUrl))
                        {
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await stream.CopyToAsync(fileStream);
                            }
                        }
                    }
                }
            }
        }
    }
}
