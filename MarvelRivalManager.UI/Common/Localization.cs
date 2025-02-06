using MarvelRivalManager.Library.Entities;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace MarvelRivalManager.UI.Common
{
    /// <summary>
    ///     This class is used to localize the application.
    /// </summary>
    /// <remarks>
    ///     This lated should be replaced for a reource file.
    /// </remarks>
    internal static class Localization
    {
        public const string SELECT_FOLDER = "Select folder";
        public const string MOVE = "Move";
        public const string DELETE = "Delete";
        public const string EDIT = "Edit";
        public const string CANCEL = "Cancel";
        public const string EVALUATE = "Evaluate";
        public const string CLEAR = "Clear";
        public const string ENTER_A_VALUE = "Enter a value";
        public const string ENTER = "Enter";
        public const string PATCH = "Patch";
        public const string UNPATCH = "Unpatch";
        public const string DOWNLOAD = "Download";
        public const string HOME = "Home";
        public const string TITLE = "Marvel Rivals - Mods Manager";
        public const string TAB_MANAGER = "Mods";
        public const string TAB_ACTIONS = "Actions";
        public const string TAB_SETTINGS = "Settings";
        public const string PROFILE = "Profile";
        public const string ADD = "Add";
        public const string ENABLED = "Enabled";
        public const string DISABLED = "Disabled";
        public const string ENTER_PROFILE_NAME = "Enter profile name";
        public const string GO_BACK = "Go back";
        public const string SAVE = "Save";
        public const string FILENAME = "Filename";
        public const string STATUS = "Status";
        public const string METADATA = "Metadata";
        public const string ORDER = "Order";
        public const string TAGS = "Tags";
        public const string SYSTEM_TAGS = "System tags";
        public const string FILE_INFORMATION = "File information";
        public const string PROFILE_FILE_INFORMATION = "Profile file information";
        public const string EXTRACTION_INFORMATION = "Extraction information";
        public const string MOD_INFORMATION = "Mod information";
        public const string FILE_PATH = "Filepath";
        public const string LOCATION = "Location";
        public const string EXTENSION = "Extension";
        public const string PATH = "Path";
        public const string CONTENT = "Content";
        public const string GAME = "Game";
        public const string CONTENT_FOLDER = "Content folder";
        public const string MODS_DIRECTORY = "Mods directory";
        public const string PACKER = "Packer";
        public const string EXECUTABLE_FOLDER = "Executable folder";
        public const string THEME = "Theme";
        public const string OPTIONS = "Options";
        public const string MULTIPLE_MODS_PROFILE = "Multiple mods profile";
        public const string DEPLOY_ON_SEPARATE_FILES = "Deploy on separate files";
        public const string EVALUATE_ON_UPDATE = "Evaluate on update";
        public const string IGNORE_PACKER_TOOL = "Ignore packer tool";
        public const string USE_SINGLE_THREAD = "Use single thread for actions";
        public const string ABOUT = "About the manager";
        public const string ABOUT_VERSION = "Version";
        public const string ABOUT_CONTACT = "Discord";
    }

    /// <summary>
    ///     Application errors
    /// </summary>
    internal static class Errors
    {
        public const string INVALID_ENUM = "Generic parameter 'TEnum' must be an enum.";
    }

    /// <summary>
    ///     Application log messages
    /// </summary>
    internal static class LogMessages
    {
        private static readonly ConcurrentDictionary<string, string> _localization = new()
        {
            // General validatons
            ["GAME_FOLDER_NOT_FOUND"] = "Game content folder is not set",
            ["REPACK_TOOL_IGNORED"] = "The Repack tool is ignored on the settings option section",
            ["REPACK_PATH_NOT_FOUND"] = "The unpacker executable path is required",
            ["REPACK_EXE_NOT_FOUND"] = "The unpacker executable do not exist on the unpacker folder",

            // Patch
            ["STARTING_PATCH"] = "Starting patching folder",
            ["SKIPPING_PATCH"] = "Skipping patch",
            ["SKIPPING_CLEAN"] = "Skipping clean",
            ["SKIPPING_DELETE"] = "No need to delete",
            ["SUCCESS_PATCH"] = "Content folder patched successfully - {Time}",
            ["PACKED_FILES_NOT_AVAILABLE"] = "The packed content file do not exist",
            ["CONTENT_DO_NOT_EXIST"] = "The file ({Name}) do not exist",
            ["MOD_FILES_DO_NOT_EXIST"] = "The mods patch files do no exist",
            ["DELETING_MOD_FILES"] = "Deleting mods patch files...",
            ["DELETING_SUCCESS_MOD_FILES"] = "Deleting mods patch files complete - {Time}",
            ["UPDATING_MOD_FILES"] = "Updating mod status...",
            ["UPDATING_SUCCESS_MOD_FILES"] = "Updating mod status complete - {Time}",
            ["CONTENT_FOLDER_DO_NOT_EXIST"] = "The unpacked content folder do not exist",
            ["CLEANING_DISABLED_MODS"] = "Cleaning disabled mods",
            ["CLEANING_SUCCESS_DISABLED_MODS"] = "Cleaning disabled mods successfully - {Time}",
            ["PATCHING_GAME_PATCH_FILES"] = "Renaming game patch files",
            ["PATCHING_GAME_PATCH_FILES_COMPLETE"] = "Renaming game patch files complete",

            // Packer
            ["CREATING_EXTRACTION_FOLDER"] = "Extraction folder do not exist, creating folder...",
            ["EXTRACTION_FOLDER_NOT_FOUND"] = "Extraction folder do not exist",
            ["NO_VALID_MODS_TO_UNPACK"] = "No valids mods to unpack...",
            ["UNPACKING_MODS"] = "Unpacking mods...",
            ["UNPACKING_SUCCESS_MODS"] = "Unpacking mods complete - {Time}",
            ["UNPACKING_MOD_SINGLE"] = "- Unpacking mod {Name}",
            ["ERROR_UNPACKING_MOD_SINGLE"] = "- Failed to unpack mod {Name}",
            ["PACKING_MODS"] = "Packing mods..",
            ["PACKING_SUCCESS_MODS"] = "Packing mods complete - {Time}",
            ["UPDATING_MOD_FILES_ALIAS"] = "Updating {Name} mods status...",
            ["UPDATING_SUCCESS_MOD_FILES_ALIAS"] = "Updating {Name} mods status complete - {Time}",

            // Download client
            ["REPACK_TOOL_FOLDER_NOT_DEFINED"] = "Repack folder is not defined",
            ["REPACK_TOOL_FOLDER_NOT_FOUND"] = "Repack folder do not exist, creating folder",
            ["REPACK_TOOL_FOLDER_DELETED"] = "Deleting repack folder content",
            ["REPACK_TOOL_ALREADY_DOWNLOADED"] = "Resource folder Repack already downloaded",
            ["REPACK_TOOL_VALIDATING_DOWNLOAD"] = "Validating if the Repack exists",
            ["REPACK_TOOL_DOWNLOAD_WAS_INVALID"] = "The Repack cannot be found",
            ["REPACK_TOOL_DOWNLOAD_WAS_VALID"] = "The Repack was found",
            ["REPACK_TOOL_NEW_VERSION_AVAILABLE"] = "The Repack tool has a new version available",

            ["CLIENT_LOGIN"] = "Login into service...",
            ["CLIENT_LOGIN_COMPLETE"] = "Login completed - {Time}",
            ["CLIENT_RESOURCE_LOOKUP"] = "Downloading the {Name} resource...",
            ["CLIENT_RESOURCE_NOT_FOUND"] = "Resource {Name} not found in the service",
            ["CLIENT_RESOURCE_PROGRESS"] = "Download of the resource {Name}",
            ["CLIENT_RESOURCE_DOWNLOADED"] = "Download of the resource completed - {Time}",
            ["CLIENT_RESOURCE_MOVING"] = "Moving the download to resource folder for {Name}",
            ["CLIENT_RESOURCE_MOVING_COMPLETE"] = "Moving the resource completed - {Time}",
            ["CLIENT_RESOURCE_DECOMPRESS"] = "Decompressing the download for resource {Name}",
            ["CLIENT_RESOURCE_DECOMPRESS_COMPLETE"] = "Decompressing the files completed - {Time}",
            ["CLIENT_RESOURCE_ERROR"] = "Error occurred trying to download backup resource => {Name}",
            ["CLIENT_LOGOUT"] = "Logout from service...",

            ["CLIENT_RESOURCE_FOLDER_NOT_DEFINED"] = "The resource folder is not defined",
        };

        public static string Get(string[] keys, PrintParams @params)
        {
            if (keys is null || keys.Length == 0)
                return string.Empty;

            var localization = string.Join(". ", keys
                .Select(key =>
                {
                    if (!_localization.TryGetValue(key, out var localization))
                        return string.Empty;

                    if (!string.IsNullOrEmpty(@params.Name))
                        localization = localization.SetParam(@params.Name, "{Name}");

                    if (!string.IsNullOrEmpty(@params.Time))
                        localization = localization.SetParam(@params.Time, "{Time}");

                    return localization;
                })
                .Where(value => !string.IsNullOrEmpty(value)));

            return $"[{DateTime.Now:HH:mm:ss}] {string.Join("", @params.Action.Take(10)),-5} | {localization}";
        }

        private static string SetParam(this string localization, string value, string param)
        {
            return localization.Replace($"{param}", value);
        }
    }
}
