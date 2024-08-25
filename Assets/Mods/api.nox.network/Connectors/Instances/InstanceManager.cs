﻿using System.Collections.Generic;
using System.Linq;
using api.nox.network.Utils;

namespace api.nox.network.Instances
{
    public class InstanceManager : Manager<Instance>
    {
        public static Instance Get(ushort internalId, ushort relayId) => Cache
            .FirstOrDefault(instance => instance.InternalId == internalId && instance.RelayId == relayId);
        public static List<Instance> Get(ushort relayId) => Cache
            .Where(instance => instance.RelayId == relayId).ToList();
        
        public static void Update()
        {
            foreach (var instance in Cache)
                instance.Update();
        }
    }
}