using Newtonsoft.Json;

namespace ImageUtil
{
    internal class SWTagger : ITagInterrogator
    {
        private readonly string model;

        public SWTagger(string model)
        {
            this.model = model;
        }

        public async Task Initialize(Action<string> updateCallBack)
        {
            string repoUrl = "https://huggingface.co/spaces/SmilingWolf/wd-tagger";
            var absoluteWorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Join("taggers", "sw"));

            if (await Utility.CloneRepo(repoUrl, absoluteWorkingDirectory, updateCallBack))
            { 
                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "python", "sw", "interrogate.py"), Path.Join(absoluteWorkingDirectory, "interrogate.py"), true);
            }

            var additionalPackages = new List<string> { "gradio" };
            await Utility.CreateVenv(absoluteWorkingDirectory, updateCallBack, "requirements.txt", additionalPackages);
        }

        public async Task<List<string>> TagImage(byte[] imageData, float threshold)
        {
            var output = await PythonWrapper.RunPythonScript("interrogate.py", $"{model} {threshold.ToString()}", imageData, Path.Join("taggers", "sw"), true);
            const string targetPhrase = "GENERATION RESULT";

            // Find the position of the target phrase in the input text
            int index = output.IndexOf(targetPhrase, StringComparison.OrdinalIgnoreCase);

            if (index == -1)
            {
                // Target phrase not found
                return new List<string>();
            }

            // Calculate the start position of the text following the target phrase
            int startIndex = index + targetPhrase.Length;

            // Extract and return the text following the target phrase
            // We use Trim to remove any leading white spaces
            var result = output.Substring(startIndex).Trim();
            result = result.Trim('(', ')', '\'');
            var parts = result.Split(new[] { "', {" }, StringSplitOptions.None);
            return parts[0].Split(", ").ToList();
        }        
    }
}
