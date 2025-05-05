using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Utils;

namespace api.nox.session
{
    public class Session : INoxObject
    {
        public List<INoxObject> AbstractPlayers = new();

        [NoxPublic(NoxAccess.Method)]
        public INoxObject GetAbstractPlayer(ushort id)
            => AbstractPlayers.FirstOrDefault(player => player.CallMethod<ushort>("GetId") == id);

        [NoxPublic(NoxAccess.Method)]
        public void RegisterPlayer(INoxObject player)
        {
            if (AbstractPlayers.Contains(player))
            {
                Logger.LogWarning($"Player {player} is already registered.");
                return;
            }

            player.InvokeMethod("SetSession", this);
            AbstractPlayers.Add(player);
            Logger.LogDebug($"Registered player {player}.");
        }

        [NoxPublic(NoxAccess.Method)]
        public void UnregisterPlayer(INoxObject player)
        {
            if (!AbstractPlayers.Contains(player))
            {
                Logger.LogWarning($"Player {player} is not registered.");
                return;
            }

            player.InvokeMethod("SetSession", null);
            AbstractPlayers.Remove(player);
            Logger.LogDebug($"Unregistered player {player}.");
        }
    }
}