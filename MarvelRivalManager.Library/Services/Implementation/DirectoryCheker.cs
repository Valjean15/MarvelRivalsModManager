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

        /// <see cref="IDirectoryCheker.ModRawStructure(string)"/>
        public bool ModRawStructure(string folder)
        {
            return !string.IsNullOrEmpty(folder) && folder.DirectoryContainsSubfolder("Marvel/Content");
        }

        /// <see cref="IDirectoryCheker.RepakToolExist"/>
        public bool RepakToolExist()
        {
            return Directory.Exists(Configuration.Folders.RepackFolder)
                && File.Exists(Path.Combine(Configuration.Folders.RepackFolder, "repak.exe"));
        }
    }
}
