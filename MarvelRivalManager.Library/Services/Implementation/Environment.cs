using MarvelRivalManager.Library.Services.Interface;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IEnvironment"/>
    public abstract class Environment : IEnvironment
    {
        /// <see cref="IEnvironment.Refresh"/>
        public abstract IEnvironment Refresh();

        /// <see cref="IEnvironment.Folders"/>"/>
        public Folders Folders { get; set; } = new();

        /// <see cref="IEnvironment.Options"/>"/>
        public Options Options { get; set; } = new();
    }
}
