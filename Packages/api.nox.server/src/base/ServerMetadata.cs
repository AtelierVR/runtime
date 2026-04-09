using System;
using Nox.CCK.Network;
using Nox.Servers;

namespace api.nox.server
{
    [Serializable]
    public class ServerMetadata : IServerMetadata
    {
        public string Title { get; private set; }

        public string Description { get; private set; }

        public string Icon { get; private set; }

        public string Contact { get; private set; }

        public static ServerMetadata From(NoxMetadata m)
            => m != null
                ? new ServerMetadata
                {
                    Title = m.title,
                    Description = m.description,
                    Icon = m.icon,
                    Contact = m.contact
                }
                : null;
    }
}
