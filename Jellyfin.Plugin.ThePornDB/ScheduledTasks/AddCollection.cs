using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using ThePornDB.Helpers;
using ThePornDB.Helpers.Utils;
using ThePornDB.Providers;

namespace ThePornDB.ScheduledTasks
{
    public class AddCollection : IScheduledTask
    {
        private static readonly string ImagesPath = Path.Combine(Plugin.Instance.DataFolderPath, "images");

        private static readonly Dictionary<ImageType, string> Paths = new Dictionary<ImageType, string>
        {
            { ImageType.Primary, Path.Combine(ImagesPath, "posters") },
            { ImageType.Logo, Path.Combine(ImagesPath, "logos") },
            { ImageType.Disc, Path.Combine(ImagesPath, "discs") },
        };

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

            foreach (var path in Paths)
            {
                if (!Directory.Exists(path.Value))
                {
                    Logger.Info($"Creating missing directory \"{path.Value}\"");
                    Directory.CreateDirectory(path.Value);
                }
            }

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

                var images = new List<ItemImageInfo>();
                var supported = new Dictionary<string, ImageType>
                {
                    { "poster", ImageType.Primary },
                    { "logo", ImageType.Logo },
                    { "favicon", ImageType.Disc },
                };

                var siteData = await MetadataAPI.SiteSearch(studio, cancellationToken).ConfigureAwait(false);
                if (siteData != null)
                {
                    foreach (var item in supported)
                    {
                        string poster = (string)siteData.First()[item.Key],
                            startPath = Paths?[item.Value];

                        if (!string.IsNullOrEmpty(poster) && !string.IsNullOrEmpty(startPath))
                        {
                            var filepath = Path.Combine(startPath, studio + Path.GetExtension(poster));

                            if (!File.Exists(filepath))
                            {
                                var http = await HTTP.Request(poster, cancellationToken).ConfigureAwait(false);
                                if (http.IsOK)
                                {
                                    using (var fileStream = File.Create(filepath))
                                    {
                                        http.ContentStream.Seek(0, SeekOrigin.Begin);
                                        http.ContentStream.CopyTo(fileStream);
                                    }
                                }
                            }

                            images.Add(new ItemImageInfo()
                            {
                                Path = filepath,
                                Type = item.Value,
                            });
                        }
                    }
                }

                foreach (var item in supported)
                {
                    var moviesImages = movies.Where(o => o.HasImage(item.Value));
                    if (moviesImages.Any() && !images.Where(o => o.Type == item.Value).Any())
                    {
                        images.Add(moviesImages.Random().GetImageInfo(item.Value, 0));
                    }
                }

                if (images.Any())
                {
                    foreach (var image in images)
                    {
                        collection.SetImage(image, 0);
                    }
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
