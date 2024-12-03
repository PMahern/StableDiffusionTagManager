using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.ViewModels
{
    internal class JoyCaptionAlphaTwoNaturalLanguageInterrogationViewModel : InterrogatorViewModel<string>
    {
		public static readonly List<string> ExtraOptions = new List<string> 
		{
			"If there is a person/character in the image you must refer to them as {name}.",
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
			"ONLY describe the most important elements of the image."
		};

        public override Task<string> Interrogate(byte[] imageData, Action<string> updateCallBack, Action<string> consoleCallBack)
        {
            throw new NotImplementedException();
        }

		public override bool IsValid => true;
    }
}
