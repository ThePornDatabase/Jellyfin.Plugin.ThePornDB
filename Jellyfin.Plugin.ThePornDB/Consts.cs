namespace ThePornDB
{
    public static class Consts
    {
        public const string BaseURL = "https://metadataapi.net";

        public const string SceneURL = BaseURL + "/scenes/{0}";

        public const string PerfomerURL = BaseURL + "/performers/{0}";

        public const string APIBaseURL = "https://api.metadataapi.net";

        public const string APISceneSearchURL = APIBaseURL + "/scenes?parse={0}";

        public const string APISceneURL = APIBaseURL + "/scenes/{0}";

        public const string APIPerfomerSearchURL = APIBaseURL + "/performers?q={0}";

        public const string APIPerfomerURL = APIBaseURL + "/performers/{0}";

        public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36";
    }
}
