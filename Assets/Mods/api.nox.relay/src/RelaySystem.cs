using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using api.nox.relay.connection;
using api.nox.relay.connector;
using UnityEngine;

namespace api.nox.relay
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

        public void OnUpdateMain()
        {
            foreach (var connection in Connections)
                connection.Update();
        }

        internal ushort NextId()
        {
            if (Connections.Count >= ushort.MaxValue)
                return ushort.MaxValue;

            for (var i = 0; i < 1000; i++)
            {
                var id = (ushort)Random.Range(ushort.MinValue, ushort.MaxValue);
                if (!Connections.Exists(r => r.Id == id))
                    return id;
            }

            var fallbackId = ushort.MinValue;
            while (Connections.Exists(r => r.Id == fallbackId))
                fallbackId++;

            return fallbackId;
        }
    }
}