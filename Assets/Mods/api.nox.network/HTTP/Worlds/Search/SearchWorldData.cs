using Nox.CCK.Mods;

namespace api.nox.network.Worlds
{
    public class SearchWorldData : ShareObject
    {
        public string server;
        public string query;
        public uint[] world_ids;
        public uint offset;
        public uint limit;

        public string ToParams()
        {
            var text = "";
            if (query != null) text += (text.Length > 0 ? "&" : "") + $"query={query}";
            if (world_ids != null)
                foreach (var u in world_ids)
                    text += (text.Length > 0 ? "&" : "") + $"id={u}";
            if (offset > 0) text += (text.Length > 0 ? "&" : "") + $"offset={offset}";
            if (limit > 0) text += (text.Length > 0 ? "&" : "") + $"limit={limit}";
            return text;
        }
    }
}