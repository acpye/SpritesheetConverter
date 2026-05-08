  # Documentation

## Table of Contents
1. [Loading Objects](#loading-objects)
2. [Navigating with the Camera](#navigating-with-the-camera)
3. [Applying Post-Processing Effects](#applying-post-processing-effects)
4. [Making Frames for Spritesheet](#making-frames-for-spritesheet)
5. [Exporting Spritesheets](#exporting-spritesheets)

## Loading Objects
To load objects, navigate to the top-left of the application and select the 'File' menu tab. From there, press 'Import .glb file' and select the object from the file explorer. There is a sample model '[CesiumMan.glb](https://github.com/KhronosGroup/glTF-Sample-Assets/tree/main/Models/CesiumMan)' in the 'Objects' folder of the application provided by Khronos Group.

If the model has been converted into `.glb` or `.gltf` from another format, you may want to test to see if the conversion was done correctly. You can test to see if it works using the [Khronos validator website](https://github.khronos.org/glTF-Validator/) 

## Navigating with the Camera
| Action  | Control                  | Description                                              |
|---------|--------------------------|----------------------------------------------------------|
| Orbit   | MMB                      | Rotate the camera around a central pivot point.          |
| Pan     | Shift + MMB              | Move the camera view up, down, left, or right.           |
| Dolly   | Ctrl + MMB               | Move the camera physically closer to or further away.    |
| Zoom    | Mouse Wheel              | Magnify the view (similar to a lens adjustment).         |

## Applying Post-Processing Effects

To apply post-processing effects, navigate to the top-left of the application and select the 'Shaders' menu tab. From there, you can enable and disable a variety of post-processing effects, and can be layered on top of each other.
Currently supported effects are:
- Pixelation shading
- Cel shading
- Orthographic projection

Specific shader settings are found in the model inspector and additional camera settings are found in the camera inspector. 

## Making Frames for Spritesheet

To manually take snapshots, press the 'Take Snapshot' button. You are able to adjust snapshot dimensions, beware that dimensions are locked after the first snapshot is taken.

To automatically take snapshots of the models animation, press the 'Generate Spritesheet From Animation Frames' button. You can adjust the amount of snapshots taken per second with the slider above, and also automatically generate snapshots from multiple pre-defined angles using the 'Capture Multi-Perspective Spritesheet' button.

To automatically generate a 360° spritesheet of the model, press the 'Generate Spritesheet From Model Rotation' button. You can adjust the total amount of snapshots taken with the slider above. 

Snapshots automatically update inside the 'Animation Preview' window at the top right of the screen.

## Exporting Spritesheets

To export, you can either navigate to the 'File' menu tab in the top left and press the 'Export Spritesheet button' or in the 'Spritesheet Generator' window press 'Export'.

Exported spritesheets are sent to the 'Output' folder found in the project folder.
