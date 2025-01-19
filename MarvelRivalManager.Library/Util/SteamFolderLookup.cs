using Microsoft.Win32;

namespace MarvelRivalManager.Library.Util
{
    /// <summary>
    ///     Lookup the steam folder util class
    /// </summary>
    public static class SteamFolderLookup
    {
        public static string GetGameFolderByRelativePath(string relativePath)
        {
            foreach (var folder in TryToGetGameInstallationFolders())
            {
                var posible = Path.Combine(folder.Replace("\\\\", "\\"), "steamapps\\common", relativePath);
                if (Directory.Exists(posible))
                    return posible;
            }

            return string.Empty;
        }

        #region Private Methods

        private static string[] TryToGetGameInstallationFolders()
        {
            try
            {
                var key32 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\VALVE\\");
                var key64 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Valve\\");

                var posibles32 = TryToGetGameInstallationFolders(ref key32);
                var posibles64 = TryToGetGameInstallationFolders(ref key64);

                return posibles32.Concat(posibles64).ToArray();
            }
            catch
            {
                // Left blank intentionally
                return [];
            }
        }

        private static List<string> TryToGetGameInstallationFolders(ref RegistryKey key)
        {
            var posibles = new List<string>();

            try
            {
                if (key is null || string.IsNullOrEmpty(key?.ToString() ?? string.Empty))
                    return posibles;

                foreach (var name in key?.GetSubKeyNames() ?? [])
                {
                    using var subkey = key?.OpenSubKey(name);
                    var path = subkey?.GetValue("InstallPath") ?? string.Empty;

                    if (path is null || string.IsNullOrEmpty(path?.ToString() ?? string.Empty))
                        continue;

                    var root = path?.ToString() ?? string.Empty;
                    if (string.IsNullOrEmpty(root))
                        continue;

                    var config = Path.Combine(root, "steamapps/libraryfolders.vdf");
                    if (!File.Exists(config))
                        continue;

                    foreach (var line in File.ReadAllLines(config))
                    {
                        // Look for the part that contains "path"
                        if (line.Contains("path"))
                        {
                            // Extract the path value (after "path" key)
                            posibles.Add(line.Split('"')[3]);
                        }
                    }
                }
            }
            catch
            {
                // Left blank intentionally
            }

            return posibles;
        }

        #endregion
    }
}
