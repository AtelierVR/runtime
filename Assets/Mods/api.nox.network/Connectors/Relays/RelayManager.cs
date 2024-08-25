using api.nox.network.Relays;
using api.nox.network.Utils;
using UnityEngine;

namespace api.nox.network
{
    public class RelayManager : Manager<Relay>
    {
        private NetworkSystem networkSystem;

        public RelayManager(NetworkSystem networkSystem)
        {
            this.networkSystem = networkSystem;
            foreach (var relay in Cache)
                relay.Connector.Close();
            Cache.Clear();
        }

        public static Relay Get(ushort id) => Cache.Find(r => r.Id == id);

        // ReSharper disable Unity.PerformanceAnalysis
        public static void Update()
        {
            foreach (var relay in Cache)
                relay.Update();
        }

        public static ushort NextId()
        {
            if (Cache.Count >= ushort.MaxValue)
                return ushort.MaxValue;
            ushort id;
            var i = 0;
            do
            {
                id = (ushort)Random.Range(ushort.MinValue, ushort.MaxValue);
                i++;
                if (i > 1000)
                {
                    id = ushort.MinValue;
                    while (Cache.Exists(r => r.Id == id))
                        id++;
                    break;
                }
            } while (Cache.Exists(r => r.Id == id));

            return id;
        }

        public static Relay New<T>() where T : IConnector, new() => Set(new Relay(new T()));
    }
}