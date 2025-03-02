using System.Collections.Generic;
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


        public static UserUpdate From(Dictionary<string, object> data)
        {
            var update = new UserUpdate();
            if (data.TryGetValue("username", out var username) && username is string s) update.username = s;
            if (data.TryGetValue("display", out var display) && display is string s2) update.display = s2;
            if (data.TryGetValue("email", out var email) && email is string s3) update.email = s3;
            if (data.TryGetValue("password", out var password) && password is string s4) update.password = s4;
            if (data.TryGetValue("thumbnail", out var thumbnail) && thumbnail is string s5) update.thumbnail = s5;
            if (data.TryGetValue("banner", out var banner) && banner is string s6) update.banner = s6;
            if (data.TryGetValue("links", out var links) && links is string[] s7) update.links = s7;
            if (data.TryGetValue("home", out var home) && home is string s8) update.home = s8;
            return update;
        }
    }
}