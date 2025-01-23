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
        public ValueTask<bool> Delete(Action<string, bool> informer);

        /// <summary>
        ///     Download a resource for repacker
        /// </summary>
        public ValueTask<bool> Download(Action<string, bool> informer, CancellationToken? cancellationToken = null);
    }
}
