#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using api.nox.avatar.builder;
using api.nox.avatar.network;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Avatars;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using UnityEngine.UIElements;
using Nox.CCK.Mods.Panels;
using Nox.CCK.Utils;
using Avatar = api.nox.avatar.network.Avatar;

namespace api.nox.avatar.editor {
	public class AvatarPublisherPanel : IEditorPanelBuilder {
		public string GetId()
			=> "publisher";

		public string GetName()
			=> "Avatar/Publisher";

		public bool IsHidden()
			=> false;

		public VisualElement[] GetHeaders() {
			var button = new Button { text = "Builder" };
			button.AddToClassList("nox-transparent");
			button.RegisterCallback<ClickEvent>(OnGoto);
			return new VisualElement[] { button };
		}

		private void OnGoto(ClickEvent evt) {
			Editor.CoreAPI.PanelAPI.SetActivePanel("api.nox.avatar.builder");
			evt.StopPropagation();
		}

		public VisualElement Make(Dictionary<string, object> data) {
			_root.ClearBindings();
			_root.Clear();

			// Reset cached UI elements
			_progressContainer = null;
			_progressBar       = null;
			_progressLabel     = null;
			_publishButton     = null;

			var child = Editor.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("publisher.uxml").CloneTree();
			child.style.flexGrow = 1;
			_root.Add(child);

			// Cache UI elements
			_progressContainer = _root.Q<VisualElement>("progress-container");
			_progressBar       = _root.Q<ProgressBar>("progress-bar");
			_progressLabel     = _root.Q<Label>("progress-label");
			_publishButton     = _root.Q<Button>("publish-button");

			var versionLabel = _root.Q<Label>("version");
			if (versionLabel != null)
				versionLabel.text = "v" + Editor.CoreAPI.ModMetadata.GetVersion();

			var assignedIcon = _root.Q<Image>("assigned-icon");
			if (assignedIcon != null)
				assignedIcon.image = Editor.CoreAPI.AssetAPI.GetAsset<Texture2D>("ui", "icons/warning.png");

			var descriptor    = AvatarBuilderPanel.Descriptors.Length > 0 ? AvatarBuilderPanel.Descriptors[0] : null;
			var platformField = _root.Q<EnumField>("platform-field");
			if (platformField != null)
				platformField.Init(descriptor?.target ?? Platform.None);

			var detectPlatformButton = _root.Q<Button>("detect-platform");
			if (detectPlatformButton != null)
				detectPlatformButton.clicked += () => {
					var platformFieldInner = _root.Q<EnumField>("platform-field");
					if (platformFieldInner != null)
						platformFieldInner.value = PlatformExtensions.CurrentPlatform;
				};

			var descriptorField = _root.Q<ObjectField>("descriptor-field");
			if (descriptorField != null)
				descriptorField.value = descriptor;
			platformField?.RegisterValueChangedCallback(
				e => {
					var mainDescriptor = AvatarBuilderPanel.Descriptors.Length > 0 ? AvatarBuilderPanel.Descriptors[0] : null;
					if (!mainDescriptor) return;
					var plat = (Platform)e.newValue;
					// Allow Platform.None as a valid selection (no specific platform)
					if (plat == Platform.None || plat.IsSupported())
						mainDescriptor.target = plat;
					else {
						EditorUtility.DisplayDialog("Error", $"\"{plat}\" is not supported.", "Ok");
						Logger.LogError(
							$"Platform \"{plat.GetPlatformName()}\" ({plat.GetBuildTarget()}) is not supported."
						);
						var platformFieldInner = _root.Q<EnumField>("platform-field");
						if (platformFieldInner != null)
							platformFieldInner.value = e.previousValue;
					}
				}
			);

			var gotoBuilderButton = _root.Q<Button>("goto-builder");
			if (gotoBuilderButton != null)
				gotoBuilderButton.clicked += () => Editor.CoreAPI.PanelAPI.SetActivePanel("api.nox.avatar.builder");

			var gotoLoginButton = _root.Q<Button>("goto-login");
			if (gotoLoginButton != null)
				gotoLoginButton.clicked += () => Editor.CoreAPI.PanelAPI.SetActivePanel("api.nox.user.login");

			_lastDisplay = DisplayFlags.None;
			var attachId     = _root.Q<UnsignedIntegerField>("attach-id");
			var attachServer = _root.Q<TextField>("attach-server");
			var attachButton = _root.Q<Button>("attach-avatar");
			if (attachButton != null && attachId != null && attachServer != null) {
				attachButton.clicked += async () => {
					var id     = attachId.value;
					var server = attachServer.value;
					if (string.IsNullOrWhiteSpace(server))
						server = Main.Instance.UserAPI.GetCurrent().GetServerAddress();
					await AttachAvatar(server, id, true);
				};
			}

			var fetchInfoButton = _root.Q<Button>("info-fetch");
			if (fetchInfoButton != null) {
				fetchInfoButton.clicked += async () => {
					if (_avatar == null) return;
					var e = await AttachAvatar(_avatar.GetServerAddress(), _avatar.GetId());
					if (e == null)
						EditorUtility.DisplayDialog("Error", "An error occured while fetching the avatar.", "Ok");
				};
			}

			var updateInfoButton = _root.Q<Button>("info-update");
			if (updateInfoButton != null) {
				updateInfoButton.clicked += async () => {
					Logger.Log("Update" + _avatar);
					if (_avatar == null || !_lastDisplay.HasFlag(DisplayFlags.Avatar)) return;
					Logger.Log("Update1" + _avatar);
					var nameField        = _root.Q<TextField>("info-name");
					var descriptionField = _root.Q<TextField>("info-description");

					if (nameField == null || descriptionField == null) return;

					var name        = nameField.value;
					var description = descriptionField.value;

					SetDisplay(DisplayFlags.Loading);
					var success = await Main.Instance.Network.Update(
						_avatar.GetId(),
						new UpdateAvatarRequest() {
							title       = name,
							description = description
						},
						_avatar.GetServerAddress()
					);
					if (success != null) {
						_avatar = success;
						UpdateAvatar();
						SetDisplay(DisplayFlags.Avatar | DisplayFlags.AvatarAsset);
					} else EditorUtility.DisplayDialog("Error", "An error occured while updating the avatar.", "Ok");
				};
			}

			var detachInfoButton = _root.Q<Button>("info-detach");
			if (detachInfoButton != null) {
				detachInfoButton.clicked += () => {
					if (_avatar == null || !_lastDisplay.HasFlag(DisplayFlags.Avatar)) return;
					var target = AvatarBuilderPanel.Descriptors.Length > 0 ? AvatarBuilderPanel.Descriptors[0] : null;
					if (!target) return;
					target.publishId     = 0;
					target.publishServer = "";
					EditorUtility.SetDirty(target);
					_avatar = null;
					UpdateAvatar();

					// Clear thumbnail preview
					var thumbnailPreview = _root.Q<VisualElement>("thumbnail-preview");
					var thumbnailStatus  = _root.Q<Label>("thumbnail-status");
					if (thumbnailPreview != null && thumbnailStatus != null) {
						thumbnailPreview.Clear();
						thumbnailPreview.Add(thumbnailStatus);
						thumbnailStatus.text = "No avatar attached";
					}

					SetDisplay(DisplayFlags.AvatarNotFound);
				};
			}

			var config            = Config.Load();
			var autoVersionToggle = _root.Q<Toggle>("asset-auto-version");
			if (autoVersionToggle != null) {
				autoVersionToggle.value = config.Get("sdk.auto_version", true);
				autoVersionToggle
					.RegisterValueChangedCallback(
						e => {
							var load = Config.Load();
							load.Set("sdk.auto_version", e.newValue);
							load.Save();
						}
					);
			}

			var strictToggle = _root.Q<Toggle>("asset-strict");
			if (strictToggle != null) {
				strictToggle.value = config.Get("sdk.strict_version", true);
				strictToggle
					.RegisterValueChangedCallback(
						e => {
							var load = Config.Load();
							load.Set("sdk.strict_version", e.newValue);
							load.Save();
						}
					);
			}

			var assetVersionField = _root.Q<UnsignedIntegerField>("asset-version");
			if (assetVersionField != null) {
				assetVersionField
					.RegisterValueChangedCallback(
						e => {
							var target = AvatarBuilderPanel.Descriptors.Length > 0 ? AvatarBuilderPanel.Descriptors[0] : null;
							if (!target) return;
							target.publishVersion = e.newValue;
							EditorUtility.SetDirty(target);
						}
					);
			}

			// Connect button events
			if (_publishButton != null)
				_publishButton.clicked += () => OnPublishAsync().Forget();

			var thumbnailUploadButton = _root.Q<Button>("thumbnail-upload");
			if (thumbnailUploadButton != null)
				thumbnailUploadButton.clicked += () => OnThumbnailUploadAsync().Forget();

			var thumbnailField = _root.Q<ObjectField>("thumbnail-field");
			if (thumbnailField != null) {
				thumbnailField.RegisterValueChangedCallback(OnThumbnailFieldChanged);
			}

			var thumbnailFixButton = _root.Q<Button>("thumbnail-fix-button");
			if (thumbnailFixButton != null) {
				thumbnailFixButton.clicked += OnThumbnailFixClicked;
			}

			SetDisplay(DisplayFlags.NotLogged);
			UpdateAvatar();
			return _root;
		}


