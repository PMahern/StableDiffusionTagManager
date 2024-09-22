# This file is a modified version of the original file from the JoyCaption project: https://huggingface.co/spaces/fancyfeast/joy-caption-pre-alpha/blob/main/app.py.
from PIL import Image
import sys
from io import BytesIO
import base64
import spaces
from huggingface_hub import InferenceClient
from torch import nn
from transformers import AutoModel, AutoProcessor, AutoTokenizer, PreTrainedTokenizer, PreTrainedTokenizerFast, AutoModelForCausalLM
from pathlib import Path
import torch
import torch.amp.autocast_mode
import os
import torchvision.transforms.functional as TVF

CLIP_PATH = "google/siglip-so400m-patch14-384"
MODEL_PATH = "unsloth/Meta-Llama-3.1-8B-bnb-4bit"
CHECKPOINT_PATH = Path("9em124t2-499968")
TITLE = "<h1><center>JoyCaption Alpha One (2024-09-20a)</center></h1>"
CAPTION_TYPE_MAP = {
	("descriptive", "formal", False, False): ["Write a descriptive caption for this image in a formal tone."],
	("descriptive", "formal", False, True): ["Write a descriptive caption for this image in a formal tone within {word_count} words."],
	("descriptive", "formal", True, False): ["Write a {length} descriptive caption for this image in a formal tone."],
	("descriptive", "informal", False, False): ["Write a descriptive caption for this image in a casual tone."],
	("descriptive", "informal", False, True): ["Write a descriptive caption for this image in a casual tone within {word_count} words."],
	("descriptive", "informal", True, False): ["Write a {length} descriptive caption for this image in a casual tone."],

	("training_prompt", "formal", False, False): ["Write a stable diffusion prompt for this image."],
	("training_prompt", "formal", False, True): ["Write a stable diffusion prompt for this image within {word_count} words."],
	("training_prompt", "formal", True, False): ["Write a {length} stable diffusion prompt for this image."],

	("rng-tags", "formal", False, False): ["Write a list of Booru tags for this image."],
	("rng-tags", "formal", False, True): ["Write a list of Booru tags for this image within {word_count} words."],
	("rng-tags", "formal", True, False): ["Write a {length} list of Booru tags for this image."],
}

HF_TOKEN = os.environ.get("HF_TOKEN", None)

class ImageAdapter(nn.Module):
	def __init__(self, input_features: int, output_features: int, ln1: bool, pos_emb: bool, num_image_tokens: int, deep_extract: bool):
		super().__init__()
		self.deep_extract = deep_extract

		if self.deep_extract:
			input_features = input_features * 5

		self.linear1 = nn.Linear(input_features, output_features)
		self.activation = nn.GELU()
		self.linear2 = nn.Linear(output_features, output_features)
		self.ln1 = nn.Identity() if not ln1 else nn.LayerNorm(input_features)
		self.pos_emb = None if not pos_emb else nn.Parameter(torch.zeros(num_image_tokens, input_features))

		# Mode token
		#self.mode_token = nn.Embedding(n_modes, output_features)
		#self.mode_token.weight.data.normal_(mean=0.0, std=0.02)   # Matches HF's implementation of llama3

		# Other tokens (<|image_start|>, <|image_end|>, <|eot_id|>)
		self.other_tokens = nn.Embedding(3, output_features)
		self.other_tokens.weight.data.normal_(mean=0.0, std=0.02)   # Matches HF's implementation of llama3

	def forward(self, vision_outputs: torch.Tensor):
		if self.deep_extract:
			x = torch.concat((
				vision_outputs[-2],
				vision_outputs[3],
				vision_outputs[7],
				vision_outputs[13],
				vision_outputs[20],
			), dim=-1)
			assert len(x.shape) == 3, f"Expected 3, got {len(x.shape)}"  # batch, tokens, features
			assert x.shape[-1] == vision_outputs[-2].shape[-1] * 5, f"Expected {vision_outputs[-2].shape[-1] * 5}, got {x.shape[-1]}"
		else:
			x = vision_outputs[-2]

		x = self.ln1(x)

		if self.pos_emb is not None:
			assert x.shape[-2:] == self.pos_emb.shape, f"Expected {self.pos_emb.shape}, got {x.shape[-2:]}"
			x = x + self.pos_emb

		x = self.linear1(x)
		x = self.activation(x)
		x = self.linear2(x)

		# Mode token
		#mode_token = self.mode_token(mode)
		#assert mode_token.shape == (x.shape[0], mode_token.shape[1], x.shape[2]), f"Expected {(x.shape[0], 1, x.shape[2])}, got {mode_token.shape}"
		#x = torch.cat((x, mode_token), dim=1)

		# <|image_start|>, IMAGE, <|image_end|>
		other_tokens = self.other_tokens(torch.tensor([0, 1], device=self.other_tokens.weight.device).expand(x.shape[0], -1))
		assert other_tokens.shape == (x.shape[0], 2, x.shape[2]), f"Expected {(x.shape[0], 2, x.shape[2])}, got {other_tokens.shape}"
		x = torch.cat((other_tokens[:, 0:1], x, other_tokens[:, 1:2]), dim=1)

		return x

	def get_eot_embedding(self):
		return self.other_tokens(torch.tensor([2], device=self.other_tokens.weight.device)).squeeze(0)



