from PIL import Image
import sys
from app import stream_chat
from io import BytesIO

def main():
    byte_data = sys.stdin.buffer.read()
    image_bytes = BytesIO(byte_data)

    with Image.open(image_bytes).convert("RGB") as img:
        captions = stream_chat(img)
        print("GENERATION RESULT")
        print(captions)

if __name__ == "__main__":
    main()