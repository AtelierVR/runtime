using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NTransform = api.nox.network.Utils.Transform;

namespace api.nox.network
{
    public class Entity
    {
        public Dictionary<ushort, NTransform> Transfroms = new();
    }
}