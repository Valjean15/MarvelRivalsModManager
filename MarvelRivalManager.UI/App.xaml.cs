using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.UI.Configuration;
using MarvelRivalManager.UI.Helper;
using MarvelRivalManager.UI.Pages;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

using System;

namespace MarvelRivalManager.UI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        #region Private fields

        private static Window? m_window;
        private static Win32WindowHelper? m_win32WindowHelper;

        #endregion

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            InitializeServices();
        }

        /// <summary>
        ///     Initialize service collections of services using DI
        /// </summary>
        private static void InitializeServices()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build()
                ;

            // - Register app configuration
            // - Register app services
            // - Register app views that require DI
            _ = new Services(new ServiceCollection()
                .AddSingleton<IEnvironment, AppEnvironment>(provider => configuration.Get<AppEnvironment>() ?? new())
                .AddMavelRivalManagerServices()
                .AddSingleton<Home>()
                .AddTransient<ModManager>()
                .AddTransient<Settings>()
                .BuildServiceProvider()
            );
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            m_window = Services.Get<Home>();
            m_window?.Activate();

            // Default size of the window
            if (m_window is not null)
            {
                m_window.AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));
                m_window.ExtendsContentIntoTitleBar = true;

                WindowHelper.TrackWindow(m_window);

                m_win32WindowHelper = new Win32WindowHelper(m_window);
                m_win32WindowHelper.SetWindowMinMaxSize(new Win32WindowHelper.POINT() { x = 1000, y = 800 });
            }
        }
    }

    /// <summary>
    ///     Static class to expose the service provider
    /// </summary>
    public class Services
    {
        private static IServiceProvider? m_provider;
        public static IServiceProvider Provider => m_provider ?? throw new ApplicationException("Cannot initialize services");
        public static TService Get<TService>() => Provider.GetService<TService>() ?? throw new ApplicationException("Service not found");
        public Services(IServiceProvider provider) => m_provider = provider;
    }
}
