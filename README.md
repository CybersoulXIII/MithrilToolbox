# MithrilToolbox

MithrilToolbox is a work-in-progress CLI tool made for processing files from the game World of Final Fantasy. While this tool was made with this specific game in mind, it may also work with other games made with the same engine (Silicon Studio's Orochi Engine 3).

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