using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using ThePornDB.Providers;
using MediaBrowser.Model.Entities;

#if __EMBY__
#else
using MediaBrowser.Controller.Entities.Movies;
#endif

namespace ThePornDB.ScheduledTasks
{
    public class ActorCollection : IScheduledTask
    {
        private readonly ILibraryManager libraryManager;

        private readonly ICollectionManager collectionManager;

        public ActorCollection(ILibraryManager libraryManager, ICollectionManager collectionManager)
        {
            this.libraryManager = libraryManager;
            this.collectionManager = collectionManager;
        }

        public string Key => Plugin.Instance.Name + "ActorCollection";

        public string Name => "Add Collection of Actors";

        public string Description => "Creates Collection of Actors for every Tag";

        public string Category => Plugin.Instance.Name;

#if __EMBY__
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
#else
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
#endif
        {
            await Task.Yield();
            progress?.Report(0);


            var items = this.libraryManager.GetPeopleItems(new InternalPeopleQuery()).Where(p => p.ProviderIds.ContainsKey(Plugin.Instance.Name));


            var tags = items.SelectMany(o => o.Tags).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var (idx, tag) in tags.WithIndex())
            {
                progress?.Report((double)idx / tags.Count * 100);

                var actors = items.Where(o => o.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase) && !o.Name.Equals(tag));


                await this.CreateCollection(actors, tag).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }

            progress?.Report(100);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerWeekly, DayOfWeek = DayOfWeek.Sunday, TimeOfDayTicks = TimeSpan.FromHours(12).Ticks };
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
