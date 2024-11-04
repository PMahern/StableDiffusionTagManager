import sys
from io import BytesIO
import base64
from simple_lama_inpainting import SimpleLama
from PIL import Image
from PIL import ImageDraw, PngImagePlugin
from ultralytics import YOLO, YOLOWorld
from huggingface_hub import hf_hub_download
from torchvision.transforms.functional import to_pil_image
import torch
import numpy as np

def mask_to_pil(masks: torch.Tensor, shape: tuple[int, int]) -> list[Image.Image]:
    """
    Parameters
    ----------
    masks: torch.Tensor, dtype=torch.float32, shape=(N, H, W).
        The device can be CUDA, but `to_pil_image` takes care of that.

    shape: tuple[int, int]
        (W, H) of the original image
    """
    n = masks.shape[0]
    return [to_pil_image(masks[i], mode="L").resize(shape) for i in range(n)]

def img2base64(image):
    buffer = BytesIO()
    
    # Save the image to the buffer in your desired format (e.g., PNG)
    image.save(buffer, format="PNG")
    
    # Get the byte data from the buffer
    img_bytes = buffer.getvalue()
    
    # Convert the byte data to a base64 string
    img_base64 = base64.b64encode(img_bytes).decode('utf-8')
    
    return img_base64

def merge_masks(masks):
    # Convert the first mask to a NumPy array as the base
    merged_array = np.array(masks[0], dtype=np.uint16)  # Use uint16 to prevent overflow during addition

    # Add each subsequent mask to the merged_array
    for mask in masks[1:]:
        merged_array += np.array(mask, dtype=np.uint16)

    # Clip values to the valid range [0, 255] to avoid overflow
    merged_array = np.clip(merged_array, 0, 255).astype(np.uint8)

    # Convert back to a PIL image
    merged_image = Image.fromarray(merged_array)

    return merged_image

def create_mask_from_bbox(
    bbox: list[float], shape: tuple[int, int]
) -> list[Image.Image]:
    """
    Parameters
    ----------
        bboxes: list[list[float]]
            list of [x1, y1, x2, y2]
            bounding boxes
        shape: tuple[int, int]
            shape of the image (width, height)

    Returns
    -------
        masks: list[Image.Image]
        A list of masks

    """
    mask = Image.new("L", shape, 0)
    mask_draw = ImageDraw.Draw(mask)
    mask_draw.rectangle(bbox, fill=255)
    return mask

def main():
    
    simple_lama = None
    current_yolo_model = None
    yolo = None
    
    while True:
        operation = sys.stdin.readline().strip()
        
        if operation == "lama":
            
            if simple_lama == None:
                print("Initializing SimpleLama")
                simple_lama = SimpleLama()
                print("SimpleLama initialization complete.")

            imagedata = sys.stdin.readline().strip()
            binary_data = base64.b64decode(imagedata)
            image_bytes = BytesIO(binary_data)

            imagedata = sys.stdin.readline().strip()
            binary_data = base64.b64decode(imagedata)
            mask_bytes = BytesIO(binary_data)        

            with Image.open(image_bytes).convert("RGB") as img, Image.open(mask_bytes).convert('L') as mask:
                print("GENERATION START")
                result = simple_lama(img, mask)
                print(img2base64(result))
                print("GENERATION END")
                sys.stdout.flush()
        elif operation == "yolo":
            modelpath = sys.stdin.readline().strip()
            threshold = sys.stdin.readline().strip()

            imagedata = sys.stdin.readline().strip()
            binary_data = base64.b64decode(imagedata)
            image_bytes = BytesIO(binary_data)

            with Image.open(image_bytes).convert("RGB")as img:
                if current_yolo_model != modelpath:
                    yolo = YOLO(modelpath)

                pred = yolo(img, conf=float(threshold))
                
                maskImages = []

                for result in pred:
                        bboxes = result.boxes.xyxy.cpu().numpy().tolist()
                        if result.masks is not None:
                            masks = result.masks
                            convertedMasks = mask_to_pil(masks.data, img.size)
                            maskImages.extend([maskImage.convert("L") for maskImage in convertedMasks])

                            for j, bbox in enumerate(bboxes):
                                if j >= len(masks) or masks[j] is None:
                                    bboxmask = create_mask_from_bbox(bbox, img.size)
                                    maskImages.append(bboxmask)
                        else:
                            maskImages.extend([create_mask_from_bbox(bbox, img.size) for bbox in bboxes])
                        
                if len(maskImages) > 0:
                    combined_mask = merge_masks(maskImages)
                    print("GENERATION START")
                    print(img2base64(combined_mask.convert("L")))
                    print("GENERATION END")
                    sys.stdout.flush()
                else:
                    print("NO RESULTS")
                    sys.stdout.flush()
                


if __name__ == "__main__":
    main()