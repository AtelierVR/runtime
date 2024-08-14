using System;
using Nox.CCK.Mods;

namespace api.nox.network
{
    [Serializable]
    public class User : ShareObject
    {
        [ShareObjectExport] public uint id;
        [ShareObjectExport] public string username;
        [ShareObjectExport] public string display;
        [ShareObjectExport] public string[] tags;
        [ShareObjectExport] public string server;
        [ShareObjectExport] public float rank;
        [ShareObjectExport] public string[] links;
        [ShareObjectExport] public string banner;
        [ShareObjectExport] public string thumbnail;

        /**
         * @brief Check if the user matches the given reference.
         * @param reference The reference to check against.
         * @param default_server The default server to use if the reference does not specify one.
         */
        public bool Match(string reference, string default_server)
        {
            var identifier = UserIdentifier.FromString(reference);
            if (new UserIdentifier(id.ToString(), server).ToMinimalString() == identifier.ToMinimalString(default_server)) return true;
            if (new UserIdentifier(username, server).ToMinimalString() == identifier.ToMinimalString(default_server)) return true;
            return false;
        }

        [ShareObjectExport] public Func<string, string, bool> SharedMarch;

        public void BeforeExport()
        {
            SharedMarch = (reference, default_server) => Match(reference, default_server);
        }

        public void AfterExport()
        {
            SharedMarch = null;
        }
    }
}