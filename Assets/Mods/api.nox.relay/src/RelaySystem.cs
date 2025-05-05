using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using nox.nox.relay.connection;

namespace nox.nox.relay
{
    public class RelaySystem : MainModInitializer
    {
        public List<Connection> Connections = new();
        public static RelaySystem Instance;

        public void OnInitializeMain(MainModCoreAPI api)
        {
            Instance = this;
        }

        public async UniTask OnDisposeMainAsync()
        {
            foreach (var connection in Connections)
                await connection.Disconnect();
            Connections.Clear();
        }
        
        
    }
}