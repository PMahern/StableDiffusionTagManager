using LibGit2Sharp;
using System.Linq;
using System.Runtime.InteropServices;

namespace ImageUtil.Interrogation
{
    public class JoyCaptionBetaOneArgs
    {
        public string CaptionType { get; set; }
        public string Length { get; set; }
        public string ExtraOptions { get; set; }
        public string NameInput { get; set; }
        public string CustomPrompt { get; set; } = "";
    }

    public class JoyCaptionBetaOne : INaturalLanguageInterrogator<JoyCaptionBetaOneArgs>, ITagInterrogator<JoyCaptionBetaOneArgs>
    {
        public static readonly List<string> ExtraOptionText = new List<string>
        {
            "Do NOT include information about people/characters that cannot be changed (like ethnicity, gender, etc), but do still include changeable attributes (like hair style).",
            "Include information about lighting.",
            "Include information about camera angle.",
            "Include information about whether there is a watermark or not.",
            "Include information about whether there are JPEG artifacts or not.",
            "If it is a photo you MUST include information about what camera was likely used and details such as aperture, shutter speed, ISO, etc.",
            "Do NOT include anything sexual; keep it PG.",
            "Do NOT mention the image's resolution.",
            "You MUST include information about the subjective aesthetic quality of the image from low to very high.",
            "Include information on the image's composition style, such as leading lines, rule of thirds, or symmetry.",
            "Do NOT mention any text that is in the image.",
            "Specify the depth of field and whether the background is in focus or blurred.",
            "If applicable, mention the likely use of artificial or natural lighting sources.",
            "Do NOT use any ambiguous language.",
            "Include whether the image is sfw, suggestive, or nsfw.",
            "ONLY describe the most important elements of the image.",
            "If it is a work of art, do not include the artist's name or the title of the work.",
            "Identify the image orientation (portrait, landscape, or square) and aspect ratio if obvious.",
            "Use vulgar slang and profanity, such as (but not limited to) \"fucking,\" \"slut,\" \"cock,\" etc.",
            "Do NOT use polite euphemisms—lean into blunt, casual phrasing.",
            "Include information about the ages of any people/characters when applicable.",
            "Mention whether the image depicts an extreme close-up, close-up, medium close-up, medium shot, cowboy shot, medium wide shot, wide shot, or extreme wide shot.",
            "Do not mention the mood/feeling/etc of the image.",
            "Explicitly specify the vantage height (eye-level, low-angle worm’s-eye, bird’s-eye, drone, rooftop, etc.).",
            "If there is a watermark, you must mention it.",
            "Your response will be used by a text-to-image model, so avoid useless meta phrases like \"This image shows…\", \"You are looking at...\", etc."
        };

        public static readonly List<string> TaggingPrompts = new List<string>
        {
            "Danbooru tag list",
            "e621 tag list",
            "Rule34 tag list",
            "Booru-like tag list"
        };

        public static readonly List<string> NaturalLanguagePrompts = new List<string>
        {
            "Descriptive",
            "Descriptive (Casual)",
            "Straightforward",
            "Training Prompt",
            "MidJourney",
            "Art Critic",
            "Product Listing",
            "Social Media Post"
        };

        public static readonly List<string> LengthChoices = new List<string>
        {
            "any", "very short", "short", "medium-length", "long", "very long"
        }.Concat(Enumerable.Range(2, 25).Select(x => (x * 10).ToString())).ToList();

        private readonly string model;
        private PythonImageEngine pythonImageEngine;
        private bool initialized = false;
        private bool disposed = false;

        public async Task Initialize(Action<string> updateCallBack, Action<string> consoleCallBack)
        {
            if (!initialized)
            {
                string repoUrl = "https://huggingface.co/spaces/fancyfeast/joy-caption-beta-one";
                var absoluteWorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Join("taggers", "joycaptionbetaone"));

                await Utility.CloneRepo(repoUrl, absoluteWorkingDirectory, updateCallBack);

                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "python", "joycaptionbetaone", "interrogate.py"), Path.Join(absoluteWorkingDirectory, "interrogate.py"), true);

                var additionalPackages = new List<string> { "Pillow", "spaces", "protobuf", "tqdm", };
                additionalPackages.Add("torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu128");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    additionalPackages.AddRange(new List<string> () { "bitsandbytes", "triton-windows", "liger-kernel==0.5.9 --no-deps" });
                }

                await Utility.CreateVenv(absoluteWorkingDirectory, updateCallBack, consoleCallBack, "requirements.txt", additionalPackages);


                // Search and replace in .yaml files
                var yamljsonFiles = Directory.GetFiles(absoluteWorkingDirectory, "*.yaml", SearchOption.AllDirectories)
                                            .Concat(Directory.GetFiles(absoluteWorkingDirectory, "*.json", SearchOption.AllDirectories)).ToArray();

                pythonImageEngine = new PythonImageEngine("interrogate.py", "", Path.Join("taggers", "joycaptionbetaone"), true);
            }
            initialized = true;
        }

        public async Task<string> CaptionImage(JoyCaptionBetaOneArgs args, byte[] imageData, Action<string> consoleCallBack)
        {
            if (!initialized)
                throw new InvalidOperationException("Tried to access an uninitialized Joy Captioner Beta One.");

            if (disposed)
                throw new ObjectDisposedException("Tried to access a disposed Joy Captioner Beta One.");

            await pythonImageEngine.SendString(args.CaptionType);
            await pythonImageEngine.SendString(args.Length);
            await pythonImageEngine.SendString(args.ExtraOptions);
            await pythonImageEngine.SendString(args.NameInput);
            await pythonImageEngine.SendString(args.CustomPrompt);

            await pythonImageEngine.SendImage(imageData, consoleCallBack);
            return await pythonImageEngine.WaitForGenerationResultString(consoleCallBack);
        }

        public async Task<List<string>> TagImage(JoyCaptionBetaOneArgs args, byte[] imageData, Action<string> consoleCallBack)
        {
            await pythonImageEngine.SendString(args.CaptionType);
            await pythonImageEngine.SendString(args.Length);
            await pythonImageEngine.SendString(args.ExtraOptions);
            await pythonImageEngine.SendString(args.NameInput);
            await pythonImageEngine.SendString(args.CustomPrompt);

            await pythonImageEngine.SendImage(imageData, consoleCallBack);

            var results = await pythonImageEngine.WaitForGenerationResultString(consoleCallBack);

            //It's returning some tags with capital letters. Tags are always lowercase.
            if (TaggingPrompts.Contains(args.CaptionType))
            {
                results = results.ToLower();
            }
            return results.Split(", ").ToList();
        }

        public void Dispose()
        {
            disposed = true;
            pythonImageEngine?.Dispose();
            pythonImageEngine = null;
        }

        public bool Disposed => disposed;
    }
}
