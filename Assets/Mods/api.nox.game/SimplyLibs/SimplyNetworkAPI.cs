using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using UnityEngine;
using UnityEngine.Networking;

namespace Nox.SimplyLibs
{
    public class SimplyNetworkAPI : ShareObject
    {
        [ShareObjectImport] public SimplyInstanceAPI Instance;
        [ShareObjectImport] public SimplyUserAPI User;
        [ShareObjectImport] public SimplyServerAPI Server;
        [ShareObjectImport] public SimplyWorldAPI World;
        [ShareObjectImport] public SimplyRelayAPI Relay;

        [ShareObjectImport] public Func<string, UnityWebRequest, UniTask<Texture2D>> SharedFetchTexture;
        [ShareObjectImport] public Func<string, string, UnityWebRequest, UniTask<string>> SharedDownloadFile;

        public async UniTask<Texture2D> FetchTexture(string url, UnityWebRequest req = null) => await SharedFetchTexture(url, req);
        public async UniTask<string> DownloadFile(string url, string hash, UnityWebRequest req = null) => await SharedDownloadFile(url, hash, req);
        [ShareObjectImport] public Func<ShareObject> GetSharedCurrentUser;
        public SimplyUserMe GetCurrentUser() => GetSharedCurrentUser()?.Convert<SimplyUserMe>();
        [ShareObjectImport] public Func<ShareObject> GetSharedCurrentServer;
        public SimplyServer GetCurrentServer() => GetSharedCurrentServer()?.Convert<SimplyServer>();
    }
}