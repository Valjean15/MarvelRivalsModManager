using MarvelRivalManager.Library.Services.Interface;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IEnvironment"/>
    public abstract class Environment : IEnvironment
    {
        /// <see cref="IEnvironment.Refresh"/>
        public abstract IEnvironment Refresh();

        /// <see cref="IEnvironment.Update(IEnvironment)"/>
        public abstract void Update(IEnvironment values);

        /// <see cref="IEnvironment.Update(IEnvironment)"/>
        public abstract IEnvironment Default();

        /// <see cref="IEnvironment.Folders"/>"/>
        public Folders Folders { get; set; } = new();

        /// <see cref="IEnvironment.Options"/>"/>
        public Options Options { get; set; } = new();

        /// <see cref="IEnvironment.Variables"/>"/>
        public Dictionary<string, string> Variables { get; set; } = [];
    }
}
