# MithrilToolbox

MithrilToolbox is a work-in-progress CLI tool made for processing files from the game World of Final Fantasy. While this tool was made with this specific game in mind, it may also work with other games made with the same engine (Silicon Studio's Orochi Engine 3).

## Usage

```
MithrilToolbox.exe <verb> -arg1 param1 -arg2 param2
```

These are the verbs that can be used with the program:

```
compress       Compresses a file using ZLIB

decompress     Decompresses a file using ZLIB

dat-unpack     Unpacks an Archived File (DAT) file

cms-export     Converts a Collision Mesh (CMS) file to GLTF

cms-import     Converts a GLTF file to Collision Mesh (CMS)

crv-export     Converts a Curve (CRV) file to JSON

crv-import     Converts a JSON file to Curve (CRV)

csd-export     Converts a Character Set (CSD) file to JSON

csd-import     Converts a JSON file to Character Set (CSD)

csh-export     Converts a Cell Sheet (CSH) file to CSV

csh-import     Converts a CSV file to Cell Sheet (CSH)

mdc-export     Converts a Model Configuration (MDC) file to JSON

mdc-import     Converts a JSON file to Model Configuration (MDC)

mdl-export     Converts a Model (MDL) file to GLB/GLTF

mdl-import     Converts a GLB/GLTF file to Model (MDL)

rail-export    Converts a Rail file to JSON

rail-import    Converts a JSON file to Rail

tex-export     Converts a texture (TEX) file to DDS

tex-import     Converts a DDS file to texture (TEX)
```

## Supported filetypes

These are the currently supported (totally or partially) filetypes:

* **Archived File (*.dat):** Container type file used in the PS4/PS Vita/Nintendo Switch versions of the game. Can be unpacked to a folder.
* **Cell Sheet (*.csh):** Datatable type file. Can be exported to/imported from CSV.
* **Character Set (*.csd):** Stageset type file (used for map configuration, object placement, etc). Can be exported to/imported from JSON.
* **Model (*.mdl):** Can be exported to/imported from GLTF.
* **Model Configuration (*.mdc):** Material file. Can be exported to/imported from JSON.
* **Texture (*.tex):** Can be exported to/imported from DDS.
* **Curve (*.crv):** Curve animation (used in 2D elements). Can be exported to/imported from JSON.
* **Rail (*.rail):** Controls the camera fixed points. Can be exported to/imported from JSON.
* **Collision Mesh (*.cms):** Contains the collision mesh and BVH. Can be exported to/imported from GLTF. 

## Acknowledgments

This tool wouldn't have been possible without [Random Talking Bush](https://github.com/RandomTBush) QuickBMS script for converting texture files and MaxScript model importer.
