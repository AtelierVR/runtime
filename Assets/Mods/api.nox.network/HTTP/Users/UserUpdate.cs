using Newtonsoft.Json.Linq;
using Nox.CCK.Mods;

namespace api.nox.network.Users
{

    [System.Serializable]
    public class UserUpdate
    {
        public string username;
        public string display;
        public string email;
        public string password;
        public string thumbnail;
        public string banner;
        public string[] links;
        public string home;

        public string ToJson()
        {
            var obj = new JObject();
            if (!string.IsNullOrEmpty(username)) obj["username"] = username;
            if (!string.IsNullOrEmpty(display)) obj["display"] = display;
            if (!string.IsNullOrEmpty(email)) obj["email"] = email;
            if (!string.IsNullOrEmpty(password)) obj["password"] = password;
            if (!string.IsNullOrEmpty(thumbnail)) obj["thumbnail"] = thumbnail;
            if (!string.IsNullOrEmpty(banner)) obj["banner"] = banner;
            if (links != null) obj["links"] = JArray.FromObject(links);
            if (!string.IsNullOrEmpty(home)) obj["home"] = home;
            return obj.ToString();
        }


    }
}