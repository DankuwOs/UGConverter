Updates GUIDs from one project to another.
This is not perfect, I tested with 2 prefabs and they both had some assets mess up. This should only be used in situations where its basically required.

Usage:
- Click `Setup Metadata From Folder` and select the Assets folder of the source project to load the metadata used to match files. The save to file variant does the same thing but will output a json in whatever folder you select for someone else to use as the target project.
- Click `Load Metadata From Folder (Or file)` and select the Assets folder of the target project to load the metadata for the files again. The file variant is obviously used for updating files for a project you don't have.
- Click `Output Updated Files To Folder` and select whatever folder contains assets you want to update, or just the whole project. Then select the folder where you want the updated files to be placed.


Get the executable from `https://github.com/DankuwOs/UGConverter/releases`
