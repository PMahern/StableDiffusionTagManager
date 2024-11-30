from PIL import Image
import sys
from app import Predictor
from io import BytesIO
import time
import base64

def main():
    predictor = Predictor()

    while True:
        
        imagedata = sys.stdin.readline().strip()
        binary_data = base64.b64decode(imagedata)
        image_bytes = BytesIO(binary_data)
        
        with Image.open(image_bytes).convert("RGBA") as img:
            tagresults = predictor.predict(img, sys.argv[1], float(sys.argv[2]), 0, float(sys.argv[2]), 0)
            print("GENERATION START")
            print(tagresults)
            print("GENERATION END")
            sys.stdout.flush()

if __name__ == "__main__":
    main()