from PIL import Image
import sys
from app import Predictor
from io import BytesIO

def main():
    predictor = Predictor()

    byte_data = sys.stdin.buffer.read()
    image_bytes = BytesIO(byte_data)
    
    with Image.open(image_bytes).convert("RGBA") as img:
        tagresults = predictor.predict(img, sys.argv[1], float(sys.argv[2]), 0, float(sys.argv[2]), 0)
        print("GENERATION RESULT")
        print(tagresults)

if __name__ == "__main__":
    main()