namespace api.nox.network.Users
{
    public class SearchRequest
    {
        public string server;
        public string query;
        public string[] user_ids;
        public uint offset;
        public uint limit;

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