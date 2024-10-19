namespace api.nox.network.Worlds.Assets
{
    public class SearchRequest
    {
        public string server;
        public uint world_id;
        public uint offset;
        public uint limit;
        public string[] platforms;
        public string[] engines;
        public ushort[] versions;
        public bool with_empty;

        public string ToParams()
        {
            string text = "";
            if (offset > 0) text += (text.Length > 0 ? "&" : "") + $"offset={offset}";
            if (limit > 0) text += (text.Length > 0 ? "&" : "") + $"limit={limit}";
            if (platforms != null)
                foreach (var platform in platforms)
                    text += (text.Length > 0 ? "&" : "") + $"platform={platform}";
            if (engines != null)
                foreach (var engine in engines)
                    text += (text.Length > 0 ? "&" : "") + $"engine={engine}";
            if (versions != null)
                foreach (var version in versions)
                    text += (text.Length > 0 ? "&" : "") + $"version={version}";
            if (with_empty) text += "&empty=true";
            return text;
        }
    }
}