# Load CLIP
print("Loading CLIP")
clip_processor = AutoProcessor.from_pretrained(CLIP_PATH, token=HF_TOKEN)
clip_model = AutoModel.from_pretrained(CLIP_PATH, token=HF_TOKEN)
clip_model = clip_model.vision_model

if (CHECKPOINT_PATH / "clip_model.pt").exists():
	print("Loading VLM's custom vision model")
	checkpoint = torch.load(CHECKPOINT_PATH / "clip_model.pt", map_location='cpu')
	checkpoint = {k.replace("_orig_mod.module.", ""): v for k, v in checkpoint.items()}
	clip_model.load_state_dict(checkpoint)
	del checkpoint

clip_model.eval()
clip_model.requires_grad_(False)
clip_model.to("cuda")


# Tokenizer
print("Loading tokenizer")
tokenizer = AutoTokenizer.from_pretrained(MODEL_PATH, use_fast=False, token=HF_TOKEN)
assert isinstance(tokenizer, PreTrainedTokenizer) or isinstance(tokenizer, PreTrainedTokenizerFast), f"Tokenizer is of type {type(tokenizer)}"

# LLM
print("Loading LLM")
if (CHECKPOINT_PATH / "text_model").exists:
	print("Loading VLM's custom text model")
	text_model = AutoModelForCausalLM.from_pretrained(CHECKPOINT_PATH / "text_model", device_map=0, torch_dtype=torch.bfloat16, token=HF_TOKEN)
else:
	text_model = AutoModelForCausalLM.from_pretrained(MODEL_PATH, device_map="auto", torch_dtype=torch.bfloat1, token=HF_TOKEN)

text_model.eval()

# Image Adapter
print("Loading image adapter")
image_adapter = ImageAdapter(clip_model.config.hidden_size, text_model.config.hidden_size, False, False, 38, False)
image_adapter.load_state_dict(torch.load(CHECKPOINT_PATH / "image_adapter.pt", map_location="cpu"))
image_adapter.eval()
image_adapter.to("cuda")


