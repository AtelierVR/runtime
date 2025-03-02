using System;
using Nox.CCK.Utils;

namespace api.nox.network.Instances
{
    [Serializable]
    public class Instance : ICached, INoxObject
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
        public string owner;
        public string address;
        public ushort client_count;
        public InstancePlayer[] players;

        public string GetCacheKey() => GetCacheKey(id, server);
        public static string GetCacheKey(uint id, string server) => $"instance.{id}.{server}";
        public override string ToString() => $"{GetType().Name}[id={id}, server={server}]";
    }
}