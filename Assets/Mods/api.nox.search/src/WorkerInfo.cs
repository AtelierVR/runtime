using System;

namespace Mods.api.nox.search.src
{
    [Serializable]
    public class WorkerInfo
    {
        public string address;
        public string title;
        public string[] features;
        public bool navigation;
    }
}