		private readonly VisualElement _root = new();

		// Cache for UI elements to avoid repeated queries
		private VisualElement _progressContainer;
		private ProgressBar   _progressBar;
		private Label         _progressLabel;
		private Button        _publishButton;

		private DisplayFlags _displayFlags;
		private DisplayFlags _lastDisplay;
		private Avatar       _avatar;

		private void SetDisplay(DisplayFlags flags) {
			_displayFlags = flags;

			// Force immediate UI update instead of waiting for Update() method
			if (_root.childCount == 0) return;

			var avatarNotFoundElement = _root.Q<VisualElement>("avatar-not-found");
			if (avatarNotFoundElement != null)
				avatarNotFoundElement.style.display = _displayFlags.HasFlag(DisplayFlags.AvatarNotFound) ? DisplayStyle.Flex : DisplayStyle.None;

			var avatarElement = _root.Q<VisualElement>("avatar");
			if (avatarElement != null)
				avatarElement.style.display = _displayFlags.HasFlag(DisplayFlags.Avatar) ? DisplayStyle.Flex : DisplayStyle.None;

			var avatarAssetElement = _root.Q<VisualElement>("avatar-asset");
			if (avatarAssetElement != null)
				avatarAssetElement.style.display = _displayFlags.HasFlag(DisplayFlags.AvatarAsset) ? DisplayStyle.Flex : DisplayStyle.None;

			var notLoggedElement = _root.Q<VisualElement>("not-logged");
			if (notLoggedElement != null)
				notLoggedElement.style.display = _displayFlags.HasFlag(DisplayFlags.NotLogged) ? DisplayStyle.Flex : DisplayStyle.None;

			var loadingElement = _root.Q<VisualElement>("loading");
			if (loadingElement != null)
				loadingElement.style.display = _displayFlags.HasFlag(DisplayFlags.Loading) ? DisplayStyle.Flex : DisplayStyle.None;

			var noDescriptorElement = _root.Q<VisualElement>("no-descriptor");
			if (noDescriptorElement != null)
				noDescriptorElement.style.display = _displayFlags.HasFlag(DisplayFlags.NoDescriptor) ? DisplayStyle.Flex : DisplayStyle.None;
		}

