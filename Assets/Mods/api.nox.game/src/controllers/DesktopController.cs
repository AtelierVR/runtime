using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.game.UI;
using Nox.CCK.Mods.Events;
using Nox.CCK.Players;
using UnityEngine;
using UnityEngine.InputSystem;

namespace api.nox.game.controllers
{
    public class DesktopController : BaseController
    {
        [Header("References")] public Animator animator;
        public CharacterController controller;

        [Header("Settings")] public float movementSpeed = 5f;
        public float jumpForce = 8f;
        public float gravity = 9.81f;
        public float mouseSensitivity = 100f;
        public float[] movementKeys = new float[6];
        public Vector3 inputMovement = Vector3.zero;
        private Mouse _mouse;
        
        [Header("Menu")]
        public ViewPortMenu menu;

        private readonly string[] _keys =
        {
            "forward",
            "backward",
            "left",
            "right",
            "jump",
            "crouch",
            "sprint",
            "menu"
        };

        private EventSubscription _keyBindingRemoved;

        public void Start()
        {
            LockCursor = true;
            _keyBindingRemoved = GameClientSystem.CoreAPI.EventAPI
                .Subscribe("key_binding_removed", OnKeyBindingRemoved);
            Rebind();
            _mouse = InputSystem.GetDevice<Mouse>();
        }

        private void OnKeyBindingRemoved(EventData args) => Rebind();

        private void Rebind()
        {
            foreach (var key in _keys)
                if (!GameSystem.Instance.KeyBindings.HasKeyBinding($"generic.movement.{key}"))
                    GameSystem.Instance.KeyBindings.AddKeyBinding(
                        $"generic.movement.{key}",
                        new InputAction(
                            $"generic.movement.{key}",
                            InputActionType.Button,
                            key switch
                            {
                                "jump" => "<Keyboard>/space",
                                "crouch" => "<Keyboard>/leftCtrl",
                                "forward" => "<Keyboard>/w",
                                "backward" => "<Keyboard>/s",
                                "left" => "<Keyboard>/a",
                                "right" => "<Keyboard>/d",
                                "sprint" => "<Keyboard>/leftShift",
                                "menu" => "<Keyboard>/tab",
                                _ => throw new ArgumentOutOfRangeException()
                            }),
                        "generic.movement"
                    );


            foreach (var key in _keys)
            {
                var action = GameSystem.Instance.KeyBindings.GetKeyBinding($"generic.movement.{key}").Action;
                if (action == null) continue;
                if (new[]
                    {
                        "forward", 
                        "backward", 
                        "left", 
                        "right",
                        "jump",
                        "crouch"
                    }.Contains(key))
                {
                    action.performed += ctx => OnMoveKey(key, ctx.ReadValue<float>());
                    action.canceled += ctx => OnMoveKey(key, 0);
                }
                
                
            }
        }


        private void OnMoveKey(string key, float value)
        {
            switch (key)
            {
                case "forward":
                    movementKeys[0] = value;
                    break;
                case "backward":
                    movementKeys[1] = value;
                    break;
                case "left":
                    movementKeys[2] = value;
                    break;
                case "right":
                    movementKeys[3] = value;
                    break;
                case "jump":
                    movementKeys[4] = value;
                    break;
                case "crouch":
                    movementKeys[5] = value;
                    break;
            }

            inputMovement = new Vector3(
                movementKeys[3] - movementKeys[2],
                movementKeys[4] - movementKeys[5],
                movementKeys[0] - movementKeys[1]
            ).normalized;
        }

        public void Update()
        {
            HandleFly();
            HandleLook();
            HandleMovement();
            HandleJump();
        }

        private void HandleLook()
        {
            var look = _mouse.delta.ReadValue();
            var lookDelta = look * mouseSensitivity * Time.deltaTime;

            // rotate up-down with camera
            var cameraRotation = playerCamera.transform.localEulerAngles;
            cameraRotation.x -= lookDelta.y;
            
            // between 90,0 and 270,360 
            cameraRotation.x = cameraRotation.x > 180f
                ? Mathf.Clamp(cameraRotation.x, 270f, 360f)
                : Mathf.Clamp(cameraRotation.x, -90f, 90f);
            playerCamera.transform.localEulerAngles = cameraRotation;

            // rotate left-right with player
            transform.Rotate(Vector3.up, lookDelta.x);
        }
        
        private void HandleJump()
        {
            if (IsFlying) return;
            
            if (movementKeys[4] > 0 && IsGrounded)
                controller.Move(Vector3.up * jumpForce * Time.deltaTime);
        }

        private void HandleMovement()
        {
            var finalMovement = inputMovement;
            finalMovement *= movementSpeed * Time.deltaTime;
            finalMovement.y = 0;
            finalMovement = transform.forward * finalMovement.z + transform.right * finalMovement.x;
            controller.Move(finalMovement);
        }


        public void OnDestroy()
        {
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_keyBindingRemoved);
            foreach (var key in _keys)
                if (GameSystem.Instance.KeyBindings.HasKeyBinding($"generic.movement.{key}"))
                    GameSystem.Instance.KeyBindings.RemoveKeyBinding($"generic.movement.{key}");
        }

        private void HandleFly()
        {
            if (IsGrounded && IsFlying)
                IsFlying = false;
        }


        public override bool IsGrounded
            => controller.isGrounded;


        public override Dictionary<PlayerRig, Transform> GetParts()
        {
            throw new NotImplementedException();
        }

        public override void Move(Vector3 direction)
        {
            if (!CanMovement) return;
            controller.Move(direction);
        }

        public override bool CanMovement { get; set; }
        public override double MaxSpeed { get; set; }
        public override bool IsCrouching { get; set; }


        public override bool IsFlying
        {
            get => false;
            set { }
        }

        public bool LockCursor
        {
            get => Cursor.lockState == CursorLockMode.Locked;
            set
            {
                Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !value;
            }
        }
    }
}