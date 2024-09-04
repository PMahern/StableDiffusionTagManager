using LibGit2Sharp;
using Newtonsoft.Json;
using SdWebUiApi;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ImageUtil
{
    static class Utility
    {
        /// <summary>
        /// Finds and replaces occurrences of a string in a file.
        /// </summary>
        /// <param name="filePath">Path to the file to process.</param>
        /// <param name="searchString">The string to search for.</param>
        /// <param name="replaceString">The string to replace with.</param>
        /// <returns>True if replacements were made, otherwise false.</returns>
        public static bool FindAndReplace(string filePath, string searchString, string replaceString)
        {
            // Temporary file path to write the updated content
            string tempFilePath = filePath + ".tmp";
            bool replacementMade = false;

            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                using (StreamWriter writer = new StreamWriter(tempFilePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Check if the line contains the search string
                        if (line.Contains(searchString))
                        {
                            replacementMade = true;
                        }

                        // Replace occurrences of the search string with the replace string
                        string updatedLine = line.Replace(searchString, replaceString);
                        writer.WriteLine(updatedLine);
                    }
                }

                // Replace the original file with the updated file
                File.Delete(filePath);
                File.Move(tempFilePath, filePath);

                return replacementMade;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while processing the file: {ex.Message}");
                return false;
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
                await DownloadIfLFSLink(filePath, lfsurl);
            }

            foreach (string subFolderPath in Directory.GetDirectories(folderPath).Where(path => !path.Contains(".git")))
            {
                await ProcessLFSFilesInFolder(subFolderPath, lfsurl);
            }
        }

        public async static Task<bool> CreateVenv(string absoluteWorkingDirectory, Action<string> updateCallBack, string? requirementsFile = null, IEnumerable<string>? additionalDependencies = null)
        {
            string venvPathPath = Path.Join(absoluteWorkingDirectory, "venv");

            if (!Directory.Exists(venvPathPath))
            {
                updateCallBack("Creating virtual environment and downloading model dependencies, this can take a while...");

                Process p = new Process();
                ProcessStartInfo info = new ProcessStartInfo();


                info.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash"; ;
                var arguments = new StringBuilder();

                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    arguments.Append("/k \"python -m venv venv && venv\\Scripts\\activate.bat");
                }
                else
                {
                    arguments.Append("-c \"python -m venv venv && source venv/bin/activate");
                }

                if(requirementsFile != null)
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
                

                //var installSubstr = requirementsFile != $"pip install{(requirementsFile != null ? " -r requirements.txt " : "")}";
                //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                //{
                //    info.FileName = "cmd.exe";
                //    info.Arguments = $"/k \"python -m venv venv && venv\\Scripts\\activate.bat && {installSubstr} && pip install Pillow && pip install spaces && pip install protobuf && pip install bitsandbytes && exit\"";
                //}
                //else
                //{
                //    info.FileName = "/bin/bash";
                //    info.Arguments = $"-c \"python -m venv venv && source venv/bin/activate && {installSubstr} && pip install Pillow && pip install spaces && pip install protobuf && pip install bitsandbytes && exit\"";
                //}

                info.RedirectStandardInput = true;
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                info.WorkingDirectory = absoluteWorkingDirectory;

                p.StartInfo = info;
                p.Start();

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
