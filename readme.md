# Stable Diffusion Tag Manager

Stable Diffusion Tag Manager is a simple desktop GUI application for managing an image set for training/refining a stable diffusion (or other) text-to-image generation model.  The main goal of this program is to combine several common tasks that are needed to prepare and tag images before feeding them into a set of tools like [these scripts](https://github.com/kohya-ss/sd-scripts) by [kohya-ss](https://github.com/bmaltais/kohya_ss).

## Prequisites

Stable Diffusion Tag Manager is a stand alone application with no prequisites to launch the application on linux and windows. However, some of it's functionality relies on other projects which have prerequisites.

- Comic book panel extraction uses the [kumiko](https://github.com/njean42/kumiko) library by [njean42](https://github.com/njean42), which requires python.
- Image interrogation and touchup via stable diffusion rely on a running instance of [Automatic1111's](https://github.com/AUTOMATIC1111) [stable diffusion webui](https://github.com/AUTOMATIC1111/stable-diffusion-webui) that can be reached from the local system.

## Building the project

The project is currently developed in Visual Studio 2022. Building it is pretty straightforward. 

- Clone the repo
- Get the latest submodules by executing the command `git submodule update --init --recursive` in the repo root (this is for kumiko).
- Build the solution `src/StableDiffusionTagManager.sln`.
  
## Usage

Users can load a folder of images with corresponding tag files, when a folder is opened for the first time they will be prompted to create a project. Projects are not a requirement but allow some settings that are global to the image set.

![image](https://github.com/PMahern/StableDiffusionTagManager/assets/18010074/0707f413-71bd-4f42-a9bd-bb62b71480a9)

After loading your image set you'll be presented with the following layout.

![image](https://github.com/PMahern/StableDiffusionTagManager/assets/18010074/609b450d-d4c2-43f5-8ec0-867a9ce73410)

Across the top are the images in the set and on the right are the tags for the currently selected image. On the right is an image viewer with the currently selected image and some simple controls for manipulating the image and on the right are the tags for the current image.

## The Image Viewer 

The image viewer itself has some tools for simple editing of the image. There's a selection mode for selecting a region in the photo to crop it into a new photo, a painting mode for simple drawing (mostly to paint over out things you don't want in the image set), a button to extract comic panels from the image, a button to launch a "touch up" utility for using stable diffusion to inpaint on the image, and an interrogate button for generating tags for the current image using AUTOMATIC1111's built in interrogate tool.

### Image "Touch up"

After clicking the "Touch up Image" button a new window will popup which will allow you to feed the image into AUTOMATIC1111's stable diffusion webui and pick a new version. It has a limited set of inputs you can feed into the inpaint function, it's not meant to replace the full set of functions currently already available in stable diffusion webui's interface. If there's any missing inputs that seem like they should be added feel free to suggest!

![image](https://github.com/PMahern/StableDiffusionTagManager/assets/18010074/0666ccc9-f21b-4b82-a4ba-04424a3cde90)

![image](https://github.com/PMahern/StableDiffusionTagManager/assets/18010074/f393e433-36c2-4e03-8cd2-c1583196864d)

Probably not the best example, but the intent is that you can fix up images before using them to train, removing elements like speech bubbles or other characters from the scene.

## Tagging

It's usually best to try and use the interrogate function to generate an initial set of tags. It hallucinates a lot of tags but you can quickly trim the excess ones and it often times may spot things you didn't think to add. After getting an initial set of tags there are several hotkeys to help with the extremely tedious process of tagging.

- Shift + enter will add a tag in front of the currently focused tag (or at the front of all the tags if none is selected).
- Ctrl + enter will add a tag to the beginning
- Alt + enter will add a tag to the end of the current tag set
- Shift + left or right arrow will move focus between the current images tags
- Ctrl + left or right will move the currently focused tag left or right
- Alt + left or right will move between images in the set
- Ctrl + delete will delete the currently focused tag
- Alt + delete will delete the currently selected image

### Thanks

This project wouldn't be possible without the awesome Avalonia ui library and the aforementioned other libraries and tools. I also grabbed the base source code for the image viewer control from the [UVtools library](https://github.com/sn4k3/UVtools).
