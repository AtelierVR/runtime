using api.nox.network.Relays;
using api.nox.network.Utils;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.network
{
    public class RelayAPI : ShareObject
    {
        private readonly NetworkSystem _mod;
        internal RelayAPI(NetworkSystem mod) => _mod = mod;
    }
}