#if __EMBY__
using Emby.Web.GenericEdit;
#else
using MediaBrowser.Model.Plugins;
#endif

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

    public enum CollectionType
    {
        Scene = 0,
        Movie = 1,
        JAV = 2,
        All = 3,
    }

    public enum StudioStyle
    {
        Site = 0,
        Network = 1,
        All = 2,
        Parent = 3,
    }

    public enum ScenesImageStyle
    {
        Poster = 0,
        Background = 1,
    }

    public enum ActorsOverviewStyle
    {
        None = 0,
        Default = 1,
        CustomExtras = 2,
    }

    public enum ActorsRoleStyle
    {
        None = 0,
        Gender = 1,
        NameByScene = 2,
    }

    public enum ActorsImageStyle
    {
        Poster = 0,
        Face = 1,
    }

#if __EMBY__
    public class PluginConfiguration : EditableOptionsBase
    {
#else
    public class PluginConfiguration : BasePluginConfiguration
    {
#endif
        public PluginConfiguration()
        {
            this.MetadataAPIToken = string.Empty;

            this.UseFilePath = false;
            this.UseOSHash = false;

            this.OrderStyle = OrderStyle.Default;
            this.TagStyle = TagStyle.Genre;

            this.AddCollectionOnSite = false;

            this.CollectionMinSize = 0;
            this.AddCollectionToCollections = true;
            this.CollectionType = CollectionType.All;

            this.StudioStyle = StudioStyle.All;

            this.UseCustomTitle = false;
            this.CustomTitle = "{studio}: {title} ({actors})";

            this.UseUnmatchedTag = false;
            this.UnmatchedTag = "Missing From ThePornDB";

            this.DisableMediaAutoIdentify = false;
            this.DisableActorsAutoIdentify = false;
            this.DisableMaleActors = false;
            this.DisableActors = false;
            this.DisableGenres = false;

            this.ScenesImage = ScenesImageStyle.Poster;

            this.AddDisambiguation = true;
            this.ActorsRole = ActorsRoleStyle.Gender;
            this.ActorsImage = ActorsImageStyle.Poster;
            this.ActorsOverview = ActorsOverviewStyle.Default;
            this.ActorsOverviewFormat = "<strong style=\"color:#ff0000\">{measurements}<br/></strong>{cupsize}-{waist}-{hips}<br/>{tattoos}<br/>{piercings}<br/>{bio}";
        }

#if __EMBY__
        public override string EditorTitle => Plugin.Instance.Name;
#endif

        public string MetadataAPIToken { get; set; }

        public bool UseFilePath { get; set; }

        public bool UseOSHash { get; set; }

        public OrderStyle OrderStyle { get; set; }

        public TagStyle TagStyle { get; set; }

        public bool AddCollectionOnSite { get; set; }

        public int CollectionMinSize { get; set; }

        public bool AddCollectionToCollections { get; set; }

        public CollectionType CollectionType { get; set; }

        public StudioStyle StudioStyle { get; set; }

        public bool UseCustomTitle { get; set; }

        public string CustomTitle { get; set; }

        public bool UseUnmatchedTag { get; set; }

        public string UnmatchedTag { get; set; }

        public bool DisableMediaAutoIdentify { get; set; }

        public bool DisableActorsAutoIdentify { get; set; }

        public bool DisableMaleActors { get; set; }

        public bool DisableActors { get; set; }

        public bool DisableGenres { get; set; }

        public ScenesImageStyle ScenesImage { get; set; }

        public bool AddDisambiguation { get; set; }

        public ActorsRoleStyle ActorsRole { get; set; }

        public ActorsImageStyle ActorsImage { get; set; }

        public ActorsOverviewStyle ActorsOverview { get; set; }

        public string ActorsOverviewFormat { get; set; }
    }
}
