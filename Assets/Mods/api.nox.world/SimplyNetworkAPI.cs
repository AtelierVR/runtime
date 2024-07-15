using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.world
{
    public class SimplyNetworkAPI : ShareObject
    {
        public Func<string, UniTask<Texture2D>> FetchTexture { get; set; }
        public Func<SimplyUserMe> GetCurrentUser { get; set; }
    }
}