using MarvelRivalManager.Library.Services.Interface;

namespace MarvelRivalManager.Library.Util
{
    public static class DirectoryExtensions
    {
        public async static ValueTask MergeDirectoryAsync(this string sourceDir, string destinationDir, bool dooverride = true)
        {
            await Task.Run(() => MergeDirectory(sourceDir, destinationDir, dooverride));
        }
        public static void MergeDirectory(this string sourceDir, string destinationDir, bool dooverride = true)
        {
            if (!Directory.Exists(sourceDir) || !Directory.Exists(destinationDir))
                return;

            // Get all files in the source directory
            string[] files = Directory.GetFiles(sourceDir);

            foreach (var file in files)
            {
                try
                {
                    // Get the file name (without path)
                    string fileName = Path.GetFileName(file);

                    // Construct the destination file path
                    string destFile = Path.Combine(destinationDir, fileName);

                    if (dooverride)
                    {
                        // Copy the file to the destination folder
                        File.Copy(file, destFile, true);
                    }
                    else
                    {
                        // Copy the file to the destination folder if it doesn't exist
                        if (!File.Exists(destFile))
                        {
                            File.Copy(file, destFile);
                        }
                    }
                }
                catch
                {

                }
            }

            // Get all subdirectories in the source directory
            string[] subDirs = Directory.GetDirectories(sourceDir);

            foreach (var subDir in subDirs)
            {
                try
                {
                    // Construct the destination subdirectory path
                    string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));

                    // Create the subdirectory in the destination if it doesn't exist
                    if (!Directory.Exists(destSubDir))
                    {
                        Directory.CreateDirectory(destSubDir);
                    }

                    // Recursively merge the subdirectory
                    MergeDirectory(subDir, destSubDir, dooverride);
                }
                catch
                {

                }
            }
        }
        public static List<string> GetAllFilesFromDirectory(this string path, List<string>? files = null)
        {
            files ??= [];

            if (!Directory.Exists(path))
                return [];

            files.AddRange(Directory.GetFiles(path) ?? []);
            string[] subDirs = Directory.GetDirectories(path);
            foreach (var dir in subDirs)
            {
                GetAllFilesFromDirectory(dir, files);
            }

            return files;
        }

        public static bool DirectoryContainsSubfolders(this string path, string[] lookup)
        {
            if (!Directory.Exists(path))
                return false;

            return lookup.Aggregate(true, (acc, item) =>
            {
                if (!path.DirectoryContainsSubfolder(item))
                    return false;

                return acc;
            });
        }

        public static bool DirectoryContainsSubfolder(this string path, string lookup)
        {
            if (!Directory.Exists(path))
                return false;

            string[] subDirs = Directory.GetDirectories(path, lookup, SearchOption.AllDirectories) ?? [];
            return subDirs.Length > 0;
        }

        public static void CreateDirectoryIfNotExist(this string path)
        {
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        public static void DeleteDirectoryIfExists(this string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        public static void DeleteDirectoryContent(this string path)
        {
            if (!Directory.Exists(path))
                return;

            string[] files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                File.Delete(file);
            }

            string[] subDirs = Directory.GetDirectories(path);
            foreach (var dir in subDirs)
            {
                dir.DeleteDirectoryIfExists();
            }
        }
    }
}