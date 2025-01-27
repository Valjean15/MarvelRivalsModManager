using static MarvelRivalManager.Library.Entities.Delegates;

namespace MarvelRivalManager.Library.Services.Interface
{
    /// <summary>
    ///     Service dedicated to retrieve resources for the app
    /// </summary>
    public interface IResourcesClient
    {
        /// <summary>
         ///     Delete the resource for repacker
         /// </summary>
        public ValueTask<bool> Delete(PrintAndUndo informer);

        /// <summary>
        ///     Download a resource for repacker
        /// </summary>
        public ValueTask<bool> Download(PrintAndUndo informer, CancellationToken? cancellationToken = null);
    }
}
