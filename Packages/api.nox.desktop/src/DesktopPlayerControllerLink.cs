using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace api.nox.desktop {
	public class DesktopPlayerControllerLink : MonoBehaviour {
		[Header("Player Reference")] public DesktopPlayer player;

		[Header("Input Settings")] [Tooltip("Movement deadzone to prevent drift")]
		public float movementDeadzone = 0.1f;

		[Tooltip("Turn deadzone to prevent drift")]
		public float turnDeadzone = 0.1f;

		[Tooltip("Mouse sensitivity for looking around")]
		public float mouseSensitivity = 2f;

		[Tooltip("Maximum look angle up/down")]
		public float maxLookAngle = 90f;

		[Header("Mouse Look Settings")]
		[Tooltip("Mouse smoothing factor (0 = no smoothing, higher = more smoothing)")]
		public float mouseSmoothing = 0.1f;

		[Tooltip("Use frame rate independent mouse sensitivity")]
		public bool frameRateIndependentMouse = true;

		[Tooltip("Mouse input deadzone")]
		public float mouseDeadzone = 0.001f;

		[Header("Auto Jump Settings")] [Tooltip("Delay between auto jumps after landing")]
		public float autoJumpDelay = 0.3f;

		[Header("Double Jump to Fly Settings")] [Tooltip("Time window to detect double jump")]
		public float doubleJumpWindow = 0.3f;

		[Tooltip("Enable double jump to fly feature")]
		public bool enableDoubleJumpToFly = true; // Private fields for mouse look

		private float mouseX;
		private float mouseY;
		private float verticalRotation = 0;
		private bool  jumpPressed      = false;
		private bool  sprintPressed    = false;
		private bool  menuPressed      = false;

		// Smoothing variables for mouse input
		private Vector2 smoothMouseDelta = Vector2.zero;
		private Vector2 currentMouseDelta = Vector2.zero;
		private Vector2 targetMouseDelta = Vector2.zero;
		
		// Frame time tracking for lag compensation
		private float lastFrameTime = 0f;
		private float smoothDeltaTime = 0f;

		// Auto jump state variables
		private bool isAutoJumping = false;
		private bool wasGrounded   = true;

		private float autoJumpTimer = 0f;

		// Double jump to fly variables
		private float lastJumpTime = 0f;
		private int   jumpCount    = 0;
		private bool  useMovement  = true;

		private void Start() {
			if (player == null) {
				player = GetComponent<DesktopPlayer>();
				if (player == null) {
					Debug.LogError("DesktopPlayerControllerLink: No DesktopPlayer found!");
				}
			}

			// Configuration will be applied manually when needed

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
			if (!useMovement) {
				return Vector2.zero;
			}

			// Use the existing Keybindings.GetMovement() method
			var input = Keybindings.GetMovement();

			// Apply deadzone
			if (input.magnitude < movementDeadzone)
				input = Vector2.zero;

			return input;
		}

		private void HandleMouseLook() {
			// Block mouse look if menu is open
			if (!useMovement) {
				return;
			}

			// Calculate smooth deltaTime for lag compensation
			float currentTime = Time.unscaledTime;
			float actualDeltaTime = currentTime - lastFrameTime;
			lastFrameTime = currentTime;
			
			// Smooth deltaTime to prevent spikes during lag
			smoothDeltaTime = Mathf.Lerp(smoothDeltaTime, actualDeltaTime, 0.1f);
			
			// Get raw mouse input
			float rawMouseX = Input.GetAxis("Mouse X");
			float rawMouseY = Input.GetAxis("Mouse Y");
			
			// Apply deadzone to prevent micro-movements
			if (Mathf.Abs(rawMouseX) < mouseDeadzone) rawMouseX = 0f;
			if (Mathf.Abs(rawMouseY) < mouseDeadzone) rawMouseY = 0f;
			
			// Calculate target mouse delta with sensitivity
			targetMouseDelta = new Vector2(rawMouseX, rawMouseY) * mouseSensitivity;
			
			// Limit maximum mouse delta per frame to prevent huge jumps during lag
			float maxMouseDelta = 10f; // Maximum degrees per frame
			targetMouseDelta.x = Mathf.Clamp(targetMouseDelta.x, -maxMouseDelta, maxMouseDelta);
			targetMouseDelta.y = Mathf.Clamp(targetMouseDelta.y, -maxMouseDelta, maxMouseDelta);
			
			// Apply frame rate compensation if enabled
			if (frameRateIndependentMouse && smoothDeltaTime > 0f) {
				float targetFrameRate = 60f; // Target 60 FPS
				float frameRateMultiplier = (smoothDeltaTime * targetFrameRate);
				frameRateMultiplier = Mathf.Clamp(frameRateMultiplier, 0.1f, 3f); // Prevent extreme values
				targetMouseDelta *= frameRateMultiplier;
			}
			
			// Smooth the mouse input to reduce jitter
			if (mouseSmoothing > 0f) {
				currentMouseDelta = Vector2.Lerp(currentMouseDelta, targetMouseDelta, 
					Mathf.Clamp01(1f - mouseSmoothing));
			} else {
				currentMouseDelta = targetMouseDelta;
			}
			
			// Apply horizontal rotation to player body
			if (Mathf.Abs(currentMouseDelta.x) > 0.001f) {
				player.transform.Rotate(Vector3.up * currentMouseDelta.x);
			}
			
			// Apply vertical rotation to camera
			if (Mathf.Abs(currentMouseDelta.y) > 0.001f) {
				verticalRotation -= currentMouseDelta.y;
				verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
				player.headCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
			}
		}

		private void HandleJumpInput() {
			// Block jump input if menu is open
			if (!useMovement) {
				return;
			}

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
			if (!useMovement) {
				return;
			}

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