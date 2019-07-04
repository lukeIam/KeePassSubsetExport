# KeePassSubsetExport 
KeePassSubsetExport is a [KeePass2](https://keepass.info) plugin which automatically exports a subset of entries (tag based) to new databases with different keys.

[![Build Status](https://lukeiam.visualstudio.com/KeePassSubsetExport/_apis/build/status/KeePassSubsetExport-Build "View build on VisualStudio online")](https://lukeiam.visualstudio.com/KeePassSubsetExport/_build/latest?definitionId=1)
[![Quality Status](https://sonarcloud.io/api/project_badges/measure?project=KeePassSubsetExport&metric=alert_status "View project on SonarCloud")](https://sonarcloud.io/dashboard?id=KeePassSubsetExport)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=KeePassSubsetExport&metric=coverage "View coverage on SonarCloud")](https://sonarcloud.io/component_measures?id=KeePassSubsetExport&metric=coverage)
[![Latest release](https://img.shields.io/github/release/lukeiam/KeePassSubsetExport.svg?label=latest%20release)](https://github.com/lukeIam/KeePassSubsetExport/releases/latest)
[![Github All Releases](https://img.shields.io/github/downloads/lukeIam/KeePassSubsetExport/total.svg)](https://github.com/lukeIam/KeePassSubsetExport/releases)
[![License](https://img.shields.io/github/license/lukeIam/KeePassSubsetExport.svg)](https://github.com/lukeIam/KeePassSubsetExport/blob/master/LICENSE)

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

| Setting                                                   | Description                                                             | Optional                                   | Example                                 |
| --------------------------------------------------------- | ----------------------------------------------------------------------- | ------------------------------------------ | --------------------------------------- |
| `Title`                                                   | Name of the job                                                         | No                                         | `SubsetExport_MobilePhone`              |
| `Password`                                                | The password for the target database                                    | Yes, if `SubsetExport_KeyFilePath` is set  | `SecurePW!`                             |
| `SubsetExport_KeyFilePath`<br>[string field]              | Path to a key file                                                      | Yes, if `Password` is set                  | `C:\keys\mobile.key`                    |
| `SubsetExport_TargetFilePath`<br>[string field]           | Path to the target database.<br>(Absolute, or relative to source database parent folder.) | No                       | `C:\sync\mobile.kdbx`<br>or<br>`mobile.kdbx`<br>or<br>`..\mobile.kdbx` |
| `SubsetExport_Tag`<br>[string field]                      | Tag(s) for filtering (`,` to delimit multiple tags - `,` is not allowed in tag names)| Yes, if `SubsetExport_Group` is set        | `MobileSync`                            |
| `SubsetExport_Group`<br>[string field]                    | Group(s) for filtering (`,` to delimit multiple groups - `,` is not allowed in group names)| Yes, if `SubsetExport_Tag` is set          | `MobileGroup`                           |
| `SubsetExport_KeyTransformationRounds`<br>[string field]  | Overwrite the number of KeyTransformationRounds for AesKdf              | Yes                                        | `10000000`                              |
| `SubsetExport_RootGroupName`<br>[string field]            | Overwrite the name of the root group in the target database             | Yes                                        | `NewRootGroupName`                      |
| `SubsetExport_FlatExport`<br>[string field]               | If `True` no groups will be created in the target database (flat export)| Yes (defaults to `False`)                  | `True`                                  |
| `SubsetExport_OverrideTargetDatabase`<br>[string field]   | If `True` the traget database will be overriden, otherwise the enries will added to the target database | Yes (defaults to `True`) | `True`                    |
| `SubsetExport_OverrideEntryOnlyNewer`<br>[string field]   | If `True` only newer entries will overrides older entries (`OverrideTargetDatabase` is `False`)| Yes (defaults to `False`) | `True`                             |
| `SubsetExport_OverrideEntireGroup`<br>[string field]      | If `True` will override entire group in target Database (`OverrideTargetDatabase` is `False`)| Yes (defaults to `False`) | `True`                             |
| `SubsetExport_Argon2ParamIterations`<br>[string field]    | Overwrite the number of iterations of Argon2Kdf                         | Yes                                        | `2`                                     |
| `SubsetExport_Argon2ParamMemory`<br>[string field]        | Overwrite the memory parameter of Argon2Kdf                             | Yes                                        | `1048576` = 1MB                         |
| `SubsetExport_Argon2ParamParallelism`<br>[string field]   | Overwrite the parallelism parameter of Argon2Kdf<br>Typical parallelism values should be less or equal than to two times the number of available processor cores (less if increasing does not result in a performance increase)                                                                              | Yes                                        | `2`                                     |

- Every time the (source) database is saved the target databases will be recreated automatically
- To disable an export job temporarily just move its entry to another folder
- If both `SubsetExport_Tag` and `SubsetExport_Group` are set, only entries matching *both* will be exported

![create](https://user-images.githubusercontent.com/5115160/38439682-da51a266-39de-11e8-9cc4-744d5a3f0dae.png)

## KeePassSubsetExport vs Partial KeePass Database Export
I started developing KeePassSubsetExport before [Partial KeePass Database Export](https://github.com/heinrich-ulbricht/partial-keepass-database-export) was published, so the basic functionality is similar.  
But KeePassSubsetExport has some advantages:
- The folder structure is copied to the target database
- Multiple export jobs are supported
- Key-File protection of the target databases is supported
- KeyTransformationRounds of the target database is set to the number of the source database (can be overwritten)
- Exports can be limited to a folder (can be combined with a tag filter)
- Limited Field References support (in the export job password field and the entries username and password fields)
