using UnityEngine;
using NTransform = api.nox.network.Utils.Transform;

namespace api.nox.network
{
    public class Object : Entity
    {
        public ushort Id;
        private ushort _ownerId;

        public ObjectTransfertType TransfertType;

        public void SetOwner(ushort newOwnerId)
        {

        }
    }

    public enum ObjectTransfertType
    {
        None,
        Manual,
        Automatic
    }
}