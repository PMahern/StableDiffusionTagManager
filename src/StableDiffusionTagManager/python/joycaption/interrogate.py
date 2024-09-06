from PIL import Image
import sys
from app import stream_chat
from io import BytesIO
import base64

def main():
    while True:
        
        imagedata = sys.stdin.readline().strip()
        binary_data = base64.b64decode(imagedata)
        image_bytes = BytesIO(binary_data)

        with Image.open(image_bytes).convert("RGB") as img:
            captions = stream_chat(img)
            print("GENERATION START")
            print(captions)
            print("GENERATION END")
            sys.stdout.flush()

if __name__ == "__main__":
    main()