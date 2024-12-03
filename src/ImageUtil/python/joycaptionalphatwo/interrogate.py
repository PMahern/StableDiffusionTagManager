# This file is a modified version of the original file from the JoyCaption project: https://huggingface.co/spaces/fancyfeast/joy-caption-alpha-two/blob/main/app.py.
import spaces
import gradio as gr
from huggingface_hub import InferenceClient
from torch import nn
from transformers import AutoModel, AutoProcessor, AutoTokenizer, PreTrainedTokenizer, PreTrainedTokenizerFast, AutoModelForCausalLM
from pathlib import Path
import torch
import torch.amp.autocast_mode
from PIL import Image
import os
import torchvision.transforms.functional as TVF
import sys
import base64
from io import BytesIO

CLIP_PATH = "google/siglip-so400m-patch14-384"
CHECKPOINT_PATH = Path("cgrkzexw-599808")
TITLE = "<h1><center>JoyCaption Alpha Two (2024-09-26a)</center></h1>"
CAPTION_TYPE_MAP = {
	"Descriptive": [
		"Write a descriptive caption for this image in a formal tone.",
		"Write a descriptive caption for this image in a formal tone within {word_count} words.",
		"Write a {length} descriptive caption for this image in a formal tone.",
	],
	"Descriptive (Informal)": [
		"Write a descriptive caption for this image in a casual tone.",
		"Write a descriptive caption for this image in a casual tone within {word_count} words.",
		"Write a {length} descriptive caption for this image in a casual tone.",
	],
	"Training Prompt": [
		"Write a stable diffusion prompt for this image.",
		"Write a stable diffusion prompt for this image within {word_count} words.",
		"Write a {length} stable diffusion prompt for this image.",
	],
	"MidJourney": [
		"Write a MidJourney prompt for this image.",
		"Write a MidJourney prompt for this image within {word_count} words.",
		"Write a {length} MidJourney prompt for this image.",
	],
	"Booru tag list": [
		"Write a list of Booru tags for this image.",
		"Write a list of Booru tags for this image within {word_count} words.",
		"Write a {length} list of Booru tags for this image.",
	],
	"Booru-like tag list": [
		"Write a list of Booru-like tags for this image.",
		"Write a list of Booru-like tags for this image within {word_count} words.",
		"Write a {length} list of Booru-like tags for this image.",
	],
	"Art Critic": [
		"Analyze this image like an art critic would with information about its composition, style, symbolism, the use of color, light, any artistic movement it might belong to, etc.",
		"Analyze this image like an art critic would with information about its composition, style, symbolism, the use of color, light, any artistic movement it might belong to, etc. Keep it within {word_count} words.",
		"Analyze this image like an art critic would with information about its composition, style, symbolism, the use of color, light, any artistic movement it might belong to, etc. Keep it {length}.",
	],
	"Product Listing": [
		"Write a caption for this image as though it were a product listing.",
		"Write a caption for this image as though it were a product listing. Keep it under {word_count} words.",
		"Write a {length} caption for this image as though it were a product listing.",
	],
	"Social Media Post": [
		"Write a caption for this image as if it were being used for a social media post.",
		"Write a caption for this image as if it were being used for a social media post. Limit the caption to {word_count} words.",
		"Write a {length} caption for this image as if it were being used for a social media post.",
	],
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

		# <|image_start|>, IMAGE, <|image_end|>
		other_tokens = self.other_tokens(torch.tensor([0, 1], device=self.other_tokens.weight.device).expand(x.shape[0], -1))
		assert other_tokens.shape == (x.shape[0], 2, x.shape[2]), f"Expected {(x.shape[0], 2, x.shape[2])}, got {other_tokens.shape}"
		x = torch.cat((other_tokens[:, 0:1], x, other_tokens[:, 1:2]), dim=1)

		return x

	def get_eot_embedding(self):
		return self.other_tokens(torch.tensor([2], device=self.other_tokens.weight.device)).squeeze(0)



# Load CLIP
print("Loading CLIP")
clip_processor = AutoProcessor.from_pretrained(CLIP_PATH)
clip_model = AutoModel.from_pretrained(CLIP_PATH)
clip_model = clip_model.vision_model

assert (CHECKPOINT_PATH / "clip_model.pt").exists()
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
tokenizer = AutoTokenizer.from_pretrained(CHECKPOINT_PATH / "text_model", use_fast=True)
assert isinstance(tokenizer, PreTrainedTokenizer) or isinstance(tokenizer, PreTrainedTokenizerFast), f"Tokenizer is of type {type(tokenizer)}"

# LLM
print("Loading LLM")
print("Loading VLM's custom text model")
text_model = AutoModelForCausalLM.from_pretrained(CHECKPOINT_PATH / "text_model", device_map=0, torch_dtype=torch.bfloat16)
text_model.eval()

# Image Adapter
print("Loading image adapter")
image_adapter = ImageAdapter(clip_model.config.hidden_size, text_model.config.hidden_size, False, False, 38, False)
image_adapter.load_state_dict(torch.load(CHECKPOINT_PATH / "image_adapter.pt", map_location="cpu"))
image_adapter.eval()
image_adapter.to("cuda")


@spaces.GPU()
@torch.no_grad()
def stream_chat(input_image: Image.Image, caption_type: str, caption_length: str | int, extra_options: list[str], name_input: str, custom_prompt: str) -> tuple[str, str]:
	torch.cuda.empty_cache()

	# 'any' means no length specified
	length = None if caption_length == "any" else caption_length

	if isinstance(length, str):
		try:
			length = int(length)
		except ValueError:
			pass
	
	# Build prompt
	if length is None:
		map_idx = 0
	elif isinstance(length, int):
		map_idx = 1
	elif isinstance(length, str):
		map_idx = 2
	else:
		raise ValueError(f"Invalid caption length: {length}")
	
	prompt_str = CAPTION_TYPE_MAP[caption_type][map_idx]

	# Add extra options
	if len(extra_options) > 0:
		prompt_str += " " + " ".join(extra_options)
	
	# Add name, length, word_count
	prompt_str = prompt_str.format(name=name_input, length=caption_length, word_count=caption_length)

	if custom_prompt.strip() != "":
		prompt_str = custom_prompt.strip()
	
	# For debugging
	print(f"Prompt: {prompt_str}")

	# Preprocess image
	# NOTE: I found the default processor for so400M to have worse results than just using PIL directly
	#image = clip_processor(images=input_image, return_tensors='pt').pixel_values
	image = input_image.resize((384, 384), Image.LANCZOS)
	pixel_values = TVF.pil_to_tensor(image).unsqueeze(0) / 255.0
	pixel_values = TVF.normalize(pixel_values, [0.5], [0.5])
	pixel_values = pixel_values.to('cuda')

	# Embed image
	# This results in Batch x Image Tokens x Features
	with torch.amp.autocast_mode.autocast('cuda', enabled=True):
		vision_outputs = clip_model(pixel_values=pixel_values, output_hidden_states=True)
		embedded_images = image_adapter(vision_outputs.hidden_states)
		embedded_images = embedded_images.to('cuda')
	
	# Build the conversation
	convo = [
		{
			"role": "system",
			"content": "You are a helpful image captioner.",
		},
		{
			"role": "user",
			"content": prompt_str,
		},
	]

	# Format the conversation
	convo_string = tokenizer.apply_chat_template(convo, tokenize = False, add_generation_prompt = True)
	assert isinstance(convo_string, str)

	# Tokenize the conversation
	# prompt_str is tokenized separately so we can do the calculations below
	convo_tokens = tokenizer.encode(convo_string, return_tensors="pt", add_special_tokens=False, truncation=False)
	prompt_tokens = tokenizer.encode(prompt_str, return_tensors="pt", add_special_tokens=False, truncation=False)
	assert isinstance(convo_tokens, torch.Tensor) and isinstance(prompt_tokens, torch.Tensor)
	convo_tokens = convo_tokens.squeeze(0)   # Squeeze just to make the following easier
	prompt_tokens = prompt_tokens.squeeze(0)

	# Calculate where to inject the image
	eot_id_indices = (convo_tokens == tokenizer.convert_tokens_to_ids("<|eot_id|>")).nonzero(as_tuple=True)[0].tolist()
	assert len(eot_id_indices) == 2, f"Expected 2 <|eot_id|> tokens, got {len(eot_id_indices)}"

	preamble_len = eot_id_indices[1] - prompt_tokens.shape[0]   # Number of tokens before the prompt

	# Embed the tokens
	convo_embeds = text_model.model.embed_tokens(convo_tokens.unsqueeze(0).to('cuda'))

	# Construct the input
	input_embeds = torch.cat([
		convo_embeds[:, :preamble_len],   # Part before the prompt
		embedded_images.to(dtype=convo_embeds.dtype),   # Image
		convo_embeds[:, preamble_len:],   # The prompt and anything after it
	], dim=1).to('cuda')

	input_ids = torch.cat([
		convo_tokens[:preamble_len].unsqueeze(0),
		torch.zeros((1, embedded_images.shape[1]), dtype=torch.long),   # Dummy tokens for the image (TODO: Should probably use a special token here so as not to confuse any generation algorithms that might be inspecting the input)
		convo_tokens[preamble_len:].unsqueeze(0),
	], dim=1).to('cuda')
	attention_mask = torch.ones_like(input_ids)

	# Debugging
	print(f"Input to model: {repr(tokenizer.decode(input_ids[0]))}")

	#generate_ids = text_model.generate(input_ids, inputs_embeds=inputs_embeds, attention_mask=attention_mask, max_new_tokens=300, do_sample=False, suppress_tokens=None)
	#generate_ids = text_model.generate(input_ids, inputs_embeds=inputs_embeds, attention_mask=attention_mask, max_new_tokens=300, do_sample=True, top_k=10, temperature=0.5, suppress_tokens=None)
	generate_ids = text_model.generate(input_ids, inputs_embeds=input_embeds, attention_mask=attention_mask, max_new_tokens=300, do_sample=True, suppress_tokens=None)   # Uses the default which is temp=0.6, top_p=0.9

	# Trim off the prompt
	generate_ids = generate_ids[:, input_ids.shape[1]:]
	if generate_ids[0][-1] == tokenizer.eos_token_id or generate_ids[0][-1] == tokenizer.convert_tokens_to_ids("<|eot_id|>"):
		generate_ids = generate_ids[:, :-1]

	caption = tokenizer.batch_decode(generate_ids, skip_special_tokens=False, clean_up_tokenization_spaces=False)[0]

	return prompt_str, caption.strip()


def main():
	while True:
		caption_type = sys.stdin.readline().strip()
		caption_length = sys.stdin.readline().strip()
		extra_options = sys.stdin.readline().strip().split(",")
		name_input = sys.stdin.readline().strip()
		custom_prompt = sys.stdin.readline().strip()
		imagedata = sys.stdin.readline().strip()

		binary_data = base64.b64decode(imagedata)
		image_bytes = BytesIO(binary_data)

		with Image.open(image_bytes).convert("RGB") as img:
			prompt_str, captions = stream_chat(img, caption_type, caption_length, extra_options, name_input, custom_prompt)
			print("GENERATION START")
			print(captions)
			print("GENERATION END")
			sys.stdout.flush()

if __name__ == "__main__":
    main()