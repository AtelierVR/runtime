using System;
using api.nox.network.HTTP;

namespace api.nox.network.Worlds
{
    [Serializable]
    public class World : ICached
    {
        public uint id;
        public string title;
        public string description;
        public ushort capacity;
        public string[] tags;
        public string owner;
        public string server;
        public string thumbnail;

        public string GetCacheKey() => $"world.{id}.{server}";

        public WorldIdentifier ToIdentifier() => new(id, server);

        public override string ToString() => $"{GetType().Name}[id={id}, server={server}]";
    }
}