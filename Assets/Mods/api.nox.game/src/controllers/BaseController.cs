using System.Collections.Generic;
using Nox.CCK.Players;
using UnityEngine;

namespace api.nox.game.controllers
{
    public abstract class BaseController : MonoBehaviour
    {
        public virtual uint Priority => 0;

        [Header("Base Settings")] public Transform baseTransform;
        public Camera playerCamera;
        public Transform transformOffset;
        public CapsuleCollider bodyCollider;

        private bool _isInitialized;
        private float _jumpForce;
        private float _flySpeed;
        private bool _canJump = true;
        private bool _canFly = false;

        public virtual Transform GetPart(PlayerRig rig)
            => GetParts().GetValueOrDefault(rig);

        public virtual Dictionary<PlayerRig, Transform> GetParts()
            => new()
            {
                { PlayerRig.Base, baseTransform },
                { PlayerRig.Head, playerCamera.transform },
            };

        public abstract void Move(Vector3 direction);

        public virtual bool IsGrounded => false;
        public abstract bool CanMovement { get; set; }
        public abstract double MaxSpeed { get; set; }
        public abstract bool IsCrouching { get; set; }

        public virtual bool IsFlying
        {
            get => _canFly;
            set { }
        }

        public virtual Quaternion Rotation
        {
            get => baseTransform.transform.rotation;
            set => baseTransform.transform.rotation = value;
        }

        public virtual Vector3 Position
        {
            get => baseTransform.transform.position;
            set => baseTransform.transform.position = value;
        }

        public float Height
        {
            get => bodyCollider.height;
            set => bodyCollider.height = value;
        }

        public float JumpForce
        {
            get => _jumpForce > float.Epsilon ? _jumpForce : Nox.CCK.Utils.Constants.DefaultJumpForce;
            set => _jumpForce = value;
        }

        public float FlySpeed
        {
            get => _flySpeed > float.Epsilon ? _flySpeed : Nox.CCK.Utils.Constants.DefaultFlySpeed;
            set => _flySpeed = value;
        }

        public bool CanJump
        {
            get => _canJump && CanMovement;
            set => _canJump = value;
        }
        
        public bool CanFly
        {
            get => _canFly && CanMovement;
            set => _canFly = value;
        }

        public virtual void OnControllerDisable(BaseController current)
        {
        }

        public virtual void OnControllerEnable(BaseController last)
        {
        }

        public virtual void OnInitialize()
        {
        }

        public virtual void Dispose()
        {
        }

        protected virtual Quaternion GetForward()
        {
            var rotation = playerCamera.transform.rotation.eulerAngles;
            rotation.x = 0;
            return Quaternion.Euler(rotation);
        }

        public virtual bool Teleport(Transform target)
            => Teleport(new Nox.CCK.Utils.Transform(target));

        public virtual bool Teleport(Nox.CCK.Utils.Transform target)
        {
            if (target.Flags.HasFlag(Nox.CCK.Utils.TransformFlags.Position))
                Position = target.GetPosition();
            if (target.Flags.HasFlag(Nox.CCK.Utils.TransformFlags.Rotation))
                Rotation = target.GetRotation();
            return true;
        }


        /*{
            Logger.Log($"Controller enabled: {GetType().Name}");

            Player.enabled = false;
            Player.headCamera = playerCamera;
            Player.forwardFollow = transformOffset;
            Player.trackingContainer = transform;
            if (!_isInitialized)
            {
                _isInitialized = true;
                OnInitialize();
            }

            Player.enabled = true;
            // Height = height;
        }*/

        /*{
            get => !Player.useGrounding && !Player.body.useGravity;
            set
            {
                if (IsFlying != value)
                    Player.ToggleFlying();
            }
        }*/


        /*{
            if (!CanMovement)
            {
                Logger.LogWarning("Player can't move", this);
                return;
            }

            if (!IsFlying)
            {
                if (direction.magnitude > float.Epsilon)
                    Player.Move(direction, useRelativeDirection: true);
                else Player.Move(Vector2.zero, useRelativeDirection: false);
                if (elevation > 0) Jump(JumpForce);
                return;
            }

            var forward = GetForward();
            var velocity = forward * new Vector3(direction.x, 0, direction.y) + elevation * Vector3.up;

            Player.AddVelocity(velocity * FlySpeed, ForceMode.VelocityChange);
        }*/
    }
}