		internal void Update() {
			if (!Editor.HasOnePanelOpened() || _root.childCount == 0) return;
			var user = Main.Instance.UserAPI.GetCurrent();
			if (_lastDisplay == DisplayFlags.NotLogged && user != null)
				OnLogged().Forget();
			if (_lastDisplay != DisplayFlags.NotLogged && user == null)
				SetDisplay(DisplayFlags.NotLogged);
			var descriptor = AvatarBuilderPanel.Descriptors.Length > 0 ? AvatarBuilderPanel.Descriptors[0] : null;

			var descriptorField = _root.Q<ObjectField>("descriptor-field");
			if (descriptorField != null)
				descriptorField.value = descriptor;

			var version      = descriptor?.publishVersion ?? 0;
			var assetVersion = _root.Q<UnsignedIntegerField>("asset-version");
			if (assetVersion != null && assetVersion.value != version)
				assetVersion.value = version;

			// Update _lastDisplay to prevent unnecessary re-updates
			_lastDisplay = _displayFlags;
		}

		private async UniTask OnLogged() {
			var descriptor = AvatarBuilderPanel.Descriptors.Length > 0 ? AvatarBuilderPanel.Descriptors[0] : null;
			if (!descriptor) {
				SetDisplay(DisplayFlags.NoDescriptor);
				return;
			}

			SetDisplay(DisplayFlags.Loading);
			var avatarId      = descriptor.publishId;
			var serverAddress = descriptor.publishServer;
			await AttachAvatar(serverAddress, avatarId);
		}

		private async UniTask<INoxObject> AttachAvatar(string server, uint id, bool create = false) {
			var descriptor = AvatarBuilderPanel.Descriptors.Length > 0 ? AvatarBuilderPanel.Descriptors[0] : null;
			if (!descriptor) {
				SetDisplay(DisplayFlags.NoDescriptor);
				_avatar = null;
				return null;
			}

			SetDisplay(DisplayFlags.Loading);

			Avatar avatar      = null;
			if (id > 0) avatar = await Main.Instance.Network.Fetch(id, server);

			if (avatar == null && create)
				avatar = await Main.Instance.Network.Create(new CreateAvatarRequest { Id = id }, server);

			if (avatar != null) {
				var user          = Main.Instance.UserAPI.GetCurrent();
				var isContributor = user != null && user.ToIdentifier().Equals(avatar.GetOwnerId());

				if (!isContributor) {
					EditorUtility.DisplayDialog(
						"Error",
						"You are not a contributor of this avatar, you cannot attach it.",
						"Ok"
					);
					Logger.LogError("You are not a contributor of this avatar, you cannot attach it.");
					SetDisplay(DisplayFlags.AvatarNotFound);
					_avatar = null;
					return null;
				}
			}

			if (avatar == null) {
				if (create) {
					EditorUtility.DisplayDialog("Error", "An error occured while creating the avatar.", "Ok");
					Logger.LogError(
						"An error occured while creating the avatar, please check the server and your permissions."
					);
				}

				SetDisplay(DisplayFlags.AvatarNotFound);
				_avatar = null;
				return null;
			}

			SetDisplay(DisplayFlags.Avatar | DisplayFlags.AvatarAsset);
			descriptor.publishId     = avatar.GetId();
			descriptor.publishServer = avatar.GetServerAddress();
			EditorUtility.SetDirty(descriptor);
			_avatar = avatar;
			UpdateAvatar();
			return avatar;
		}


		[Flags]
		private enum DisplayFlags {
			None           = 0,
			NotLogged      = 1 << 0,
			Loading        = 1 << 1,
			NoDescriptor   = 1 << 2,
			AvatarNotFound = 1 << 3,
			Avatar         = 1 << 4,
			AvatarAsset    = 1 << 5
		}

