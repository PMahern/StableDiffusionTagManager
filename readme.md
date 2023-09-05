# Stable Diffusion Tag Manager

Stable Diffusion Tag Manager is a simple desktop GUI application for managing an image set for training/refining a stable diffusion (or other) text-to-image generation model.  The main goal of this program is to combine several common tasks that are needed to prepare and tag images before feeding them into a set of tools like [these scripts](https://github.com/kohya-ss/sd-scripts) by [kohya-ss](https://github.com/bmaltais/kohya_ss).

## Prequisites

Stable Diffusion Tag Manager is a stand alone application with no prequisites to launch the application on linux, osx, and windows. However, some of it's functionality relies on other projects which have prerequisites.

- Comic book panel extraction uses the [kumiko](https://github.com/njean42/kumiko) library by [njean42](https://github.com/njean42), which requires python.
- Image interrogation and touchup via stable diffusion rely on a running instance of [Automatic1111's](https://github.com/AUTOMATIC1111) [stable diffusion webui](https://github.com/AUTOMATIC1111/stable-diffusion-webui) that can be reached from the local system.

## Building the project

The project is currently developed in Visual Studio 2022. Building it is pretty straightforward. 

- Clone the repo
- Get the latest submodules by executing the command `git submodule update --init --recursive` in the repo root (this is for kumiko).
- Build the solution `src/StableDiffusionTagManager.sln`.
  
## Usage

Users can load a folder of images with corresponding tag files, when a folder is opened for the first time they will be prompted to create a project. Projects are not a requirement but allow some settings that are global to the image set.

## Projects

![image](https://github.com/PMahern/StableDiffusionTagManager/assets/18010074/0707f413-71bd-4f42-a9bd-bb62b71480a9)

Projects allow you to specify settings to help expedite certain tasks. The default prompt prefix will add the entered text as a prefix to the tags currently defined on an image when opening the image touch up window. The default negative prompt and denoise stregth will be automatically entered into their respective fields on the touch up screen. The image size setting specifies a size that all cropped images will be resized to, with the intent that eventually all images in the set will be resized to the same size since models tend to train better if the images are all the same size and match the size the original base model was trained on (such as 512x512 pixels for stable diffusion 1.5). If no image size is specified or 0 is entered into either dimension then images will be cropped to their original dimensions.

After loading your image set you'll be presented with the following layout.

![image](https://github.com/PMahern/StableDiffusionTagManager/assets/18010074/609b450d-d4c2-43f5-8ec0-867a9ce73410)

Across the top are the images in the set and on the right are the tags for the currently selected image. On the left is an image viewer with the currently selected image and some simple controls for manipulating the image.

## The Image Viewer 

The image viewer itself has some tools for simple editing of the image. There's a selection mode for selecting a region in the photo to crop it into a new photo, a painting mode for simple drawing (mostly to paint over things you don't want in the image set), a button to extract comic panels from the image, a button to launch a "touch up" utility for using stable diffusion to inpaint on the image, and an interrogate button for generating tags for the current image using AUTOMATIC1111's built in interrogate tool. In select mode there is a lock button in the top right which will lock the selection region to it's current aspect ratio. This allows you to for instance lock the aspect ratio to 1:1 and drag a selection area that will always be a square, and with the image size set to 512x512 in the project settings the cropped image will be resized to 512x512 without stretching the image in either dimension.

### Image "Touch up"

After clicking the "Touch up Image" button a new window will popup which will allow you to feed the image into AUTOMATIC1111's stable diffusion webui and pick a new version. It has a limited set of inputs you can feed into the inpaint function, it's not meant to replace the full set of functions currently already available in stable diffusion webui's interface. If there's any missing inputs that seem like they should be added feel free to suggest!

![image](https://github.com/PMahern/StableDiffusionTagManager/assets/18010074/0666ccc9-f21b-4b82-a4ba-04424a3cde90)

![image](https://github.com/PMahern/StableDiffusionTagManager/assets/18010074/f393e433-36c2-4e03-8cd2-c1583196864d)

Probably not the best example, but the intent is that you can fix up images before using them to train, removing elements like speech bubbles or other characters from the scene.

## Tagging

Upon startup, the application will look for a tags.csv in the folder where the exe lies. It expects a csv with two columns, the first being a tag name and the second being a number. It will order the tags in descending order and use these 
as autocomplete suggestions anywhere you might type a tag. One such example file can be found [here](https://github.com/stmobo/Machine-Learning/blob/master/danbooru-chars.csv).

It's usually best to try and use the interrogate function to generate an initial set of tags. It hallucinates a lot of tags but you can quickly trim the excess ones and it often times may spot things you didn't think to add. After getting an initial set of tags there are several hotkeys to help with the extremely tedious process of tagging.

- Shift + enter will add a tag in front of the currently focused tag (or at the front of all the tags if none is selected).
- Ctrl + enter will add a tag to the beginning of the current tag set
- Alt + enter will add a tag to the end of the current tag set
- Shift + left or right arrow will move focus between the current image's tags
- Ctrl + left or right will move the currently focused tag left or right
- Alt + left or right will move between images in the set
- Ctrl + delete will delete the currently focused tag
- Alt + delete will delete the currently selected image (with a warning)



### Thanks

This project wouldn't be possible without the awesome Avalonia ui library and the aforementioned other libraries and tools. I also grabbed the base source code for the image viewer control from the [UVtools library](https://github.com/sn4k3/UVtools).
