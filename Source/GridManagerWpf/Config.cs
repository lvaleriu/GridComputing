using ServiceStack.Configuration;

namespace GridManagerWpf
{
    public class Config
    {
        public Config(AppSettings resourceManager)
        {
            UrlBase = resourceManager.GetString("UrlBase");
        }

        public string UrlBase { get; private set; }
    }
}