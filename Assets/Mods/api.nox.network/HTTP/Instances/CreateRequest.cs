using Newtonsoft.Json.Linq;
namespace api.nox.network.Instances
{
    public class CreateRequest
    {
        public string server;

        public string name;
        public string title;
        public string description;
        public string expose;
        public string world;
        public ushort capacity;
        public bool use_password;
        public string password;
        public bool use_whitelist;
        public string[] whitelist;
        public string thumbnail;

        internal string ToJSON()
        {
            var obj = new JObject();
            if (!string.IsNullOrEmpty(name)) obj["name"] = name;
            if (!string.IsNullOrEmpty(title)) obj["title"] = title;
            if (!string.IsNullOrEmpty(description)) obj["description"] = description;
            if (!string.IsNullOrEmpty(thumbnail)) obj["thumbnail"] = thumbnail;
            obj["expose"] = expose;
            obj["world"] = world;
            obj["capacity"] = capacity;
            obj["use_password"] = use_password;
            if (string.IsNullOrEmpty(password)) obj["password"] = password;
            obj["use_whitelist"] = use_whitelist;
            if (whitelist != null) obj["whitelist"] = JArray.FromObject(whitelist);
            return obj.ToString();
        }
    }
}