using MarvelRivalManager.Library.Services.Interface;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IEnvironment"/>
    public abstract class Environment : IEnvironment
    {
        /// <see cref="IEnvironment.Load"/>
        public abstract IEnvironment Load();

        /// <see cref="IEnvironment.Folders"/>"/>
        public Folders Folders { get; set; } = new();
    }
}
