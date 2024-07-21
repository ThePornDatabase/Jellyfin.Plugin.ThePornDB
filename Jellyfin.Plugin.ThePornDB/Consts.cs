namespace ThePornDB
{
    public static class Consts
    {
        public const string BaseURL = "https://theporndb.net";

        public const string SceneURL = BaseURL + "/scenes/{0}";

        public const string MovieURL = BaseURL + "/movies/{0}";

        public const string JAVURL = BaseURL + "/jav/{0}";

        public const string PerfomerURL = BaseURL + "/performers/{0}";

        public const string SiteURL = BaseURL + "/sites/{0}";

        public const string APIBaseURL = "https://api.theporndb.net";

        public const string APISceneSearchURL = APIBaseURL + "/scenes?parse={0}&hash={1}&year={2}";

        public const string APIMovieSearchURL = APIBaseURL + "/movies?parse={0}&hash={1}&year={2}";

        public const string APIJAVSearchURL = APIBaseURL + "/jav?parse={0}&hash={1}&year={2}";

        public const string APISceneURL = APIBaseURL + "/scenes/{0}";

        public const string APIMovieURL = APIBaseURL + "/movies/{0}";

        public const string APIJAVURL = APIBaseURL + "/jav/{0}";

        public const string APIPerfomerSearchURL = APIBaseURL + "/performers?q={0}";

        public const string APIPerfomerURL = APIBaseURL + "/performers/{0}";

        public const string APISiteSearchURL = APIBaseURL + "/sites?q={0}";

        public const string APISiteURL = APIBaseURL + "/sites/{0}";

        public static readonly string UserAgent = $"Jellyfin.Plugin.ThePornDB/{Plugin.Instance.Version}";
    }
}
