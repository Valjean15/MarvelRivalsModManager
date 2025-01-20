using MarvelRivalManager.Library.Entities;

namespace MarvelRivalManager.Library.Services.Interface
{
    /// <summary>
    ///     Service dedicated to retrieve resources for the app
    /// </summary>
    public interface IResourcesClient
    {
        /// <summary>
        ///     Download a resource
        /// </summary>
        public ValueTask<bool> Download(KindOfMod kind, Action<string> informer);

        /// <summary>
        ///     Download a resource for unpacker
        /// </summary>
        public ValueTask<bool> Unpacker(Action<string> informer);
    }
}