		private void UpdateAvatar() {
			if (_root.childCount == 0) return;

			var serverField = _root.Q<TextField>("info-server");
			if (serverField != null)
				serverField.value = _avatar?.GetServerAddress() ?? "";

			var idField = _root.Q<UnsignedIntegerField>("info-id");
			if (idField != null)
				idField.value = _avatar?.GetId() ?? 0;

			var nameField = _root.Q<TextField>("info-name");
			if (nameField != null)
				nameField.value = _avatar?.GetTitle() ?? "";

			var descriptionField = _root.Q<TextField>("info-description");
			if (descriptionField != null)
				descriptionField.value = _avatar?.GetDescription() ?? "";

			var tagsList = _root.Q<ListView>("info-tags");
			if (tagsList != null) {
				tagsList.makeItem    = () => new Label { style = { marginLeft = 4, marginRight = 4 } };
				tagsList.itemsSource = _avatar?.GetTags() ?? Array.Empty<string>();
				tagsList.bindItem = (e, i) => {
					var label = (Label)e;
					if (_avatar?.GetTags() != null && i < _avatar.GetTags().Length)
						label.text = _avatar.GetTags()[i];
				};
			}

			UpdateThumbnailPreview();
		}

		private void UpdateThumbnailPreview() {
			if (_root.childCount == 0) return;

			// Check if user has selected a local texture first
			var thumbnailField = _root.Q<ObjectField>("thumbnail-field");
			if (thumbnailField != null && thumbnailField.value is Texture2D localTexture) {
				UpdateThumbnailPreviewWithTexture(localTexture);
				return;
			}

			var thumbnailPreview = _root.Q<VisualElement>("thumbnail-preview");
			var thumbnailStatus  = _root.Q<Label>("thumbnail-status");

			if (thumbnailPreview != null && thumbnailStatus != null) {
				if (_avatar != null) {
					// Try to download and display the current thumbnail from server
					DownloadAndDisplayThumbnail().Forget();
				} else {
					thumbnailStatus.text        = "No avatar attached";
					thumbnailStatus.style.color = new StyleColor(new UnityEngine.Color(1f, 1f, 1f, 0.6f));
				}
			}
		}

		private async UniTask DownloadAndDisplayThumbnail() {
			var thumbnailPreview   = _root.Q<VisualElement>("thumbnail-preview");
			var thumbnailStatus    = _root.Q<Label>("thumbnail-status");
			var thumbnailImage     = _root.Q<Image>("thumbnail-image");
			var thumbnailFixButton = _root.Q<Button>("thumbnail-fix-button");

			if (thumbnailPreview == null || thumbnailStatus == null || thumbnailImage == null || thumbnailFixButton == null || _avatar == null) return;

			try {
				var thumbnailUrl = _avatar.GetThumbnailUrl();

				if (!string.IsNullOrEmpty(thumbnailUrl)) {
					// Show loading state
					thumbnailStatus.text = "Loading thumbnail...";
					thumbnailPreview.RemoveFromClassList("thumbnail-error");
					thumbnailPreview.RemoveFromClassList("thumbnail-warning");
					thumbnailPreview.RemoveFromClassList("thumbnail-success");
					thumbnailPreview.AddToClassList("thumbnail-loading");

					thumbnailImage.style.display     = DisplayStyle.None;
					thumbnailFixButton.style.display = DisplayStyle.None;
					thumbnailStatus.style.display    = DisplayStyle.Flex;

					var texture = await Main.Instance.NetworkAPI.FetchTexture(thumbnailUrl);

					if (texture != null) {
						// Display the downloaded thumbnail
						thumbnailImage.image           = texture;
						thumbnailImage.scaleMode       = ScaleMode.ScaleToFit;
						thumbnailImage.style.width     = 100;
						thumbnailImage.style.height    = 100;
						thumbnailImage.style.maxWidth  = 200;
						thumbnailImage.style.maxHeight = 200;

						thumbnailStatus.text = $"Current thumbnail - {texture.width}x{texture.height}";
						thumbnailPreview.RemoveFromClassList("thumbnail-error");
						thumbnailPreview.RemoveFromClassList("thumbnail-warning");
						thumbnailPreview.RemoveFromClassList("thumbnail-loading");
						thumbnailPreview.AddToClassList("thumbnail-success");

						thumbnailImage.style.display  = DisplayStyle.Flex;
						thumbnailStatus.style.display = DisplayStyle.Flex;
					} else {
						thumbnailStatus.text = "Failed to load thumbnail";
						thumbnailPreview.RemoveFromClassList("thumbnail-success");
						thumbnailPreview.RemoveFromClassList("thumbnail-warning");
						thumbnailPreview.RemoveFromClassList("thumbnail-loading");
						thumbnailPreview.AddToClassList("thumbnail-error");

						thumbnailImage.style.display  = DisplayStyle.None;
						thumbnailStatus.style.display = DisplayStyle.Flex;
					}
				} else {
					thumbnailStatus.text = "No thumbnail available";
					thumbnailPreview.RemoveFromClassList("thumbnail-error");
					thumbnailPreview.RemoveFromClassList("thumbnail-warning");
					thumbnailPreview.RemoveFromClassList("thumbnail-loading");
					thumbnailPreview.RemoveFromClassList("thumbnail-success");

					thumbnailImage.style.display     = DisplayStyle.None;
					thumbnailFixButton.style.display = DisplayStyle.None;
					thumbnailStatus.style.display    = DisplayStyle.Flex;
				}
			} catch (Exception ex) {
				Logger.LogError($"Failed to load thumbnail: {ex.Message}");
				thumbnailStatus.text = "Failed to load thumbnail";
				thumbnailPreview.RemoveFromClassList("thumbnail-success");
				thumbnailPreview.RemoveFromClassList("thumbnail-warning");
				thumbnailPreview.RemoveFromClassList("thumbnail-loading");
				thumbnailPreview.AddToClassList("thumbnail-error");

				thumbnailImage.style.display     = DisplayStyle.None;
				thumbnailFixButton.style.display = DisplayStyle.None;
				thumbnailStatus.style.display    = DisplayStyle.Flex;
			}
		}

