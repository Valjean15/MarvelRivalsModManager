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
                .AddSingleton<IGameSettings, GameSettings>()
                .AddSingleton<IModDataAccess, ModDataAccess>()
                .AddSingleton<IResourcesClient, ResourcesClient>()
                .AddSingleton<IModManager, ModManager>()
                .AddSingleton<IProfileManager, ProfileManager>()
                .AddSingleton<IRepack, Repack>()
                .AddSingleton<IPatcher, Patcher>()
                ;

            return service;
        }
    }
}
