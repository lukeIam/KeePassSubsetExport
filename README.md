# KeePassSubsetExport 
KeePassSubsetExport is a [KeePass2](https://keepass.info) plugin which automatically exports a subset of entries (tag based) to new databases with different keys.

## Why?
I'm using the plugin to export some entries of my main database to another database which is [synced](https://syncthing.net) to my mobile devices.
Additionally, I'm sharing some other entries with my family.

## Disclaimer
This is my first KeePass plugin and I'm tried not to compromise security - but I can't guarantee it.  
**So use this plugin at your own risk.**  
If you have more experience with KeePass plugins, I would be very grateful if you have a look on the code.

## How to install?
- Download the latest release from [here](https://github.com/lukeIam/KeePassSubsetExport/releases)
- Place KeePassSubsetExport.plgx in the KeePass program directory
- Start KeePass and the plugin is automatically loaded (check the Plugin menu)

## How to use?
- Open the database containing the entries that should be exported
- Create a folder `SubsetExportSettings` under the root folder
- For each export job (target database) create a new entry:
  - set `Title` = `SubsetExport_MobilePhone`
  - set `Password` = The password for the target database
  (optional if `SubsetExport_KeyFilePath` is set)
  - add a string filed with the name `SubsetExport_KeyFilePath` and set a key file for target database (e.g. `C:\keys\mobile.key`)
  (optional if `Password` is set)
  - add a string filed with the name `SubsetExport_TargetFilePath` and set a target database (e.g. `C:\sync\mobile.kdbx`)
  - add a string filed with the name `SubsetExport_Tag` and set the tag that should be exported (e.g. `MobileSync`)
- Every time the (source) database is saved the target databases will be recreated automatically

![create](https://user-images.githubusercontent.com/5115160/38439682-da51a266-39de-11e8-9cc4-744d5a3f0dae.png)