		private Texture2D _currentTexture;

		private void OnThumbnailFixClicked() {
			if (_currentTexture != null) {
				MakeTextureReadable(_currentTexture);
			}
		}

		private void OnThumbnailFieldChanged(ChangeEvent<UnityEngine.Object> evt) {
			var texture = evt.newValue as Texture2D;
			_currentTexture = texture;
			UpdateThumbnailPreviewWithTexture(texture);
		}

		private void UpdateThumbnailPreviewWithTexture(Texture2D texture) {
			var thumbnailPreview   = _root.Q<VisualElement>("thumbnail-preview");
			var thumbnailStatus    = _root.Q<Label>("thumbnail-status");
			var thumbnailImage     = _root.Q<Image>("thumbnail-image");
			var thumbnailFixButton = _root.Q<Button>("thumbnail-fix-button");

			if (thumbnailPreview == null || thumbnailStatus == null || thumbnailImage == null || thumbnailFixButton == null) return;

			// Hide all elements initially
			thumbnailStatus.style.display    = DisplayStyle.None;
			thumbnailImage.style.display     = DisplayStyle.None;
			thumbnailFixButton.style.display = DisplayStyle.None;

			if (texture != null) {
				// Validate texture
				if (!texture.isReadable) {
					thumbnailStatus.text = "Texture must be readable";
					thumbnailPreview.RemoveFromClassList("thumbnail-success");
					thumbnailPreview.RemoveFromClassList("thumbnail-warning");
					thumbnailPreview.RemoveFromClassList("thumbnail-loading");
					thumbnailPreview.AddToClassList("thumbnail-error");

					thumbnailFixButton.text = "Fix Automatically";

					thumbnailStatus.style.display    = DisplayStyle.Flex;
					thumbnailFixButton.style.display = DisplayStyle.Flex;
					return;
				}

				// Check texture type for optimal compatibility
				var assetPath = AssetDatabase.GetAssetPath(texture);
				if (!string.IsNullOrEmpty(assetPath)) {
					var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
					if (importer != null && importer.textureType != TextureImporterType.Default) {
						thumbnailStatus.text = $"Texture type is '{importer.textureType}' (recommend 'Default')";
						thumbnailPreview.RemoveFromClassList("thumbnail-success");
						thumbnailPreview.RemoveFromClassList("thumbnail-error");
						thumbnailPreview.RemoveFromClassList("thumbnail-loading");
						thumbnailPreview.AddToClassList("thumbnail-warning");

						thumbnailFixButton.text = "Fix Settings";

						thumbnailStatus.style.display    = DisplayStyle.Flex;
						thumbnailFixButton.style.display = DisplayStyle.Flex;
						return;
					}
				}

				// Display the texture
				thumbnailImage.image = texture;

				thumbnailStatus.text = $"Preview - {texture.width}x{texture.height} - Ready to upload";
				thumbnailPreview.RemoveFromClassList("thumbnail-error");
				thumbnailPreview.RemoveFromClassList("thumbnail-warning");
				thumbnailPreview.RemoveFromClassList("thumbnail-loading");
				thumbnailPreview.AddToClassList("thumbnail-success");

				thumbnailImage.style.display  = DisplayStyle.Flex;
				thumbnailStatus.style.display = DisplayStyle.Flex;
			} else {
				thumbnailStatus.text = "No thumbnail selected";
				thumbnailPreview.RemoveFromClassList("thumbnail-error");
				thumbnailPreview.RemoveFromClassList("thumbnail-warning");
				thumbnailPreview.RemoveFromClassList("thumbnail-loading");
				thumbnailPreview.RemoveFromClassList("thumbnail-success");

				thumbnailStatus.style.display = DisplayStyle.Flex;
			}
		}

