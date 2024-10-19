using System.Collections.Generic;
using System.Linq;
using api.nox.network.Utils;

namespace api.nox.network.RelayInstances
{
    public class RelayInstanceManager : Manager<RelayInstance>
    {
        public static RelayInstance Get(ushort internalId, ushort relayId) => Cache
            .FirstOrDefault(instance => instance.InternalId == internalId && instance.RelayId == relayId);
        public static List<RelayInstance> Get(ushort relayId) => Cache
            .Where(instance => instance.RelayId == relayId).ToList();

        public static void Update()
        {
            foreach (var RelayInstance in Cache)
                RelayInstance.Update();
        }

        public static void Dispose()
        {
            foreach (var RelayInstance in Cache.ToList())
                RelayInstance.Dispose();
            Cache.Clear();
        }
    }
}