using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.desktop {
	public class DesktopPlayerControllerLink : MonoBehaviour {
		[Header("Player Reference")]
		public DesktopPlayer player;

		[Header("Input Settings")]
		[Tooltip("Movement deadzone to prevent drift")]
		public float movementDeadzone = 0.1f;

		[Tooltip("Turn deadzone to prevent drift")]
		public float turnDeadzone = 0.1f;

		[Tooltip("Mouse sensitivity for looking around")]
		public float mouseSensitivity = 0.5f;

		[Tooltip("Maximum look angle up/down")]
		public float maxLookAngle = 90f;

		[Tooltip("Mouse input deadzone")]
		public float mouseDeadzone = 0.1f;

		[Header("Auto Jump Settings")]
		[Tooltip("Delay between auto jumps after landing")]
		public float autoJumpDelay = 0.3f;

		[Header("Double Jump to Fly Settings")]
		[Tooltip("Time window to detect double jump")]
		public float doubleJumpWindow = 0.3f;

		[Tooltip("Enable double jump to fly feature")]
		public bool enableDoubleJumpToFly = true;

		// Private fields
		private float verticalRotation;
		private bool  jumpPressed;
		private bool  sprintPressed;
		private bool  menuPressed;

		// Auto jump state variables
		private bool  isAutoJumping;
		private bool  wasGrounded = true;
		private float autoJumpTimer;

		// Double jump to fly variables
		private float lastJumpTime;
		private int   jumpCount;
		private bool  useMovement = true;

		private void Start() {
			if (!player) {
				player = GetComponent<DesktopPlayer>();
				if (!player)
					Logger.LogError("No DesktopPlayer found!");
			}

			// Lock cursor to center of screen for FPS-style look
			Cursor.lockState = CursorLockMode.Locked;
		}

		private void Update() {
			if (!player) return;

			// Handle movement in Update like XRHandPlayerControllerLink
			var moveInput = GetMovementInput();
			player.Move(moveInput, true, true);

			HandleMouseLook();
			HandleJumpInput();
			HandleCrouchInput();
			HandleSprintInput();
			HandleMenuInput();
		}

		private void FixedUpdate() {
			if (!player) return;

			// Also apply movement in FixedUpdate for physics consistency like XRHandPlayerControllerLink
			var moveInput = GetMovementInput();
			player.Move(moveInput, true, true);

			// Handle flying direction if flying
			if (player.IsFlying()) {
				var flyDir = Vector3.zero;
				if (Keybindings.IsPressed("jump"))
					flyDir.y = 1f;
				else if (Keybindings.IsPressed("crouch"))
					flyDir.y = -1f;

				player.Fly(flyDir);
			}
		}

		private Vector2 GetMovementInput() {
			// Block movement if menu is open
			if (!useMovement) return Vector2.zero;

			// Use the existing Keybindings.GetMovement() method
			var input = Keybindings.GetMovement();

			// Apply deadzone
			if (input.magnitude < movementDeadzone)
				input = Vector2.zero;

			return input;
		}

		private void HandleMouseLook() {
			// Block mouse look if menu is open
			if (!useMovement) return;

			// Get mouse input using new Input System
			var mouse = InputSystem.GetDevice<Mouse>();
			if (mouse == null) return;

			var mouseDelta = mouse.delta.ReadValue();

			// Apply deadzone to prevent micro-movements
			if (Mathf.Abs(mouseDelta.x) < mouseDeadzone)
				mouseDelta.x = 0f;

			if (Mathf.Abs(mouseDelta.y) < mouseDeadzone)
				mouseDelta.y = 0f;

			// Apply sensitivity
			mouseDelta *= mouseSensitivity;

			// Apply horizontal rotation to player
			if (Mathf.Abs(mouseDelta.x) > 0.001f) {
				player.transform.Rotate(0, mouseDelta.x, 0, Space.Self);
			}

			// Apply vertical rotation to camera
			if (Mathf.Abs(mouseDelta.y) > 0.001f) {
				verticalRotation                          -= mouseDelta.y;
				verticalRotation                          =  Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
				player.headCamera.transform.localRotation =  Quaternion.Euler(verticalRotation, 0, 0);
			}
		}

		private void HandleJumpInput() {
			// Block jump input if menu is open
			if (!useMovement) return;

			var jumpCurrentlyPressed = Keybindings.IsPressed("jump");
			var isGrounded           = player.IsGrounded(); // Assuming this method exists
			// Start auto jumping when jump key is first pressed
			if (jumpCurrentlyPressed && !jumpPressed) {
				if (enableDoubleJumpToFly) {
					var timeSinceLastJump = Time.time - lastJumpTime;

					if (player.IsFlying()) {
						// If already flying, check for double jump to disable flying
						if (jumpCount == 1 && timeSinceLastJump <= doubleJumpWindow) {
							// Double jump while flying - disable flying
							player.ToggleFlying();
							jumpCount     = 0;
							isAutoJumping = false;
						} else {
							// First jump while flying - just track it
							jumpCount    = 1;
							lastJumpTime = Time.time;
						}
					} else {
						// Not flying - normal double jump to fly logic
						if (jumpCount == 0 || (isGrounded && timeSinceLastJump > doubleJumpWindow)) {
							// First jump or grounded reset
							player.Jump();
							jumpCount     = 1;
							lastJumpTime  = Time.time;
							isAutoJumping = true;
							autoJumpTimer = 0f;
						} else if (jumpCount == 1 && timeSinceLastJump <= doubleJumpWindow && !isGrounded) {
							// Second jump within window while in air - enable flying
							player.ToggleFlying();
							jumpCount     = 2;
							isAutoJumping = false; // Stop auto jumping when flying
						}
					}
				} else {
					// Normal jump when double jump disabled
					player.Jump();
					isAutoJumping = true;
					autoJumpTimer = 0f;
				}
			}

			// Reset auto jumping when jump key is released
			if (!jumpCurrentlyPressed && jumpPressed) {
				isAutoJumping = false;
				autoJumpTimer = 0f;
			}

			// If the player is grounded, start delay for auto jump
			if (isGrounded && isAutoJumping && !player.IsFlying())
				autoJumpTimer += Time.deltaTime;

			// If the player is grounded and auto jump timer has elapsed, perform an auto jump
			if (isGrounded && isAutoJumping && autoJumpTimer >= autoJumpDelay && !player.IsFlying()) {
				player.Jump();
				autoJumpTimer = 0f;
			} // Reset jump count when grounded and disable flying if touching ground

			if (isGrounded && !wasGrounded) {
				jumpCount = 0;
				// Disable flying when touching ground
				if (player.IsFlying()) {
					player.ToggleFlying();
					isAutoJumping = false;
				}
			}

			jumpPressed = jumpCurrentlyPressed;
			wasGrounded = isGrounded;
		}

		private void HandleCrouchInput() {
			// Block crouch input if menu is open
			if (!useMovement) return;

			var crouchPressed = Keybindings.IsPressed("crouch");
			// Only crouch if not flying
			if (!player.IsFlying())
				player.SetCrouching(crouchPressed);
		}

		private void HandleSprintInput() {
			// Block sprint input if menu is open
			if (!useMovement) {
				return;
			}

			var sprintCurrentlyPressed = Keybindings.IsPressed("sprint");
			player.SetSprinting(sprintCurrentlyPressed);
			sprintPressed = sprintCurrentlyPressed;
		}

		private void HandleMenuInput() {
			var menuCurrentlyPressed = Keybindings.IsPressed("main");

			// Detect key press (not hold)
			if (menuCurrentlyPressed && !menuPressed) {
				// Toggle menu visibility
				var isMenuVisible = player.menu != null && player.menu.GetActive();

				if (player.menu != null) {
					player.menu.SetActive(!isMenuVisible);

					// Handle cursor lock and movement input blocking
					if (!isMenuVisible) {
						// Menu is being opened
						Cursor.lockState = CursorLockMode.None;
						Cursor.visible   = true;
						useMovement      = false; // Block movement inputs
					} else {
						// Menu is being closed
						Cursor.lockState = CursorLockMode.Locked;
						Cursor.visible   = false;
						useMovement      = true; // Re-enable movement inputs
					}
				}
			}

			menuPressed = menuCurrentlyPressed;
		}

		private void OnApplicationFocus(bool hasFocus) {
			// Re-lock cursor when application gains focus, but only if menu is not open
			if (hasFocus && useMovement)
				Cursor.lockState = CursorLockMode.Locked;
		}
	}
}