		private void MakeTextureReadable(Texture2D texture) {
			if (texture == null) return;
			try {
				// Get the asset path
				var assetPath = AssetDatabase.GetAssetPath(texture);
				if (string.IsNullOrEmpty(assetPath)) {
					EditorUtility.DisplayDialog("Error", "Cannot find texture asset path.", "Ok");
					return;
				}

				// Get the texture importer
				var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
				if (importer == null) {
					EditorUtility.DisplayDialog("Error", "Cannot access texture import settings.", "Ok");
					return;
				}

				// Enable read/write and set compatible format
				importer.isReadable         = true;
				importer.textureType        = TextureImporterType.Default;
				importer.textureCompression = TextureImporterCompression.Uncompressed;

				// Apply the changes
				AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
				AssetDatabase.Refresh();

				// Test if the texture can now be encoded
				var testData = texture.EncodeToPNG();
				if (testData == null || testData.Length == 0) {
					EditorUtility.DisplayDialog(
						"Warning",
						$"Texture '{texture.name}' is now readable but still cannot be encoded to PNG.\n" + "You may need to manually adjust the texture format in import settings.", "Ok"
					);
				}

				// Refresh the preview
				UpdateThumbnailPreviewWithTexture(texture);

				Logger.Log($"Made texture '{texture.name}' readable for thumbnail upload.");
			} catch (System.Exception ex) {
				EditorUtility.DisplayDialog(
					"Error",
					$"Failed to make texture readable: {ex.Message}", "Ok"
				);
				Logger.LogError($"Failed to make texture readable: {ex.Message}");
			}
		}

		private async UniTask OnThumbnailUploadAsync() {
			if (_avatar == null) {
				EditorUtility.DisplayDialog("Error", "No avatar attached.", "Ok");
				Logger.LogError("No avatar attached for thumbnail upload.");
				return;
			}

			var thumbnailField = _root.Q<ObjectField>("thumbnail-field");
			if (thumbnailField == null) {
				EditorUtility.DisplayDialog("Error", "Thumbnail field not found.", "Ok");
				Logger.LogError("Thumbnail field not found.");
				return;
			}

			var texture = thumbnailField.value as Texture2D;
			if (texture == null) {
				EditorUtility.DisplayDialog("Error", "Please select a thumbnail image.", "Ok");
				Logger.LogError("No thumbnail texture selected.");
				return;
			}

			// Validate texture format
			if (!texture.isReadable) {
				EditorUtility.DisplayDialog("Error", "Texture must be readable. Please check the texture import settings.", "Ok");
				Logger.LogError("Texture is not readable.");
				return;
			}

			// Additional validation: try to encode to test if it works
			try {
				var testData = texture.EncodeToPNG();
				if (testData == null || testData.Length == 0) {
					EditorUtility.DisplayDialog(
						"Error",
						"Texture cannot be encoded to PNG. This may be due to:\n" + "• Unsupported texture format\n" + "• Compressed texture that can't be read\n" + "• Non-power-of-2 dimensions on some platforms\n\n" + "Try:\n" + "• Setting texture format to 'RGBA32' or 'RGB24'\n" + "• Enabling 'Read/Write Enabled'\n" + "• Using power-of-2 dimensions", "Ok"
					);
					Logger.LogError("Texture encoding test failed - EncodeToPNG returned null.");
					return;
				}
			} catch (System.Exception ex) {
				EditorUtility.DisplayDialog(
					"Error",
					$"Texture encoding test failed: {ex.Message}\n\n" + "Please check texture import settings.", "Ok"
				);
				Logger.LogError($"Texture encoding test failed: {ex.Message}");
				return;
			}

			try {
				SetDisplay(DisplayFlags.Loading);
				Logger.Log("Uploading thumbnail...");

				var success = await Main.Instance.Network.UploadThumbnail(
					_avatar.GetId(),
					texture,
					_avatar.GetServerAddress(),
					progress => ShowProgress(progress, $"Uploading thumbnail... {progress * 100:F0}%")
				);
				if (success) {
					ShowProgress(1.0f, "Thumbnail uploaded successfully!");
					await UniTask.Delay(1000); // Show success message briefly
					Logger.Log("Thumbnail uploaded successfully.");
					// Refresh the thumbnail preview
					UpdateThumbnailPreview();
				} else {
					EditorUtility.DisplayDialog("Error", "Failed to upload thumbnail.", "Ok");
					Logger.LogError("Failed to upload thumbnail.");
				}
			} catch (Exception ex) {
				EditorUtility.DisplayDialog("Error", $"An error occurred while uploading thumbnail: {ex.Message}", "Ok");
				Logger.LogError($"An error occurred while uploading thumbnail: {ex.Message}");
			} finally {
				HideProgress();
				SetDisplay(DisplayFlags.Avatar | DisplayFlags.AvatarAsset);
			}
		}

