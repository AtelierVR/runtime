using System;
using api.nox.network.HTTP;
using api.nox.network.Relays;

namespace api.nox.network.Instances
{
    [Serializable]
    public class Instance : ICached
    {
        public uint id;
        public string title;
        public string description;
        public string thumbnail;
        public string server;
        public string name;
        public ushort capacity;
        public string[] tags;
        public string world;
        public string address;
        public ushort client_count;
        public InstancePlayer[] players;

        public string GetCacheKey() => $"instance.{id}.{server}";
        public Relay GetRelay() => NetworkSystem.ModInstance.Relay.GetRelay(address);
    }
}