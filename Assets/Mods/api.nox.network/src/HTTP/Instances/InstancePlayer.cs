using System;

namespace api.nox.network.Instances
{
    [Serializable]
    public class InstancePlayer
    {
        public string user;
        public string display;
        
        public override string ToString()
            => $"{GetType().Name}[user={user}, display={display}]";
    }
}