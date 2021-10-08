using MediaBrowser.Model.Plugins;

namespace ThePornDB.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            this.MetadataAPIToken = string.Empty;

            this.UseFilePath = true;
            this.UseOSHash = true;

            this.UseCustomTitle = false;
            this.CustomTitle = "{studio}: {title} ({actors})";

            this.UseUnmatchedTag = false;
            this.UnmatchedTag = "Missing From ThePornDB";
        }

        public string MetadataAPIToken { get; set; }

        public bool UseFilePath { get; set; }

        public bool UseOSHash { get; set; }

        public bool UseCustomTitle { get; set; }

        public string CustomTitle { get; set; }

        public bool UseUnmatchedTag { get; set; }

        public string UnmatchedTag { get; set; }
    }
}
