using MarvelRivalManager.Library.Services.Implementation;
using MarvelRivalManager.Library.Services.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Configuration
{
    public static class MarvelRivalsManagerConfigurationExtensions
    {
        public static IServiceCollection AddMavelRivalManagerServices(this IServiceCollection service)
        {
            service
                .AddSingleton<IDirectoryCheker, DirectoryCheker>()
                .AddSingleton<IResourcesClient, ResourcesClient>()
                .AddSingleton<IModManager, ModManager>()
                .AddSingleton<IRepack, Repack>()
                .AddSingleton<IPatcher, Patcher>()
                ;

            return service;
        }
    }
}
