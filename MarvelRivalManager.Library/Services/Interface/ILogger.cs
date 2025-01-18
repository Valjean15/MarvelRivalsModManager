using MarvelRivalManager.Library.Entities;

namespace MarvelRivalManager.Library.Services.Interface
{
    /// <summary>
    ///     Console logger interface
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        ///     Write a message to the console
        /// </summary>
        void Log(string message, LoggerLevel level = LoggerLevel.Info);
    }
}
