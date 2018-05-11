# KeePassSubsetExport 
KeePassSubsetExport is a [KeePass2](https://keepass.info) plugin which automatically exports a subset of entries (tag based) to new databases with different keys.

## Why?
I'm using the plugin to export some entries of my main database to another database which is [synced](https://syncthing.net) to my mobile devices.
Additionally, I'm sharing some other entries with my family.

## Disclaimer
This is my first KeePass plugin and I tried not to compromise security - but I can't guarantee it.  
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

| Setting                                              | Description                                                             | Optional                                 | Example                               |
| ---------------------------------------------------- | ----------------------------------------------------------------------- | ---------------------------- | ------------------------------------- |
| `Title`                                                | Name of the job                                                         | No                                       | SubsetExport_MobilePhone              |
| `Password`                                             | The password for the target database                                    | Yes, if `SubsetExport_KeyFilePath` is set  | SecurePW!                             |
| `SubsetExport_KeyFilePath`<br>[string field]              | Path to a key file                                                      | Yes, if `Password` is set                  | C:\keys\mobile.key                    |
| `SubsetExport_TargetFilePath`<br>[string field]           | Path to the target database                                             | No                                       | C:\sync\mobile.kdbx                   |
| `SubsetExport_Tag`<br>[string field]                      | Tag for filtering                                                       | Yes, if `SubsetExport_Group` is set        | MobileSync                            |
| `SubsetExport_Group`<br>[string field]                    | Group for filtering                                                     | Yes, if `SubsetExport_Tag` is set          | MobileGroup                           |
| `SubsetExport_KeyTransformationRounds`<br>[string field]  | Overwrite the number of KeyTransformationRounds for the target database | Yes                                      | 10000000                              |
| `SubsetExport_RootGroupName`<br>[string field]                           | Overwrite the name of the root group in the target database             | Yes                                      | NewRootGroupName                      |

- Every time the (source) database is saved the target databases will be recreated automatically

![create](https://user-images.githubusercontent.com/5115160/38439682-da51a266-39de-11e8-9cc4-744d5a3f0dae.png)

## KeePassSubsetExport vs Partial KeePass Database Export
I started developing KeePassSubsetExport before [Partial KeePass Database Export](https://github.com/heinrich-ulbricht/partial-keepass-database-export) was published, so the basic functionality is similar.  
But KeePassSubsetExport has some advantages:
- The folder structure is copied to the target database
- Multiple export jobs are supported
- Key-File protection of the target databases is supported
- KeyTransformationRounds of the target database is set to the number of the source database (can be overwritten)
- Exports can be limited to a folder (can be combined with a tag filter)
