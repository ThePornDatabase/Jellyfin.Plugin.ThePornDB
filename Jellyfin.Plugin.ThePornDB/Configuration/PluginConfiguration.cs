using MediaBrowser.Model.Plugins;

namespace ThePornDB.Configuration
{
    public enum OrderStyle
    {
        Default = 0,
        DistanceByTitle = 1,
    }

    public enum TagStyle
    {
        Genre = 0,
        Tag = 1,
        Disabled = 2,
    }

    public enum StudioStyle
    {
        Site = 0,
        Network = 1,
        Both = 2,
    }

    public enum ActorsOverviewStyle
    {
        None = 0,
        Default = 1,
        CustomExtras = 2,
    }

    public enum ActorsImageStyle
    {
        Poster = 0,
        Face = 1,
    }

    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            this.MetadataAPIToken = string.Empty;

            this.UseFilePath = true;
            this.UseOSHash = true;

            this.OrderStyle = OrderStyle.Default;
            this.TagStyle = TagStyle.Genre;

            this.AddCollectionToCollections = true;
            this.StudioStyle = StudioStyle.Both;

            this.UseCustomTitle = false;
            this.CustomTitle = "{studio}: {title} ({actors})";

            this.UseUnmatchedTag = false;
            this.UnmatchedTag = "Missing From ThePornDB";

            this.DisableMediaAutoIdentify = false;
            this.DisableActorsAutoIdentify = false;

            this.ActorsImage = ActorsImageStyle.Poster;
            this.ActorsOverview = ActorsOverviewStyle.Default;
            this.ActorsOverviewFormat = "<strong style=\"color:#ff0000\">{measurements}<br/></strong>{cupsize}-{waist}-{hips}<br/>{tattoos}<br/>{piercings}<br/>{bio}";
        }

        public string MetadataAPIToken { get; set; }

        public bool UseFilePath { get; set; }

        public bool UseOSHash { get; set; }

        public OrderStyle OrderStyle { get; set; }

        public TagStyle TagStyle { get; set; }

        public bool AddCollectionToCollections { get; set; }

        public StudioStyle StudioStyle { get; set; }

        public bool UseCustomTitle { get; set; }

        public string CustomTitle { get; set; }

        public bool UseUnmatchedTag { get; set; }

        public string UnmatchedTag { get; set; }

        public bool DisableMediaAutoIdentify { get; set; }

        public bool DisableActorsAutoIdentify { get; set; }

        public ActorsImageStyle ActorsImage { get; set; }

        public ActorsOverviewStyle ActorsOverview { get; set; }

        public string ActorsOverviewFormat { get; set; }
    }
}
