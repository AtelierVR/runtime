using Nox.CCK.Mods;

namespace api.nox.network
{
    public class SearchUserData : ShareObject
    {
        [ShareObjectImport] public string server;
        [ShareObjectImport] public string query;
        [ShareObjectImport] public string[] user_ids;
        [ShareObjectImport] public uint offset;
        [ShareObjectImport] public uint limit;

        public string ToParams()
        {
            var text = "";
            if (query != null) text += (text.Length > 0 ? "&" : "") + $"query={query}";
            if (user_ids != null)
                foreach (var u in user_ids)
                    text += (text.Length > 0 ? "&" : "") + $"id={u}";
            if (offset > 0) text += (text.Length > 0 ? "&" : "") + $"offset={offset}";
            if (limit > 0) text += (text.Length > 0 ? "&" : "") + $"limit={limit}";
            return text;
        }
    }
}