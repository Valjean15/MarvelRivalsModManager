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
                .AddTransient<IModManager, ModManager>()
                .AddTransient<IUnpacker, Unpacker>()
                .AddTransient<IPatcher, Patcher>()
                ;

            return service;
        }
    }
}
