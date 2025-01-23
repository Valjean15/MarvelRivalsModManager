
# Marvel Rivals - Mod Manager
Mod manager build up using **WinUI3**, which was made in mind as a tool to be compatible with the Season 0 (using ~mods folder) and Season 1 (patch) (using [Repak](https://github.com/trumank/repak) method)

## How mods work on Marvel Rivals
In Season 0, mods in `.pak` format only needed to be placed directly into the ~mods folder in order to be used.

In Season 1 - pre patch, mods in order to be used must be unzipped (files in `.pak` format) and all mod files placed directly into the game's content folder.

In Season 1 - patch, mods in order to be used must be unzipped (files in `.pak` format) and all mod files packed on a single pak file to be later loaded.

## How to configure the manager
For the Season 0, only it's needed to set the *Enabled* folder to the ~mods on the game content directory.

For the Season 1 (patch), it's needed to download (can be using the manager or another source) the [Repak](https://github.com/trumank/repak).

![alt text](https://github.com/Valjean15/MarvelRivalsModManager/blob/master/Blob/Download.png)

After that you can configure the mods you wanna install, after managing the mods, just remain to click on the button *Unpack* (to unpack the mod enabled) and at last *Patch* to apply the mods unpacked.

# Mod manager
The mod manager is separated on three views, which are related between each other thought a layer of services.

## Settings
Here you configure the directories necessary for the manager to work correctly. By default only the when the app is launched the first time (due it would create aa file `usersettings_1.json` to store the settings variables), it would create the folder **_MarvelRivalModManager_** on `C:\Users\Public\Documents\`. This default configuration can be change any time later.

![alt text](https://github.com/Valjean15/MarvelRivalsModManager/blob/master/Blob/Settings.png)

 - **Content Folder**
Represent where the game content is located and where the mod would be deployed for the Season 1, the manager would try, on the first time is launched, to locate the Steam folder using the public registry values related to steam (`SOFTWARE\VALVE\` and `SOFTWARE\Wow6432Node\Valve\`) . For example, on Steam is located in `~/steamapp/common/Marvel Rivals/MarvelGame/Marvel/Content`.

 - **Mod directories**
Represents the directories used to manage the enabled and disabled states of mods. This was made on this way to maintain compatibility with the Season 0 mod management, and for later use when a proper mod bypass for Season 1 is made.
	
	There are two directories for this section *Enabled* and *Disabled* folder, were both are identical on structure. On the root of the folder the mods are stored, and a *profile* folder is created were each mod metadata is stored. Also if exists any image related for a mod, a folder *images* is created to store all the images related to the mod folder.
	
 - **Unpacker**
Represent were the mods are unpacked and merged to later be patched on the game content folder, also it contains the unpacker program (`repak.exe`) which is used to unpack `.pak` files with no hash password, also is used to pack the raw files again to later be moved to the game content

## Manager
The manager separate the mods into two sections *Enabled* and *Disabled*, which each one represent a folder, described on the **Settings** section, and by default only *Enabled* mods would be patch into the game raw files.

![alt text](https://github.com/Valjean15/MarvelRivalsModManager/blob/master/Blob/ContextMenu.png)

 The manager has a command bar with two options: 
 - Add: Open a dialog up to select a single o multiple mods, on formats (`.pak`, `.zip`, `.rar` and `.7z`). When a mod is added the mod would be decompressed to generate the metadata of the mod, in the case of the `.pak` files it would try to unpack it using the *unpacker* described on **Settings** section.
 - Remove: Delete selected the mods from the lists and related content, metadata and images.

Next to the command bar, there is a search box, which you can filter mods of the list using the field *Name* and *Tags* (this include the system tags and custom tags). 

To enable/disable a mod, you can select multiple mods and later just drag'n drop on the other list, each mod hace a context menu, which have the following options: 
 - Move: Move the mod to the othe list (enabled/disabled).
 - Edit: Open editor view where you can edit some metadata of the mod like the name, logo and custom tags for search on the bar.
 - Delete: Delete selected the mod related content, metadata and images.

![alt text](https://github.com/Valjean15/MarvelRivalsModManager/blob/master/Blob/Details.png)

## Actions
The actions view is more dedicated to apply the raw files of the mods (compressed or on `.pak ` format).

![alt text](https://github.com/Valjean15/MarvelRivalsModManager/blob/master/Blob/Action.png)

- **Unpack**
Retrieve all the mods on the *Enabled* list, and try to unpack into raw files and merge all this content into a folder called `extraction`, located on the **Unpacker** folder, to later be patched on the game folder thought the *Patch* option.

```mermaid
graph LR
mod((Mod)) -- Get file --> pak((PAK))
mod((Mod)) -- Get file --> compressed((Compressed))
pak((PAK)) --> unpacker((Unpacker))
unpacker((Unpacker)) -- Missing paker.exe --> ignored
unpacker((Unpacker)) -- Exist paker.exe --> validate
compressed((Compressed)) -- Extract a --> raw((Raw files)) --> validate 
compressed((Compressed)) -- Extract a --> pak
validate[Validate structure] -- Valid --> target((Extracted folder))
validate[Validate structure] -- Invalid --> ignored((Ignored))
```

![alt text](https://github.com/Valjean15/MarvelRivalsModManager/blob/master/Blob/Unpack.png)

-  **Patch**
Retrieve all the mods that are marked as *unpacked*, pack all files into a single `.pak` file and move it to the game folder content. If there is a disabled mod that is *unpacked* the manager would try to remove the files affected on the extraction folder.

- **Unpatch**
	Tries to delete the generated `.pak` file on the content folder.

- **Download**
	This options would try to download from [Mega folder](https://mega.nz/folder/m1xmxT4Y#J-wEYO5NyLgT_WWG13CMzA) the files needed to the manager to work as the `repak.exe`.

- **Delete**
	Tries to delete the downloaded resources.

![alt text](https://github.com/Valjean15/MarvelRivalsModManager/blob/master/Blob/Download.png)

- **Clear**
	Only clear the logs of the console.
