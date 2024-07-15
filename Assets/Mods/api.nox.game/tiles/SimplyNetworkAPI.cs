using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.game
{
    public class SimplyNetworkAPI : ShareObject
    {
        [ShareObjectImport] public SimplyInstanceAPI Instance;
        [ShareObjectImport] public SimplyUserAPI User;
        [ShareObjectImport] public SimplyServerAPI Server;
        [ShareObjectImport] public SimplyWorldAPI World;

        [ShareObjectImport] public Func<string, UniTask<Texture2D>> FetchTexture;
        [ShareObjectImport] public Func<ShareObject> GetSharedCurrentUser;
        public SimplyUserMe GetCurrentUser() => GetSharedCurrentUser()?.Convert<SimplyUserMe>();
        [ShareObjectImport] public Func<ShareObject> GetSharedCurrentServer;
        public SimplyServer GetCurrentServer() => GetSharedCurrentServer()?.Convert<SimplyServer>();
    }
}