		private async UniTask OnPublishAsync() {
			var descriptor = AvatarBuilderPanel.Descriptors.Length > 0 ? AvatarBuilderPanel.Descriptors[0] : null;
			if (!descriptor || descriptor == null) {
				ShowErrorDialog("No descriptor found.", useDirectMessage: true);
				Logger.LogError("No descriptor found.");
				return;
			}

			// Check if avatar is attached
			if (_avatar == null) {
				ShowErrorDialog("No avatar attached. Please attach an avatar before publishing.", useDirectMessage: true);
				Logger.LogError("No avatar attached.");
				return;
			}

			// Cache descriptor properties early to avoid accessing destroyed object later
			var descriptorName = descriptor.name;
			var target         = descriptor.target;

			// If platform is None, use the current build target
			if (target == Platform.None) {
				target = PlatformExtensions.CurrentPlatform;
				Logger.Log($"Platform is set to None, using current build target: {target.GetPlatformName()}");
			}

			if (!target.IsSupported()) {
				ShowErrorDialog($"{target.GetPlatformName()} is not supported.", useDirectMessage: true);
				Logger.LogError(
					$"Platform \"{target.GetPlatformName()}\" ({target.GetBuildTarget()}) is not supported."
				);
				return;
			}

			var assetVersionField = _root.Q<UnsignedIntegerField>("asset-version");
			if (assetVersionField == null) {
				ShowErrorDialog("Asset version field not found.", useDirectMessage: true);
				Logger.LogError("Asset version field not found.");
				return;
			}

			var version = (ushort)assetVersionField.value;

			Logger.Log("Checking avatar...");
			ShowProgress(0f, "Verifying avatar...");

			_avatar = await Main.Instance.Network.Fetch(_avatar.GetId(), _avatar.GetServerAddress());
			if (_avatar == null) {
				HideProgress();
				ShowErrorDialog("An error occured while fetching the avatar.", useDirectMessage: true);
				Logger.LogError("An error occured while fetching the avatar.");
				SetDisplay(DisplayFlags.AvatarNotFound);
				return;
			}

			var config        = Config.Load();
			var autoVersion   = config.Get("sdk.auto_version", true);
			var strictVersion = config.Get("sdk.strict_version", true);

			ShowProgress(0.05f, "Checking asset versions...");

			// Create temporary build path
			var    tempBuildPath = CreateTempBuildPath();
			string builtFilePath = null;

			try {
				Logger.Log("Building avatar...");
				ShowProgress(0.2f, "Building avatar...");

				// Validate descriptor still exists before building
				if (!descriptor || descriptor == null) {
					HideProgress();
					ShowErrorDialog("Descriptor was destroyed during build process.", useDirectMessage: true);
					Logger.LogError("Descriptor was destroyed during build process.");
					SetDisplay(DisplayFlags.Avatar | DisplayFlags.AvatarAsset);
					return;
				}

				// Create build data
				var buildData = new BuildData {
					Descriptor       = descriptor,
					Target           = target,
					OutputPath       = tempBuildPath,
					Filename         = descriptorName + "_" + version + ".nox",
					ShowDialog       = false, // We'll handle the dialog ourselves
					ProgressCallback = (progress, status) => ShowProgress(0.2f + (progress * 0.5f), status)
				};

				// Start the build process
				Logger.Log($"Building avatar '{descriptorName}'");
				var result = await Builder.Build(buildData);
				Logger.Log($"Build result: {result.Type}, Message: {result.Message}");

				// Handle build result
				if (result.Type != BuildResultType.Success) {
					HideProgress();
					string errorMessage = result.Type switch {
						BuildResultType.Failed            => $"Build failed: {result.Message}",
						BuildResultType.AlreadyBuilding   => "A build is already in progress.",
						BuildResultType.EditorCompiling   => "Unity is currently compiling scripts.",
						BuildResultType.InvalidTarget     => "Invalid build target specified.",
						BuildResultType.UnsupportedTarget => $"Build target {target} is not supported.",
						BuildResultType.InvalidGameObject => "The avatar is not valid for building.",
						_                                 => $"Unknown build error: {result.Type}"
					};
					ShowErrorDialog(errorMessage, useDirectMessage: true);
					Logger.LogError(errorMessage);
					SetDisplay(DisplayFlags.Avatar | DisplayFlags.AvatarAsset);
					return;
				}

				Logger.Log("Build completed successfully.");

				// Find the built file
				builtFilePath = Path.Join(buildData.OutputPath, buildData.Filename);
				if (!File.Exists(builtFilePath)) {
					HideProgress();
					ShowErrorDialog("Built file not found: " + builtFilePath, useDirectMessage: true);
					Logger.LogError("Built file not found: " + builtFilePath);
					SetDisplay(DisplayFlags.Avatar | DisplayFlags.AvatarAsset);
					return;
				}

				Logger.Log($"Built file: {builtFilePath}");

				ShowProgress(0.75f, "Preparing file for upload...");
				Logger.Log("Uploading avatar file...");

				// Read the built file as byte array
				var fileData   = File.ReadAllBytes(builtFilePath);
				var fileSizeMB = fileData.Length / (1024.0 * 1024.0);
				Logger.Log($"File size: {fileSizeMB:F2} MB");

				ShowProgress(0.78f, $"Calculating file hash for {fileSizeMB:F1} MB file...");

				// Calculate file hash for validation
				string fileHash = null;
				using (var sha256 = System.Security.Cryptography.SHA256.Create()) {
					var hashBytes = sha256.ComputeHash(fileData);
					fileHash = System.BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
				}

				Logger.Log($"File hash: {fileHash}");
				ShowProgress(0.8f, $"Starting upload of {fileSizeMB:F1} MB file...");

				// Check if we need to create a new asset or use existing one
				var search = await Main.Instance.Network.SearchAssets(
					_avatar.GetId(), new AssetSearchRequest {
						Versions  = new[] { version },
						Platforms = new[] { target.GetPlatformName() },
						Engines   = new[] { Constants.CurrentEngine.GetEngineName() },
						ShowEmpty = true,
						Limit     = 1,
						Offset    = 0
					}, _avatar.GetServerAddress()
				);

				var asset = search?.GetAssets().FirstOrDefault();

				if (asset == null) {
					ShowProgress(0.82f, "Creating new asset entry...");
					asset = await Main.Instance.Network.CreateAsset(
						_avatar.GetId(), new CreateAssetRequest {
							Version  = version,
							Engine   = Constants.CurrentEngine.GetEngineName(),
							Platform = target.GetPlatformName()
						}, _avatar.GetServerAddress()
					);
				} else {
					ShowProgress(0.82f, "Using existing asset entry...");
				}

				if (asset == null) {
					HideProgress();
					ShowErrorDialog("Failed to create or find asset entry.", useDirectMessage: true);
					Logger.LogError("Failed to create or find asset entry.");
					SetDisplay(DisplayFlags.Avatar | DisplayFlags.AvatarAsset);
					return;
				}

				// Upload the file directly to the asset (without updating avatar info)
				var uploadSuccess = await Main.Instance.Network.UploadAssetFile(
					_avatar.GetId(),
					asset.GetId(),
					fileData,
					_avatar.GetServerAddress(),
					onProgress: progress => {
						var totalSize = fileData.Length / (1024.0 * 1024.0); // Size in MB
						var sizeUploaded = progress * totalSize;
						ShowProgress(0.85f + progress * 0.15f, $"Uploading... {sizeUploaded:F2} MB / {totalSize:F2} MB - {progress * 100:F0}%");
					}
				);

				if (!uploadSuccess) {
					HideProgress();
					ShowErrorDialog("Failed to upload avatar file.", useDirectMessage: true);
					Logger.LogError("Failed to upload avatar file.");
					SetDisplay(DisplayFlags.Avatar | DisplayFlags.AvatarAsset);
					return;
				}

				ShowProgress(1.0f, "Upload completed!");
				Logger.Log("Avatar uploaded successfully.");

				// Update version in descriptor - check if it still exists
				if (descriptor && descriptor != null) {
					descriptor.publishVersion = version;
					EditorUtility.SetDirty(descriptor);
				} else {
					Logger.LogWarning("Cannot update descriptor version - descriptor was destroyed during publish.");
				}

				// Hide progress bar after 3 seconds
				HideProgressAfterDelay(3000).Forget();
				SetDisplay(DisplayFlags.Avatar | DisplayFlags.AvatarAsset);
			} catch (Exception ex) {
				HideProgress();
				ShowErrorDialog($"An error occurred during publish: {ex.Message}", useDirectMessage: true);
				Logger.LogError($"An error occurred during publish: {ex.Message}");
				SetDisplay(DisplayFlags.Avatar | DisplayFlags.AvatarAsset);
			} finally {
				// Cleanup temporary build path
				CleanupTempPath(tempBuildPath);
			}
		}

