namespace ImageUtil
{
    public class JoyCaptioner : INaturalLanguageInterrogator
    {
        public async Task Initialize(Action<string> updateCallBack)
        {
            string repoUrl = "https://huggingface.co/spaces/fancyfeast/joy-caption-pre-alpha";
            var absoluteWorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Join("taggers", "joycaption"));

            if (await Utility.CloneRepo(repoUrl, absoluteWorkingDirectory, updateCallBack))
            {
                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "python", "joycaption", "interrogate.py"), Path.Join(absoluteWorkingDirectory, "interrogate.py"), true);

                //Replace the use of the meta hosted llama model to one that is publically available
                Utility.FindAndReplace(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "taggers", "joycaption", "app.py"), "meta-llama/Meta-Llama-3.1-8B", "unsloth/Meta-Llama-3.1-8B-bnb-4bit");
            }

            var additionalPackages = new List<string> { "Pillow", "spaces", "protobuf", "bitsandbytes" };
            await Utility.CreateVenv(absoluteWorkingDirectory, updateCallBack, "requirements.txt", additionalPackages);
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
