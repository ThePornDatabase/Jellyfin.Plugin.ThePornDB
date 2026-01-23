using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using ThePornDB.Providers;

#if __EMBY__
#else
using Jellyfin.Data.Enums;
#endif

namespace ThePornDB.ScheduledTasks
{
    public class AddCollection : IScheduledTask
    {
        private readonly ILibraryManager libraryManager;

        private readonly ICollectionManager collectionManager;

        public AddCollection(ILibraryManager libraryManager, ICollectionManager collectionManager)
        {
            this.libraryManager = libraryManager;
            this.collectionManager = collectionManager;
        }

        public string Key => Plugin.Instance.Name + "AddCollection";

        public string Name => "Add Collection";

        public string Description => "Creates Collection for every scene";

        public string Category => Plugin.Instance.Name;

#if __EMBY__
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
#else
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
#endif
        {
            await Task.Yield();
            progress?.Report(0);

            var allMovies = this.libraryManager.GetItemList(new InternalItemsQuery()
            {
#if __EMBY__
                IncludeItemTypes = new[] { nameof(Movie) },
#else
                IncludeItemTypes = [BaseItemKind.Movie],
#endif
            });
            var items = allMovies.Where(o => o.ProviderIds.ContainsKey(Plugin.Instance.Name));

            if (Plugin.Instance.Configuration.CollectionType != Configuration.CollectionType.All)
            {
                var (prefixID, _, _) = Base.GetSettings((SceneType)Plugin.Instance.Configuration.CollectionType);
                items = items.Where(o => o.ProviderIds[Plugin.Instance.Name].StartsWith(prefixID, StringComparison.OrdinalIgnoreCase));
            }

            if (Plugin.Instance.Configuration.UseUnmatchedTag)
            {
                var missingScenes = allMovies.Where(o => o.Genres.Contains(Plugin.Instance.Configuration.UnmatchedTag, StringComparer.Ordinal));
                await this.CreateCollection(missingScenes, Plugin.Instance.Configuration.UnmatchedTag).ConfigureAwait(false);
            }

            var studios = items.SelectMany(o => o.Studios).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var (idx, studio) in studios.WithIndex())
            {
                progress?.Report((double)idx / studios.Count * 100);

                var movies = items.Where(o => o.Studios.Contains(studio, StringComparer.OrdinalIgnoreCase) && !o.Name.Equals(studio));
                if (movies.Count() < Plugin.Instance.Configuration.CollectionMinSize)
                {
                    continue;
                }

                if (!Plugin.Instance.Configuration.AddCollectionToCollections)
                {
                    movies = movies.Where(o => !(o is BoxSet));
                }

                await this.CreateCollection(movies, studio).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }

            progress?.Report(100);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo
            {
#if __EMBY__
                Type = TaskTriggerInfo.TriggerWeekly,
#else
                Type = TaskTriggerInfoType.WeeklyTrigger,
#endif
                DayOfWeek = DayOfWeek.Sunday,
                TimeOfDayTicks = TimeSpan.FromHours(12).Ticks,
            };
        }

        private async Task<BoxSet> CreateCollection(IEnumerable<BaseItem> items, string name)
        {
            var option = new CollectionCreationOptions
            {
                Name = name,
#if __EMBY__
                ItemIdList = items.Select(o => o.InternalId).ToArray(),
#else
                ItemIdList = items.Select(o => o.Id.ToString()).ToArray(),
#endif
            };

#if __EMBY__
            var collection = await this.collectionManager.CreateCollection(option).ConfigureAwait(false);
#else
            var collection = await this.collectionManager.CreateCollectionAsync(option).ConfigureAwait(false);
#endif

            return collection;
        }
    }
}