@spaces.GPU()
@torch.no_grad()
def stream_chat(input_image: Image.Image, caption_type: str, caption_tone: str, caption_length: str | int) -> str:
	torch.cuda.empty_cache()

	# 'any' means no length specified
	length = None if caption_length == "any" else caption_length

	if isinstance(length, str):
		try:
			length = int(length)
		except ValueError:
			pass

	# 'rng-tags' and 'training_prompt' don't have formal/informal tones
	if caption_type == "rng-tags" or caption_type == "training_prompt":
		caption_tone = "formal"

	# Build prompt
	prompt_key = (caption_type, caption_tone, isinstance(length, str), isinstance(length, int))
	if prompt_key not in CAPTION_TYPE_MAP:
		raise ValueError(f"Invalid caption type: {prompt_key}")

	prompt_str = CAPTION_TYPE_MAP[prompt_key][0].format(length=length, word_count=length)
	print(f"Prompt: {prompt_str}")

	# Preprocess image
	#image = clip_processor(images=input_image, return_tensors='pt').pixel_values
	image = input_image.resize((384, 384), Image.LANCZOS)
	pixel_values = TVF.pil_to_tensor(image).unsqueeze(0) / 255.0
	pixel_values = TVF.normalize(pixel_values, [0.5], [0.5])
	pixel_values = pixel_values.to('cuda')

	# Tokenize the prompt
	prompt = tokenizer.encode(prompt_str, return_tensors='pt', padding=False, truncation=False, add_special_tokens=False)

	# Embed image
	with torch.amp.autocast_mode.autocast('cuda', enabled=True):
		vision_outputs = clip_model(pixel_values=pixel_values, output_hidden_states=True)
		image_features = vision_outputs.hidden_states
		embedded_images = image_adapter(image_features)
		embedded_images = embedded_images.to('cuda')
	
	# Embed prompt
	prompt_embeds = text_model.model.embed_tokens(prompt.to('cuda'))
	assert prompt_embeds.shape == (1, prompt.shape[1], text_model.config.hidden_size), f"Prompt shape is {prompt_embeds.shape}, expected {(1, prompt.shape[1], text_model.config.hidden_size)}"
	embedded_bos = text_model.model.embed_tokens(torch.tensor([[tokenizer.bos_token_id]], device=text_model.device, dtype=torch.int64))
	eot_embed = image_adapter.get_eot_embedding().unsqueeze(0).to(dtype=text_model.dtype)

	# Construct prompts
	inputs_embeds = torch.cat([
		embedded_bos.expand(embedded_images.shape[0], -1, -1),
		embedded_images.to(dtype=embedded_bos.dtype),
		prompt_embeds.expand(embedded_images.shape[0], -1, -1),
		eot_embed.expand(embedded_images.shape[0], -1, -1),
	], dim=1)

	input_ids = torch.cat([
		torch.tensor([[tokenizer.bos_token_id]], dtype=torch.long),
		torch.zeros((1, embedded_images.shape[1]), dtype=torch.long),
		prompt,
		torch.tensor([[tokenizer.convert_tokens_to_ids("<|eot_id|>")]], dtype=torch.long),
	], dim=1).to('cuda')
	attention_mask = torch.ones_like(input_ids)

	#generate_ids = text_model.generate(input_ids, inputs_embeds=inputs_embeds, attention_mask=attention_mask, max_new_tokens=300, do_sample=False, suppress_tokens=None)
	#generate_ids = text_model.generate(input_ids, inputs_embeds=inputs_embeds, attention_mask=attention_mask, max_new_tokens=300, do_sample=True, top_k=10, temperature=0.5, suppress_tokens=None)
	generate_ids = text_model.generate(input_ids, inputs_embeds=inputs_embeds, attention_mask=attention_mask, max_new_tokens=300, do_sample=True, suppress_tokens=None)   # Uses the default which is temp=0.6, top_p=0.9

	# Trim off the prompt
	generate_ids = generate_ids[:, input_ids.shape[1]:]
	if generate_ids[0][-1] == tokenizer.eos_token_id or generate_ids[0][-1] == tokenizer.convert_tokens_to_ids("<|eot_id|>"):
		generate_ids = generate_ids[:, :-1]

	caption = tokenizer.batch_decode(generate_ids, skip_special_tokens=False, clean_up_tokenization_spaces=False)[0]

	return caption.strip()

def main():
    while True:
        promptdata = sys.stdin.readline().strip()
        imagedata = sys.stdin.readline().strip()

        binary_data = base64.b64decode(imagedata)
        image_bytes = BytesIO(binary_data)

        with Image.open(image_bytes).convert("RGB") as img:
            captions = stream_chat(img, promptdata, "formal", "any")
            print("GENERATION START")
            print(captions)
            print("GENERATION END")
            sys.stdout.flush()

if __name__ == "__main__":
    main()