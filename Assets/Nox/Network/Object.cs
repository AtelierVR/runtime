﻿using UnityEngine;
using NTransform = Nox.Scripts.Transform;

namespace Nox.Network
{
    public class Object : Entity
    {
        public ushort Id;
        private ushort _ownerId;

        public ObjectTransfertType TransfertType;

        private Rigidbody _rb => _transform?.GetComponent<Rigidbody>();
        private Transform _transform => RealTransfroms.ContainsKey(0) ? RealTransfroms[0] : null;

        public void SetOwner(ushort newOwnerId)
        {

        }

        public NTransform GetObjectTransform() => new(_transform, _rb);
    }

    public enum ObjectTransfertType
    {
        None,
        Manual,
        Automatic
    }
}