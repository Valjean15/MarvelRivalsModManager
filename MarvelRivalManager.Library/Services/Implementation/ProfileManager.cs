using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;

using System.Collections.Concurrent;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IProfileManager"/>
    internal class ProfileManager(IEnvironment configuration, IModDataAccess query, IModManager manager) : IProfileManager
    {
        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        private readonly IModDataAccess Query = query;
        private readonly IModManager Manager = manager;
        #endregion

        #region Fields
        private Profile[]? Cache = null;
        #endregion

        /// <see cref="IProfileManager.All(bool)"/>
        public async ValueTask<Profile[]> All(bool reload = false)
        {
            ValidateConfiguration();

            if (Cache is null || reload)
            {
                var files = await Task.Run(() => Directory.GetFiles(Configuration.Folders.Collections, "*.json"));
                var profiles = new ConcurrentBag<Profile>();
                Parallel.ForEach(files, file => { profiles.Add(new Profile(file)); });

                Cache = [.. profiles];
            }

            return Cache;
        }

        /// <see cref="IProfileManager.Create(string, Mod[])"/>
        public async ValueTask<Profile> Create(string name, Mod[] mods)
        {
            ValidateConfiguration();

            // Lookup for a file name that does not exist
            var index = 0;
            var file = Path.Combine(Configuration.Folders.Collections, $"{name}.json");
            while (File.Exists(file))
            {
                index++;
                file = Path.Combine(Configuration.Folders.Collections, $"{name}-{index}.json");
            }

            // Create the profile
            var profile = new Profile(file);
            profile.Metadata.Name = name;
            profile.Metadata.Selected = mods?.Select(mod => mod.File.Filename).ToArray() ?? [];

            // Save the profile
            profile.Update();

            await All(true);

            return profile;
        }

        /// <see cref="IProfileManager.Delete(Profile)"/>
        public async ValueTask Delete(Profile profile)
        {
            ValidateConfiguration();

            if (profile is null)
                return;

            profile.Filepath.DeleteFileIfExist();
            await All(true);
        }

        /// <see cref="IProfileManager.GetCurrent"/>
        public async ValueTask<Profile> GetCurrent()
        {
            ValidateConfiguration();

            var profiles = await All();
            var current = profiles.FirstOrDefault(profile => profile.Metadata.Active);

            // Select the first if no active profile found
            if (current is null)
            {
                current = profiles.FirstOrDefault();
                if (current is not null)
                    await Load(current);
            }
            
            // Create a default profile if any profile was not found
            if (current is null)
            {
                var enabled = (await Query.All(true)).Where(mod => mod.Metadata.Enabled).ToArray();
                current = await Create("default", enabled);
                await Load(current);
            }
            
            return current;
        }

        /// <see cref="IProfileManager.Load(Profile)"/>
        public async ValueTask Load(Profile profile)
        {
            ValidateConfiguration();

            var all = await Query.All(true);

            if (Configuration.Options.UseSingleThread)
            {
                foreach (var mod in all)
                {
                    mod.Metadata.Enabled = profile.Metadata.Selected.Contains(mod.File.Filename);
                    await Manager.Update(mod);
                }
            }
            else
            {
                await Parallel.ForEachAsync(all, async (mod, token) =>
                {
                    mod.Metadata.Enabled = profile.Metadata.Selected.Contains(mod.File.Filename);
                    await Manager.Update(mod);
                });
            }

            profile.Metadata.Active = true;
            profile.Update();
        }

        /// <see cref="IProfileManager.Update(Profile)"/>
        public async ValueTask<Profile> Update(Profile profile)
        {
            ValidateConfiguration();
            profile.Update();

            await All(true);

            return profile;
        }

        #region Private Methods

        /// <summary>
        ///     Validate the folder structure for the mods folder
        /// </summary>
        private void ValidateConfiguration()
        {
            // No collection folder on environment
            if (string.IsNullOrEmpty(Configuration.Folders.Collections))
                return;

            Configuration.Folders.Collections.CreateDirectoryIfNotExist();
        }

        #endregion
    }
}
