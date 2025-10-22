using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;

#if __EMBY__
using MediaBrowser.Controller.Entities.Movies;
#else
using Jellyfin.Data.Enums;
#endif

namespace ThePornDB.ScheduledTasks
{
    public class Cleanup : IScheduledTask
    {
        private readonly ILibraryManager libraryManager;

        public Cleanup(ILibraryManager libraryManager)
        {
            this.libraryManager = libraryManager;
        }

        public string Key => Plugin.Instance.Name + "Cleanup";

        public string Name => "Cleanup";

        public string Description => "Cleanup ids";

        public string Category => Plugin.Instance.Name;

#if __EMBY__
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
#else
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
#endif
        {
            await Task.Yield();
            progress?.Report(0);

            var items = this.libraryManager.GetItemList(new InternalItemsQuery()
            {
#if __EMBY__
                IncludeItemTypes = new[] { nameof(Movie) },
#else
                IncludeItemTypes = [BaseItemKind.Movie],
#endif
            }).Where(o => o.ProviderIds.ContainsKey(Plugin.Instance.Name));

            foreach (var (idx, item) in items.WithIndex())
            {
                var newItem = item;
                newItem.ProviderIds[Plugin.Instance.Name] = item.ProviderIds[Plugin.Instance.Name].Split('?', StringSplitOptions.RemoveEmptyEntries).First();

#if __EMBY__
                this.libraryManager.UpdateItem(item, newItem, ItemUpdateType.MetadataEdit);
#else
                await this.libraryManager.UpdateItemAsync(item, newItem, ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
#endif

                progress?.Report((double)idx / items.Count() * 100);
            }

            progress?.Report(100);
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return Enumerable.Empty<TaskTriggerInfo>();
        }
    }
}
