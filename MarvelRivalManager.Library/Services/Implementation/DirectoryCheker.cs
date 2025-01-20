using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IDirectoryCheker"/>
    internal class DirectoryCheker(IEnvironment configuration) : IDirectoryCheker
    {
        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        #endregion

        /// <see cref="IDirectoryCheker.BackupResource(KindOfMod)"/>
        public bool BackupResource(KindOfMod kind)
        {
            return kind switch
            {
                KindOfMod.All => new bool[] { 
                    BackupResource(KindOfMod.Characters), 
                    BackupResource(KindOfMod.UI), 
                    BackupResource(KindOfMod.Movies), 
                    BackupResource(KindOfMod.Audio) }
                .All(x => x),
                _ => BackupResource(Configuration.Folders.BackupResources.Get(kind), BackupFolders.BasicStructure(kind))
            };
        }

        /// <see cref="IDirectoryCheker.ModRawStructure(string)"/>
        public bool ModRawStructure(string folder)
        {
            return !string.IsNullOrEmpty(folder) && folder.DirectoryContainsSubfolders(["Marvel", "Marvel\\Content"]);
        }

        /// <see cref="IDirectoryCheker.UnpackerExist"/>
        public bool UnpackerExist()
        {
            return Directory.Exists(Configuration.Folders.UnpackerExecutable)
                && File.Exists(Path.Combine(Configuration.Folders.UnpackerExecutable, "repak.exe"));
        }

        #region Private method

        /// <summary>
        ///    Check if the folder contains the required subfolders
        /// </summary>
        public static bool BackupResource(string folder, string[] required)
        {
            return string.IsNullOrEmpty(folder) && folder.DirectoryContainsSubfolders(required);
        }

        #endregion
    }
}
