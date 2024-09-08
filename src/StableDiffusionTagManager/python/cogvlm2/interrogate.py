"""
Original source taken from https://github.com/THUDM/CogVLM2/blob/main/basic_demo/cli_demo.py modified to support StableDiffusionTagManager and always load 4bit
"""

import torch
from PIL import Image
from transformers import AutoModelForCausalLM, AutoTokenizer, BitsAndBytesConfig
import sys
import base64
from io import BytesIO

MODEL_PATH = "THUDM/cogvlm2-llama3-chat-19B"
DEVICE = 'cuda' if torch.cuda.is_available() else 'cpu'
TORCH_TYPE = torch.bfloat16 if torch.cuda.is_available() and torch.cuda.get_device_capability()[
    0] >= 8 else torch.float16

tokenizer = AutoTokenizer.from_pretrained(
    MODEL_PATH,
    trust_remote_code=True
)

model = AutoModelForCausalLM.from_pretrained(
    MODEL_PATH,
    torch_dtype=TORCH_TYPE,
    trust_remote_code=True,
    quantization_config=BitsAndBytesConfig(load_in_4bit=True),
    low_cpu_mem_usage=True
).eval()

def main():
    while True:
        promptdata = sys.stdin.readline().strip()

        imagedata = sys.stdin.readline().strip()
        binary_data = base64.b64decode(imagedata)
        image_bytes = BytesIO(binary_data)

        with Image.open(image_bytes).convert("RGB") as image:
            history = []

            input_by_model = model.build_conversation_input_ids(
                tokenizer,
                query=promptdata,
                history=history,
                images=[image],
                template_version='chat'
            )

            inputs = {
                'input_ids': input_by_model['input_ids'].unsqueeze(0).to(DEVICE),
                'token_type_ids': input_by_model['token_type_ids'].unsqueeze(0).to(DEVICE),
                'attention_mask': input_by_model['attention_mask'].unsqueeze(0).to(DEVICE),
                'images': [[input_by_model['images'][0].to(DEVICE).to(TORCH_TYPE)]] if image is not None else None,
            }

            gen_kwargs = {
                "max_new_tokens": 2048,
                "pad_token_id": 128002,
                "top_k": 1,
            }

            with torch.no_grad():
                outputs = model.generate(**inputs, **gen_kwargs)
                outputs = outputs[:, inputs['input_ids'].shape[1]:]
                response = tokenizer.decode(outputs[0], skip_special_tokens=True)
                print("GENERATION START")
                print(response)
                print("GENERATION END")
                sys.stdout.flush()

if __name__ == "__main__":
    main()