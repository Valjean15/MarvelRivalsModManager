namespace MarvelRivalManager.Library.Util
{
    internal static class LogFormatter
    {
        public static string AsLog(this string message, string action)
        {
            var actionFormatted = string.Join("", action.Take(10)).PadRight(5);
            return $"[{DateTime.Now:HH:mm:ss}] {actionFormatted} | {message}";
        }
    }
}
