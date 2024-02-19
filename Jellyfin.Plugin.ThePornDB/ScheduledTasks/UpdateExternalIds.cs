using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;

#if __EMBY__
#else
#endif

namespace ThePornDB.ScheduledTasks
{
    public class UpdateExternalIds : IScheduledTask
    {
        private readonly ILibraryManager libraryManager;

        public UpdateExternalIds(ILibraryManager libraryManager)
        {
            this.libraryManager = libraryManager;
        }

        public string Key => Plugin.Instance.Name + "UpdateExternalIds";

        public string Name => "Update ExternalIds";

        public string Description => "Update ExternalIds to new format";

        public string Category => Plugin.Instance.Name;

#if __EMBY__
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
#else
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
#endif
        {
            await Task.Yield();
            progress?.Report(0);

            var scenes = this.libraryManager.GetItemList(new InternalItemsQuery()).Where(o => o.ProviderIds.ContainsKey(Plugin.Instance.Name));
            foreach (var scene in scenes)
            {
                scene.ProviderIds.TryGetValue(Plugin.Instance.Name, out var curID);
                if (!string.IsNullOrEmpty(curID) && !curID.StartsWith("scenes/", StringComparison.OrdinalIgnoreCase))
                {
                    var newScene = scene;
                    newScene.ProviderIds[Plugin.Instance.Name] = $"scenes/{curID}";

#if __EMBY__
                    this.libraryManager.UpdateItem(scene, newScene, ItemUpdateType.MetadataEdit);
#else
                    await this.libraryManager.UpdateItemAsync(scene, newScene, ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
#endif
                }
            }

            progress?.Report(33);

            var movies = this.libraryManager.GetItemList(new InternalItemsQuery()).Where(o => o.ProviderIds.ContainsKey(Plugin.Instance.Name + "Movie"));
            foreach (var movie in movies)
            {
                movie.ProviderIds.TryGetValue(Plugin.Instance.Name + "Movie", out var curID);
                if (!string.IsNullOrEmpty(curID) && !curID.StartsWith("movies/", StringComparison.OrdinalIgnoreCase))
                {
                    var newMovie = movie;
                    newMovie.ProviderIds[Plugin.Instance.Name] = $"movies/{curID}";

#if __EMBY__
                    this.libraryManager.UpdateItem(movie, newMovie, ItemUpdateType.MetadataEdit);
#else
                    await this.libraryManager.UpdateItemAsync(movie, newMovie, ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
#endif
                }
            }

            progress?.Report(66);

            var javs = this.libraryManager.GetItemList(new InternalItemsQuery()).Where(o => o.ProviderIds.ContainsKey(Plugin.Instance.Name + "JAV"));
            foreach (var jav in javs)
            {
                jav.ProviderIds.TryGetValue(Plugin.Instance.Name + "JAV", out var curID);
                if (!string.IsNullOrEmpty(curID) && !curID.StartsWith("jav/", StringComparison.OrdinalIgnoreCase))
                {
                    var newJav = jav;
                    newJav.ProviderIds[Plugin.Instance.Name] = $"jav/{curID}";

#if __EMBY__
                    this.libraryManager.UpdateItem(jav, newJav, ItemUpdateType.MetadataEdit);
#else
                    await this.libraryManager.UpdateItemAsync(jav, newJav, ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
#endif
                }
            }

            progress?.Report(100);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return Enumerable.Empty<TaskTriggerInfo>();
        }
    }
}
