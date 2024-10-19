namespace api.nox.network.Instances
{
    public class SearchRequest
    {
        public string server;
        public string query;
        public string world;
        public string owner;
        public uint offset;
        public uint limit;

        public string ToParams()
        {
            var text = "";
            if (query != null) text += (text.Length > 0 ? "&" : "") + $"query={query}";
            if (world != null) text += (text.Length > 0 ? "&" : "") + $"world={world}";
            if (owner != null) text += (text.Length > 0 ? "&" : "") + $"owner={owner}";
            if (offset > 0) text += (text.Length > 0 ? "&" : "") + $"offset={offset}";
            if (limit > 0) text += (text.Length > 0 ? "&" : "") + $"limit={limit}";
            return text;
        }
    }
}