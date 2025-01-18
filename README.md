Mod Manager based on WinUI3
Compatible with .pak, .zip, .rar and .7z files
Evaluate for each file if the mod can be applied based on file structure of the game
Add, Edit Enable, Disable and Delete mods
Patch mod files as raw format (Season 1 bypass)

Restore original files (to reset all applied mods or by category)
Disable or enable the mods by category
How to use
The app only has four pages

Settings
On the settings page you can configure all the stuff is necesary to the app can work:

Content Folder: The folder where is located the game files, for example in steam is located on
~/steamapp/common/Marvel Rivals/MarvelGame/Marvel/Content

Mod directories - Enabled: The folder where you wanna store the mod you wanna use on the game, also can be use to store the mods like on the Season 0 on the ~mods folder.

Mod directories - Disabled: The folder where where your mod you don't wanna use on the game.

Unpacker - Executable folder: The folder where the unpacker file is located, this is the executable that the app would use to extract all .pak files to later apply on the game files. I recommend use repak.exe, I would attach it on this mod also.﻿

Backups: This folder represent an backup of the encrypted .pak file of the game, for each category is separated to make available restore or remove invalid files from the original game. This backup content you can get it via fmodel (this method require the encrypt key) or downloading a backup from the comunity (recoment discord server to get this)

Characters: This one represent the file pakchunkCharacter-Windows.pak
UI: This one represent the file pakchunkHQ-Windows.pak and pakchunkLQ-Windows.pak

Movies: This one represent the file pakchunkMovies-Windows.pak


This folder must have the Content folder of the game structure, for example the Character folder, which is pakchunkCharacter-Windows.pak, only one folder which is  /Marvel/<all the content> Folder, that represents the same folder as the game content (the same as the Game Content) ~\steamapps\common\MarvelRivals\MarvelGame\Marvel\Content\Marvel\<all the content>
﻿- Content    =>   ~\steamapps\common\MarvelRivals\MarvelGame\Marvel\Content
        - Structure  =>   Marvel\<all the content>
Actions
On the action tab you have six options:

Unpack: As said, unpack all the mod you enabled

Patch: As said, patch all the mods you unpacked earlier, also this one can detect if a mod was unpacked or applied before (by this app) and disable it to make a clean patch of the mods you wanna apply.

Restore: Youn can restore the original files from the backup folders, to clean all the mods and restore the original state of the game.

Enable/Disable: With this one you can diasable all mods at once without restoring the content of the game, this only rename the backup files to ensure the game would load it and skip the mods.

Clear: Clear the log from the screen.
Mods
On this screen you can manage all the mod you wanna apply on the compatible format, this one use the enable and disable directories configured, the app classified the mods automatically which you can filter via a textbox, also search by name or your own tags, also this show an image you upload for the mod.

- You can add mod via a button select and add multiple at once
- You can enable/disable mod dragging between each list.
- You can remove multiple mod selecting all (CTRL + A)  and click on delete.
- You can right click to move the mod to (enable/disable) list
- You can  right click to delete a mod you don't wanna have
- You can right click to edit common properties to customize the search of your mods
Mods - Detail
On this screen you can manage the common properties to customize your mods for easy manage, like custom tags (separated by comma), custom image and custom name (different from the filename).
Also this screen show you all the info related to the mod which the app is using to work like internal statuses, unpacked file list and filepaths.

If you wanna download the source code and compile it for yourselft or make a brach;
