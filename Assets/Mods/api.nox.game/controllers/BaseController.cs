using Autohand;
using UnityEngine;

namespace api.nox.game.Controllers
{
    public abstract class BaseController : MonoBehaviour
    {
        [Header("Base Settings")]
        public AutoHandPlayer Player;
        public Camera PlayerCamera;
        public virtual uint Priority => 0;

        // set if the player can move
        public bool CanMovement
        {
            get => Player.useMovement;
            set => Player.useMovement = value;
        }

        // set the speed of the player
        public float MaxSpeed
        {
            get => Player.maxMoveSpeed;
            set => Player.maxMoveSpeed = value;
        }

        // set if the player is crounching
        public bool IsCrounching
        {
            get => Player.crouching;
            set => Player.crouching = value;
        }

        // set if the player can jump
        private bool _canJump;
        public bool CanJump
        {
            get => _canJump;
            set => _canJump = value;
        }

        // make the player jump
        public virtual void Jump() { if (_canJump) Player.Jump(); }


        // make the player teleport to a target
        public virtual void Teleport(Transform target)
        {
            Rotation = target.rotation;
            Position = target.position;
        }

        // set the player rotation
        public Quaternion Rotation
        {
            get => Player.transform.rotation;
            set => Player.SetRotation(value);
        }

        // set the player position
        public Vector3 Position
        {
            get => Player.transform.position;
            set => Player.SetPosition(value);
        }

        public bool IsGrounded()
            => Player.IsGrounded();

        public virtual void OnControllerDisable(BaseController current)
        {
            Debug.Log($"Controller disabled: {GetType().Name}");
        }

        private bool is_initialized = false;
        public virtual void OnControllerEnable(BaseController last)
        {
            Debug.Log($"Controller enabled: {GetType().Name}");
            Player.enabled = false;
            Player.headCamera = PlayerCamera;
            Player.forwardFollow = PlayerCamera.transform;
            Player.trackingContainer = transform;
            if (!is_initialized)
            {
                is_initialized = true;
                OnInitialize();
            }
            Player.enabled = true;
        }

        public virtual void OnInitialize()
        {
            Debug.Log($"Controller initialized: {GetType().Name}");
        }

        public virtual void Dispose() { }

        public bool IsFlying
        {
            get => !Player.useGrounding;
            set
            {
                if (IsFlying != value)
                    Player.ToggleFlying();
            }
        }

        public float Height => Player.bodyCollider.height;

        public bool UseMicrophone
        {
            get => false;
            set { }
        }
    }
}