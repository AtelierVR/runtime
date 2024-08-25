using Nox.CCK.Mods;

namespace api.nox.network
{
    public class SearchInstanceData : ShareObject
    {
        [ShareObjectImport] public string server;
        [ShareObjectImport] public string query;
        [ShareObjectImport] public string world;
        [ShareObjectImport] public string owner;
        [ShareObjectImport] public uint offset;
        [ShareObjectImport] public uint limit;

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