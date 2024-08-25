using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NTransform = api.nox.network.Utils.Transform;

namespace api.nox.network
{
    public class Entity
    {
        public Dictionary<ushort, Transform> RealTransfroms;
        public Dictionary<ushort, NTransform> Transfroms
        {
            get
            {
                var transfroms = new Dictionary<ushort, NTransform>();
                foreach (var pair in RealTransfroms)
                    transfroms.Add(pair.Key, new NTransform(pair.Value, pair.Value.GetComponent<Rigidbody>()));
                return transfroms;
            }
        }
    }
}