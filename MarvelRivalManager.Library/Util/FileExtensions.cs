using System.Text.Json;

namespace MarvelRivalManager.Library.Util
{
    public static class FileExtensions
    {
        public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static void DeleteFileIfExist(this string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        public static void OverrideFileWith(this string target, string source)
        {
            if (File.Exists(target) && File.Exists(source))
                File.Copy(source, target, true);
        }

        public static string MakeSafeMove(this string first, string second, int tries = 0)
        {
            if (!File.Exists(first))
                return string.Empty;

            var safe = GetSafeName(second);
            File.Move(first, safe, true);
            return safe;
        }

        public static string MakeSafeCopy(this string first, string second, int tries = 0)
        {
            if (!File.Exists(first))
                return string.Empty;

            // Avoid overwriting the file
            var safe = GetSafeName(second);
            File.Copy(first, safe, true);
            return safe;
        }

        public static T? DeserializeFileContent<T>(this string file)
        {
            if (!File.Exists(file))
                return default;

            return JsonSerializer.Deserialize<T>(File.ReadAllText(file), JsonOptions);
        }

        public static void WriteFileContent<T>(this string file, T values)
        {
            File.WriteAllText(file, JsonSerializer.Serialize(values, JsonOptions));
        }

        public static bool ChangeExtensionIfExists(this string file, string extension)
        {
            if (File.Exists(file))
                return false;

            File.Move(file, Path.ChangeExtension(file, extension));
            return true;
        }

        public static void MoveFileIfExists(this string file, string destination)
        {

        }

        private static string GetSafeName(string file, int tries = 0)
        {
            if (!File.Exists(file))
                return file;

            tries++;

            var location = Path.GetDirectoryName(file);
            var name = Path.GetFileNameWithoutExtension(file);
            var extension = Path.GetExtension(file);
            return GetSafeName(Path.Combine(location!, $"{name}-{tries}{extension}"), tries);
        }
    }
}
