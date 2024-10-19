using Newtonsoft.Json.Linq;

namespace api.nox.network.Auths
{
    [System.Serializable]
    public class LoginRequest
    {
        public string server;
        public string identifier;
        public string password;

        public string ToJSON()
        => new JObject
        {
            ["identifier"] = identifier,
            ["password"] = password
        }.ToString();

    }
}