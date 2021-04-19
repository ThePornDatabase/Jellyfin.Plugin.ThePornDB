using MediaBrowser.Model.Plugins;

namespace ThePornDB.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            this.MetadataAPIToken = string.Empty;
        }

        public string MetadataAPIToken { get; set; }
    }
}
