import sys
from io import BytesIO
import base64
from simple_lama_inpainting import SimpleLama
from PIL import Image

def main():
    print("Initializing SimpleLama")
    simple_lama = SimpleLama()
    print("SimpleLama initialization complete.")
    while True:
        imagedata = sys.stdin.readline().strip()
        binary_data = base64.b64decode(imagedata)
        image_bytes = BytesIO(binary_data)

        imagedata = sys.stdin.readline().strip()
        binary_data = base64.b64decode(imagedata)
        mask_bytes = BytesIO(binary_data)        

        with Image.open(image_bytes).convert("RGB") as img, Image.open(mask_bytes).convert('L') as mask:
            print("GENERATION START")
            result = simple_lama(img, mask)
            buffered = BytesIO()
            result.save(buffered, format="png")
            print(base64.b64encode(buffered.getvalue()).decode('utf-8'))
            print("GENERATION END")
            sys.stdout.flush()

if __name__ == "__main__":
    main()