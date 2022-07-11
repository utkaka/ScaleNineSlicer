# Scale Nine Slicer

A tool for automating work with 9 slice scaling in Unity.

### Automatic 9 slice borders detection.

![Autodetection!](Documentation~/images/autodetection.gif "Autodetection")

### Trimming sliced sprite center to 1px.

![Trim center!](Documentation~/images/trim-center.gif "Trim center")

### Trimming extra transparency.

![Trim alpha!](Documentation~/images/trim-alpha.gif "Trim alpha")

## Installation

There are 3 ways to install this plugin:
- clone/download this repository and move the Plugins folder to your Unity project's *Assets* folder
- *(via Package Manager)* add the following line to Packages/manifest.json:
  - "com.utkaka.scale-nine-slicer": "https://github.com/utkaka/ScaleNineSlicer.git",
- *(via OpenUPM)* after installing openupm-cli, run the following command:
  - openupm add com.utkaka.scale-nine-slicer

## Usage:

### Context menu in Project view

You can select textures and/or folders with sprites and perform an action on all contained sprites.

![Context menu!](Documentation~/images/project-view-context.png "Context menu")

### Editor window (*Unity 2021.2+*)

If you want a more controlled result there is an editor window *(Window/2D/Sprite Nine Slicer)*.

![Editor window!](Documentation~/images/sprite-nine-slicer-editor.png "Editor window")

Features:

- Works with multiple selection.
- Manual borders setting. You can zoom with ctrl/cmd key pressed and set borders via input fields or draggable lines on the image.
- You can enable or disable trimmings.
- You can preview the result, extend it to see how it works. Also you can export extended sprite to png.

### API

Just create an instance of *SpriteInfo* and use it's public API. You can use it both in Unity Editor and at runtime.
