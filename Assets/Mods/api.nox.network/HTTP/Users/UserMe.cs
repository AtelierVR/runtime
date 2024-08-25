using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.network
{
    [Serializable]
    public class UserMe : ShareObject
    {
        internal NetworkSystem netSystem;
        [ShareObjectExport] public uint id;
        [ShareObjectExport] public string username;
        [ShareObjectExport] public string display;
        [ShareObjectExport] public string[] tags;
        [ShareObjectExport] public string server;
        [ShareObjectExport] public float rank;
        [ShareObjectExport] public string[] links;
        [ShareObjectExport] public string banner;
        [ShareObjectExport] public string thumbnail;
        [ShareObjectExport] public string email;
        [ShareObjectExport] public uint createdAt;
        [ShareObjectExport] public string home;

        /**
         * @brief Check if the user matches the given reference.
         * @param reference The reference to check against.
         * @param default_server The default server to use if the reference does not specify one.
         */
        public bool MatchRef(string reference, string default_server)
        {
            var identifier = UserIdentifier.FromString(reference);
            if (new UserIdentifier(id.ToString(), server).ToMinimalString() == identifier.ToMinimalString(default_server)) return true;
            if (new UserIdentifier(username, server).ToMinimalString() == identifier.ToMinimalString(default_server)) return true;
            return false;
        }

        /**
         * @brief Get the user's home world.
         */
        public async UniTask<World> GetHome()
        {
            if (string.IsNullOrEmpty(home)) return null;
            var worldref = WorldIdentifier.FromString(home);
            Debug.Log($"GetHome {netSystem}.");
            return await netSystem._world.GetWorld(worldref.server ?? server, worldref.id);
        } 

        [ShareObjectExport] public Func<string, string, bool> SharedMatchRef;
        [ShareObjectExport] public Func<UniTask<ShareObject>> SharedGetHome;

        public void BeforeExport()
        {
            SharedMatchRef = (reference, default_server) => MatchRef(reference, default_server);
            SharedGetHome = async () => await GetHome();
        }

        public void AfterExport()
        {
            SharedMatchRef = null;
            SharedGetHome = null;
        }
    }
}