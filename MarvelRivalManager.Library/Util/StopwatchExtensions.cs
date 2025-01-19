using System.Diagnostics;

namespace MarvelRivalManager.Library.Util
{
    /// <summary>
    ///     Extensions related to stopwatch
    /// </summary>
    public static class StopwatchExtensions
    {
        /// <summary>
        ///     Transforms milliseconds into a human readable time
        /// </summary>
        public static string GetHumaneElapsedTime(this Stopwatch time)
        {
            var milliseconds = time.ElapsedMilliseconds;

            if (milliseconds < 1000)
                return $"{milliseconds} ms";

            var builder = new List<string>();
            var seconds = Math.Round((decimal)milliseconds / 1000, MidpointRounding.ToZero);
            var minutes = Math.Round(seconds / 60, MidpointRounding.ToZero);

            if (minutes > 0)
            {
                builder.Add($"{minutes} min");
                seconds -= minutes * 60;
            }

            if (seconds > 0)
            {
                builder.Add($"{seconds} sec");
            }

            return string.Join(" ", builder);
        }
    }
}
