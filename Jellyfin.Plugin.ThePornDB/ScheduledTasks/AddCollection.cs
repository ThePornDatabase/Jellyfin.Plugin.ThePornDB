using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;

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

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            await Task.Yield();
            progress?.Report(0);

            var items = this.libraryManager.GetItemList(new InternalItemsQuery()).Where(o => o.ProviderIds.ContainsKey(Plugin.Instance.Name));

            var studios = items.SelectMany(o => o.Studios).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var (idx, studio) in studios.WithIndex())
            {
                progress?.Report((double)idx / studios.Count * 100);

                var movies = items.Where(o => o.Studios.Contains(studio, StringComparer.OrdinalIgnoreCase));
                var option = new CollectionCreationOptions
                {
                    Name = studio,
#if __EMBY__
                    ItemIdList = movies.Select(o => o.InternalId).ToArray(),
#else
                    ItemIdList = movies.Select(o => o.Id.ToString()).ToArray(),
#endif
                };

#if __EMBY__
                var collection = this.collectionManager.CreateCollection(option);
#else
                var collection = await this.collectionManager.CreateCollectionAsync(option).ConfigureAwait(false);
#endif

                var moviesImages = movies.Where(o => o.HasImage(ImageType.Primary));
                if (moviesImages.Any())
                {
                    collection.SetImage(moviesImages.Random().GetImageInfo(ImageType.Primary, 0), 0);
                }

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
    }
}