		private void ShowProgress(float progress, string status) {
			if (_progressContainer != null)
				_progressContainer.style.display = DisplayStyle.Flex;
			if (_progressBar != null)
				_progressBar.value = progress * 100f;
			if (_progressLabel != null)
				_progressLabel.text = status;
			_publishButton?.SetEnabled(false);
		}

		private void HideProgress() {
			if (_progressContainer != null) {
				_progressContainer.style.display = DisplayStyle.None;
			}

			// Re-enable the publish button
			if (_publishButton != null) {
				_publishButton.SetEnabled(true);
			}
		}

		private async UniTask HideProgressAfterDelay(int delayMs) {
			await UniTask.Delay(delayMs);
			HideProgress();
		}

		private void ShowErrorDialog(string messageKey, params object[] args)
			=> ShowErrorDialog(messageKey, false, args);

		private void ShowErrorDialog(string messageKey, bool useDirectMessage, params object[] args) {
			var message = useDirectMessage ? messageKey : LanguageManager.Get(messageKey, args);
			EditorUtility.DisplayDialog(
				LanguageManager.Get("avatar.publisher.error"),
				message,
				LanguageManager.Get("avatar.publisher.ok")
			);
		}

		private void ShowSuccessDialog(string messageKey, params object[] args)
			=> ShowSuccessDialog(messageKey, false, args);

		private void ShowSuccessDialog(string messageKey, bool useDirectMessage, params object[] args)
			=> EditorUtility.DisplayDialog(
				LanguageManager.Get("avatar.publisher.success"),
				useDirectMessage ? messageKey : LanguageManager.Get(messageKey, args),
				LanguageManager.Get("avatar.publisher.ok")
			);

		private string CreateTempBuildPath() {
			var tempDir = Path.Combine(Path.GetTempPath(), "NoxAvatarBuild", System.Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);
			return tempDir.Replace('\\', '/') + "/";
		}

		private void CleanupTempPath(string tempPath) {
			try {
				if (!string.IsNullOrEmpty(tempPath) && Directory.Exists(tempPath)) {
					Directory.Delete(tempPath, true);
					Logger.Log($"Cleaned up temporary build directory: {tempPath}");
				}
			} catch (Exception ex) {
				Logger.LogError($"Failed to cleanup temporary directory {tempPath}: {ex.Message}");
			}
		}
	}
}
#endif