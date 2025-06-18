using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.nox.user.network;
using Nox.CCK.Mods.Panels;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.user {
	public class AuthPanel : IEditorPanelBuilder {
		public string GetId()
			=> "auth";

		public string GetName()
			=> "User/Auth";

		public bool IsHidden()
			=> Main.Instance.Network.CurrentUser != null;

		private readonly VisualElement        _root = new();
		private          VerificationRequired _verification;
		private          string               _currentIdentifier;
		private          string               _currentPassword;
		private          string               _currentServer;

		public VisualElement Make(Dictionary<string, object> data) {
			_root.ClearBindings();
			_root.Clear();

			var child = EditorUser.CoreAPI.AssetAPI
				.GetAsset<VisualTreeAsset>("auth.uxml")
				.CloneTree();
			child.style.flexGrow = 1;
			_root.Add(child);
			_root.Q<Label>("version").text = "v" + EditorUser.CoreAPI.ModMetadata.GetVersion();

			var button     = _root.Q<Button>("login-button");
			var password   = _root.Q<TextField>("password");
			var identifier = _root.Q<TextField>("identifier");
			var server     = _root.Q<TextField>("server");

			// 2FA elements
			var verificationPanel   = _root.Q<VisualElement>("verification-panel");
			var verificationMessage = _root.Q<Label>("verification-message");
			var methodSelection     = _root.Q<VisualElement>("method-selection");
			var verificationMethods = _root.Q<DropdownField>("verification-methods");
			var verificationCode    = _root.Q<TextField>("verification-code");
			var sendEmailCodeButton = _root.Q<Button>("send-email-code");
			var backToLoginButton   = _root.Q<Button>("back-to-login");

			// Initially hide verification panel
			HideVerificationPanel();

			// Add event listener for Enter key press
			server.RegisterCallback<KeyDownEvent>(
				evt => {
					if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter)) return;
					identifier.Focus();
					evt.StopPropagation();
				}
			);

			identifier.RegisterCallback<KeyDownEvent>(
				evt => {
					if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter)) return;
					password.Focus();
					evt.StopPropagation();
				}
			);

			password.RegisterCallback<KeyDownEvent>(
				evt => {
					if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter)) return;
					if (verificationPanel.style.display == DisplayStyle.None) {
						button.Focus();
					} else {
						verificationCode.Focus();
					}

					evt.StopPropagation();
				}
			);

			verificationCode.RegisterCallback<KeyDownEvent>(
				evt => {
					if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter)) return;
					button.Focus();
					evt.StopPropagation();
				}
			);

			// Main login button click
			button.clickable.clicked += async () => {
				button.SetEnabled(false);
				password.isReadOnly         = true;
				identifier.isReadOnly       = true;
				server.isReadOnly           = true;
				verificationCode.isReadOnly = true;

				try {
					if (verificationPanel.style.display == DisplayStyle.None) {
						// Initial login attempt
						await PerformLogin(identifier.value, password.value, server.value, null);
					} else {
						// 2FA verification attempt
						await PerformLogin(_currentIdentifier, _currentPassword, _currentServer, verificationCode.value);
					}
				} finally {
					button.SetEnabled(true);
					password.isReadOnly         = false;
					identifier.isReadOnly       = false;
					server.isReadOnly           = false;
					verificationCode.isReadOnly = false;
				}
			};

			// Send email code button
			sendEmailCodeButton.clickable.clicked += async () => {
				sendEmailCodeButton.SetEnabled(false);
				sendEmailCodeButton.text = "Sending...";

				try {
					var result = await Main.Instance.Network.SendVerificationCode("email", server.value);
					if (result.IsSuccess()) {
						verificationMessage.text = "Verification code sent to your email. Please check your inbox.";
					} else {
						verificationMessage.text = $"Failed to send code: {result.GetMessage()}";
					}
				} catch (System.Exception ex) {
					verificationMessage.text = $"Error sending code: {ex.Message}";
				} finally {
					sendEmailCodeButton.SetEnabled(true);
					sendEmailCodeButton.text = "Send Email Code";
				}
			};

			// Back to login button
			backToLoginButton.clickable.clicked += () => {
				HideVerificationPanel();
				ClearVerificationData();
			};

			// Verification method selection
			verificationMethods.RegisterValueChangedCallback(
				evt => {
					var selectedMethod = _verification.Methods?.FirstOrDefault(m => m.GetMethodName() == evt.newValue);
					if (selectedMethod != null) UpdateUIForVerificationMethod(selectedMethod);
				}
			);

			return _root;
		}

		private async Task PerformLogin(string identifier, string password, string serverAddress, string factorCode) {
			Logger.LogDebug($"Performing login for {identifier} with factor code: {(!string.IsNullOrEmpty(factorCode) ? "provided" : "not provided")}");

			var loginRequest = new LoginRequest {
				Identifier = identifier,
				Password   = password
			};

			if (!string.IsNullOrEmpty(factorCode)) {
				loginRequest.FactorCode = factorCode;
			}

			var response = await Main.Instance.Network.Login(loginRequest, serverAddress);

			if (response.IsError()) {
				Logger.Log($"Login error: {response.GetError()} {response.Verification.Required}");
				if (response.Verification.Required) {
					_currentIdentifier = identifier;
					_currentPassword   = password;
					_currentServer     = serverAddress;
					_verification      = response.Verification;
					ShowVerificationPanel(_verification);
					return;
				}

				HideVerificationPanel();
				return;
			}

			// Successful login
			if (Main.Instance.Network.CurrentUser != null) {
				EditorUser.CoreAPI.PanelAPI.SetActivePanel("profile");
				EditorUser.CoreAPI.PanelAPI.UpdatePanelList();
				HideVerificationPanel();
				ClearVerificationData();
			} else {
				Logger.Log($"Login successful but current user is null: {response}");
			}
		}

		private void ShowVerificationPanel(VerificationRequired response) {
			var verificationPanel   = _root.Q<VisualElement>("verification-panel");
			var verificationMessage = _root.Q<Label>("verification-message");
			var methodSelection     = _root.Q<VisualElement>("method-selection");
			var verificationMethods = _root.Q<DropdownField>("verification-methods");
			var verificationCode    = _root.Q<TextField>("verification-code");
			var button              = _root.Q<Button>("login-button");

			verificationPanel.style.display = DisplayStyle.Flex;
			verificationMessage.text        = "Please verify your identity using one of the following methods:";
			verificationCode.value          = "";

			var enabledMethods = response.Methods.Where(m => m.IsEnabled()).ToArray();

			if (enabledMethods.Length > 1) {
				// Multiple methods available - show dropdown
				methodSelection.style.display = DisplayStyle.Flex;
				var methodNames = enabledMethods.Select(m => m.GetMethodName()).ToList();
				verificationMethods.choices = methodNames;
				verificationMethods.value   = methodNames.FirstOrDefault();

				if (!string.IsNullOrEmpty(verificationMethods.value)) {
					var selectedMethod = enabledMethods.FirstOrDefault(m => m.GetMethodName() == verificationMethods.value);
					if (selectedMethod != null) {
						UpdateUIForVerificationMethod(selectedMethod);
					}
				}
			} else if (enabledMethods.Length == 1) {
				// Single method - use it directly
				methodSelection.style.display = DisplayStyle.None;
				UpdateUIForVerificationMethod(enabledMethods[0]);
			}

			button.text = "Verify";
			verificationCode.Focus();
		}

		private void UpdateUIForVerificationMethod(VerificationMethod method) {
			var sendEmailCodeButton = _root.Q<Button>("send-email-code");
			var verificationCode    = _root.Q<TextField>("verification-code");
			var verificationMessage = _root.Q<Label>("verification-message");
			if (method.IsEmail()) {
				sendEmailCodeButton.style.display = DisplayStyle.Flex;
				verificationCode.value            = "";
				verificationMessage.text          = "A verification code will be sent to your email address.";
			} else if (method.IsTotp()) {
				sendEmailCodeButton.style.display = DisplayStyle.None;
				verificationCode.value            = "";
				verificationMessage.text          = "Enter the code from your authenticator app.";
			} else {
				sendEmailCodeButton.style.display = DisplayStyle.None;
				verificationCode.value            = "";
				verificationMessage.text          = "Enter your verification code.";
			}
		}

		private void HideVerificationPanel() {
			var verificationPanel = _root.Q<VisualElement>("verification-panel");
			var button            = _root.Q<Button>("login-button");

			verificationPanel.style.display = DisplayStyle.None;
			button.text                     = "Login";
		}

		private void ClearVerificationData() {
			_verification      = null;
			_currentIdentifier = null;
			_currentPassword   = null;
			_currentServer     = null;

			var verificationCode = _root.Q<TextField>("verification-code");
			verificationCode.value = "";
		}
	}
}