namespace Nox.Servers
{
    [System.Serializable]
    public class ServerGateways
    {
        public string http;
        public string ws;
        private static string Combine(string basepath, string path)
        {
            if (!basepath.EndsWith("/") && !path.StartsWith("/"))
                return basepath + "/" + path;
            else if (basepath.EndsWith("/") && path.StartsWith("/"))
                return basepath + path[1..];
            else return basepath + path;
        }
        public string CombineHTTP(string pathname) => Combine(http, pathname);
        public string CombineWS(string pathname) => Combine(ws, pathname);
    }
}