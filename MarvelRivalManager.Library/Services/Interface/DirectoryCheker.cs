﻿namespace MarvelRivalManager.Library.Services.Interface
{
    /// <summary>
    ///     Service dedicated to check the files/folder to ensure are valid to
    ///     use in the application
    /// </summary>
    public interface IDirectoryCheker
    {
        /// <summary>
        ///     Validate if a mod folder is valid to be used in the application
        /// </summary>
        public bool ModRawStructure(string folder);

        /// <summary>
        ///     Validate if the repak tool is present in the application
        /// </summary>
        public bool RepakToolExist();
    }
}
