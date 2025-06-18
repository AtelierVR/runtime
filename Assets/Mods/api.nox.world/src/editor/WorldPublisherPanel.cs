#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.world.network;
using Cysharp.Threading.Tasks;
using Nox.CCK.Worlds;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using UnityEngine.UIElements;
using Nox.CCK.Mods.Panels;
using Nox.CCK.Utils;

namespace api.nox.world {
	public class WorldPublisherPanel : IEditorPanelBuilder {
		public string GetId()
			=> "publisher";

		public string GetName()
			=> "World/Publisher";

		public bool IsHidden()
			=> false;

		public VisualElement[] GetHeaders() {
			var button = new Button { text = "Builder" };
			button.AddToClassList("nox-transparent");
			button.RegisterCallback<ClickEvent>(OnGoto);
			return new VisualElement[] { button };
		}

		private void OnGoto(ClickEvent evt) {
			Editor.CoreAPI.PanelAPI.SetActivePanel("api.nox.world.builder");
			evt.StopPropagation();
		}

		private readonly VisualElement _root = new();

		private DisplayFlags _displayFlags;
		private DisplayFlags _lastDisplay;
		private World        _world;

		private void SetDisplay(DisplayFlags flags)
			=> _displayFlags = flags;

		internal void Update() {
			if (!Editor.HasOnePanelOpened() || _root.childCount == 0) return;
			var user = Main.Instance.UserAPI.GetCurrent();
			if (_lastDisplay == DisplayFlags.NotLogged && user != null)
				OnLogged().Forget();
			if (_lastDisplay != DisplayFlags.NotLogged && user == null)
				SetDisplay(DisplayFlags.NotLogged);
			var descriptor = WorldBuilderPanel.Descriptors.Length > 0 ? WorldBuilderPanel.Descriptors[0] : null;

			var descriptorField = _root.Q<ObjectField>("descriptor-field");
			if (descriptorField != null)
				descriptorField.value = descriptor;

			var version      = descriptor?.publishVersion ?? 0;
			var assetVersion = _root.Q<UnsignedIntegerField>("asset-version");
			if (assetVersion != null && assetVersion.value != version)
				assetVersion.value = version;

			if (_lastDisplay == _displayFlags) return;
			SetDisplay(_displayFlags);
			_lastDisplay = _displayFlags;

			var worldNotFoundElement = _root.Q<VisualElement>("world-not-found");
			if (worldNotFoundElement != null)
				worldNotFoundElement.style.display = _displayFlags.HasFlag(DisplayFlags.WorldNotFound) ? DisplayStyle.Flex : DisplayStyle.None;

			var worldElement = _root.Q<VisualElement>("world");
			if (worldElement != null)
				worldElement.style.display = _displayFlags.HasFlag(DisplayFlags.World) ? DisplayStyle.Flex : DisplayStyle.None;

			var worldAssetElement = _root.Q<VisualElement>("world-asset");
			if (worldAssetElement != null)
				worldAssetElement.style.display = _displayFlags.HasFlag(DisplayFlags.WorldAsset) ? DisplayStyle.Flex : DisplayStyle.None;

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

		private async UniTask OnLogged() {
			var descriptor = WorldBuilderPanel.Descriptors.Length > 0 ? WorldBuilderPanel.Descriptors[0] : null;
			if (!descriptor) {
				SetDisplay(DisplayFlags.NoDescriptor);
				return;
			}

			SetDisplay(DisplayFlags.Loading);
			var worldId       = descriptor.publishId;
			var serverAddress = descriptor.publishServer;
			await AttachWorld(serverAddress, worldId);
		}


		private async UniTask<INoxObject> AttachWorld(string server, uint id, bool create = false) {
			var descriptor = WorldBuilderPanel.Descriptors.Length > 0 ? WorldBuilderPanel.Descriptors[0] : null;
			if (!descriptor) {
				SetDisplay(DisplayFlags.NoDescriptor);
				_world = null;
				return null;
			}

			SetDisplay(DisplayFlags.Loading);

			World world       = null;
			if (id > 0) world = await Main.Instance.Network.Fetch(id, server);

			if (world == null && create)
				world = await Main.Instance.Network.Create(new CreateWorldRequest { Id = id }, server);

			if (world != null) {
				var user          = Main.Instance.UserAPI.GetCurrent();
				var isContributor = user != null && world.GetContributorIds().Any(contributor => user.ToIdentifier().Equals(contributor));

				if (!isContributor) {
					EditorUtility.DisplayDialog(
						"Error",
						"You are not a contributor of this world, you cannot attach it.",
						"Ok"
					);
					Logger.LogError("You are not a contributor of this world, you cannot attach it.");
					SetDisplay(DisplayFlags.WorldNotFound);
					_world = null;
					return null;
				}
			}

			if (world == null) {
				if (create) {
					EditorUtility.DisplayDialog("Error", "An error occured while creating the world.", "Ok");
					Logger.LogError(
						"An error occured while creating the world, please check the server and your permissions."
					);
				}

				SetDisplay(DisplayFlags.WorldNotFound);
				_world = null;
				return null;
			}
			SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
		descriptor.publishId     = world.GetId();
		descriptor.publishServer = world.GetServerAddress();
		EditorUtility.SetDirty(descriptor);
		_world = world;
		UpdateWorld();
		return world;
		}

		private void UpdateWorld() {
			if (_root.childCount == 0) return;

			var serverField = _root.Q<TextField>("info-server");
			if (serverField != null)
				serverField.value = _world?.GetServerAddress() ?? "";

			var idField = _root.Q<UnsignedIntegerField>("info-id");
			if (idField != null)
				idField.value = _world?.GetId() ?? 0;

			var titleField = _root.Q<TextField>("info-title");
			if (titleField != null)
				titleField.value = _world?.GetTitle() ?? "";

			var descriptionField = _root.Q<TextField>("info-description");
			if (descriptionField != null)
				descriptionField.value = _world?.GetDescription() ?? "";

			var capacityField = _root.Q<UnsignedIntegerField>("info-capacity");
			if (capacityField != null)
				capacityField.value = _world?.GetCapacity() ?? 0;

			var tagsList = _root.Q<ListView>("info-tags");
			if (tagsList != null) {
				tagsList.makeItem    = () => new Label { style = { marginLeft = 4, marginRight = 4 } };
				tagsList.itemsSource = _world?.GetTags() ?? Array.Empty<string>();
				tagsList.bindItem = (e, i) => {
					var label = (Label)e;
					if (_world?.GetTags() != null && i < _world.GetTags().Length)
						label.text = _world.GetTags()[i];
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
			var thumbnailStatus = _root.Q<Label>("thumbnail-status");
			
			if (thumbnailPreview != null && thumbnailStatus != null) {
				if (_world != null) {
					// Try to download and display the current thumbnail from server
					DownloadAndDisplayThumbnail().Forget();
				} else {
					thumbnailStatus.text = "No world attached";
					thumbnailStatus.style.color = new StyleColor(new UnityEngine.Color(1f, 1f, 1f, 0.6f));
				}
			}
		}

	private async UniTask DownloadAndDisplayThumbnail() {
		var thumbnailPreview = _root.Q<VisualElement>("thumbnail-preview");
		var thumbnailStatus = _root.Q<Label>("thumbnail-status");
		var thumbnailImage = _root.Q<Image>("thumbnail-image");
		var thumbnailFixButton = _root.Q<Button>("thumbnail-fix-button");
		
		if (thumbnailPreview == null || thumbnailStatus == null || thumbnailImage == null || thumbnailFixButton == null || _world == null) return;

		try {
			var thumbnailUrl = _world.GetThumbnailUrl();
			
			if (!string.IsNullOrEmpty(thumbnailUrl)) {
				// Show loading state
				thumbnailStatus.text = "Loading thumbnail...";
				thumbnailPreview.RemoveFromClassList("thumbnail-error");
				thumbnailPreview.RemoveFromClassList("thumbnail-warning");
				thumbnailPreview.RemoveFromClassList("thumbnail-success");
				thumbnailPreview.AddToClassList("thumbnail-loading");
				
				thumbnailImage.style.display = DisplayStyle.None;
				thumbnailFixButton.style.display = DisplayStyle.None;
				thumbnailStatus.style.display = DisplayStyle.Flex;
				
				var texture = await Main.Instance.NetworkAPI.FetchTexture(thumbnailUrl);
				
				if (texture != null) {
					// Display the downloaded thumbnail
					thumbnailImage.image = texture;
					thumbnailImage.scaleMode = ScaleMode.ScaleToFit;
					thumbnailImage.style.width = 100;
					thumbnailImage.style.height = 100;
					thumbnailImage.style.maxWidth = 200;
					thumbnailImage.style.maxHeight = 200;
					
					thumbnailStatus.text = $"Current thumbnail - {texture.width}x{texture.height}";
					thumbnailPreview.RemoveFromClassList("thumbnail-error");
					thumbnailPreview.RemoveFromClassList("thumbnail-warning");
					thumbnailPreview.RemoveFromClassList("thumbnail-loading");
					thumbnailPreview.AddToClassList("thumbnail-success");
					
					thumbnailImage.style.display = DisplayStyle.Flex;
					thumbnailStatus.style.display = DisplayStyle.Flex;
				} else {
					thumbnailStatus.text = "Failed to load thumbnail";
					thumbnailPreview.RemoveFromClassList("thumbnail-success");
					thumbnailPreview.RemoveFromClassList("thumbnail-warning");
					thumbnailPreview.RemoveFromClassList("thumbnail-loading");
					thumbnailPreview.AddToClassList("thumbnail-error");
					
					thumbnailImage.style.display = DisplayStyle.None;
					thumbnailStatus.style.display = DisplayStyle.Flex;
				}
			} else {
				thumbnailStatus.text = "No thumbnail available";
				thumbnailPreview.RemoveFromClassList("thumbnail-error");
				thumbnailPreview.RemoveFromClassList("thumbnail-warning");
				thumbnailPreview.RemoveFromClassList("thumbnail-loading");
				thumbnailPreview.RemoveFromClassList("thumbnail-success");
				
				thumbnailImage.style.display = DisplayStyle.None;
				thumbnailFixButton.style.display = DisplayStyle.None;
				thumbnailStatus.style.display = DisplayStyle.Flex;
			}
		} catch (Exception ex) {
			Logger.LogError($"Failed to load thumbnail: {ex.Message}");
			thumbnailStatus.text = "Failed to load thumbnail";
			thumbnailPreview.RemoveFromClassList("thumbnail-success");
			thumbnailPreview.RemoveFromClassList("thumbnail-warning");
			thumbnailPreview.RemoveFromClassList("thumbnail-loading");
			thumbnailPreview.AddToClassList("thumbnail-error");
			
			thumbnailImage.style.display = DisplayStyle.None;
			thumbnailFixButton.style.display = DisplayStyle.None;
			thumbnailStatus.style.display = DisplayStyle.Flex;
		}
	}

		public VisualElement Make(Dictionary<string, object> data) {
			_root.ClearBindings();
			_root.Clear();
			var child = Editor.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("publisher.uxml").CloneTree();
			child.style.flexGrow = 1;
			_root.Add(child);

			var versionLabel = _root.Q<Label>("version");
			if (versionLabel != null)
				versionLabel.text = "v" + Editor.CoreAPI.ModMetadata.GetVersion();

			var assignedIcon = _root.Q<Image>("assigned-icon");
			if (assignedIcon != null)
				assignedIcon.image = Editor.CoreAPI.AssetAPI.GetAsset<Texture2D>("ui", "icons/warning.png");

			var descriptor    = WorldBuilderPanel.Descriptors.Length > 0 ? WorldBuilderPanel.Descriptors[0] : null;
			var platformField = _root.Q<EnumField>("platform-field");
			if (platformField != null)
				platformField.Init(descriptor?.target ?? Platform.None);

			var detectPlatformButton = _root.Q<Button>("detect-platform");
			if (detectPlatformButton != null)
				detectPlatformButton.clicked += () => {
					var platformFieldInner = _root.Q<EnumField>("platform-field");
					if (platformFieldInner != null)
						platformFieldInner.value = PlatformExtensions.GetCurrentTarget().GetPlatform();
				};

			var descriptorField = _root.Q<ObjectField>("descriptor-field");
			if (descriptorField != null)
				descriptorField.value = descriptor;
			if (platformField != null)
				platformField
					.RegisterValueChangedCallback(
						e => {
							var mainDescriptor = WorldBuilderPanel.Descriptors.Length > 0 ? WorldBuilderPanel.Descriptors[0] : null;
							if (!mainDescriptor) return;
							var plat = (Platform)e.newValue;
							if (plat.IsSupported())
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
				gotoBuilderButton.clicked += () => Editor.CoreAPI.PanelAPI.SetActivePanel("api.nox.world.builder");

			var gotoLoginButton = _root.Q<Button>("goto-login");
			if (gotoLoginButton != null)
				gotoLoginButton.clicked += () => Editor.CoreAPI.PanelAPI.SetActivePanel("api.nox.user.login");

			_lastDisplay = DisplayFlags.None;
			var attachId     = _root.Q<UnsignedIntegerField>("attach-id");
			var attachServer = _root.Q<TextField>("attach-server");
			var attachButton = _root.Q<Button>("attach-world");
			if (attachButton != null && attachId != null && attachServer != null) {
				attachButton.clicked += async () => {
					var id     = attachId.value;
					var server = attachServer.value;
					if (string.IsNullOrWhiteSpace(server))
						server = Main.Instance.UserAPI.GetCurrent().GetServerAddress();
					await AttachWorld(server, id, true);
				};
			}

			var fetchInfoButton = _root.Q<Button>("info-fetch");
			if (fetchInfoButton != null) {
				fetchInfoButton.clicked += async () => {
					if (_world == null) return;
					var e = await AttachWorld(_world.GetServerAddress(), _world.GetId());
					if (e == null)
						EditorUtility.DisplayDialog("Error", "An error occured while fetching the world.", "Ok");
				};
			}

			var updateInfoButton = _root.Q<Button>("info-update");
			if (updateInfoButton != null) {
				updateInfoButton.clicked += async () => {
					Logger.Log("Update" + _world);
					if (_world == null || !_lastDisplay.HasFlag(DisplayFlags.World)) return;
					Logger.Log("Update1" + _world);
					var titleField       = _root.Q<TextField>("info-title");
					var descriptionField = _root.Q<TextField>("info-description");
					var capacityField    = _root.Q<UnsignedIntegerField>("info-capacity");

					if (titleField == null || descriptionField == null || capacityField == null) return;

					var title       = titleField.value;
					var description = descriptionField.value;
					var capacity    = capacityField.value;
					if (capacity > ushort.MaxValue) {
						EditorUtility.DisplayDialog("Error", "Capacity must be less than " + ushort.MaxValue, "Ok");
						return;
					}

					SetDisplay(DisplayFlags.Loading);
					var success = await Main.Instance.Network.Update(
						_world.GetId(), new UpdateWorldRequest() {
							title       = title,
							description = description,
							capacity    = (ushort)capacity
						}, _world.GetServerAddress()
					);
					if (success != null) {
						_world = success;
						UpdateWorld();
						SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
					} else EditorUtility.DisplayDialog("Error", "An error occured while updating the world.", "Ok");
				};
			}

			var detachInfoButton = _root.Q<Button>("info-detach");
			if (detachInfoButton != null) {
				detachInfoButton.clicked += () => {
					if (_world == null || !_lastDisplay.HasFlag(DisplayFlags.World)) return;
					var target = WorldBuilderPanel.Descriptors.Length > 0 ? WorldBuilderPanel.Descriptors[0] : null;
					if (!target) return;				target.publishId     = 0;
				target.publishServer = "";
				EditorUtility.SetDirty(target);
				_world = null;
				UpdateWorld();
				
				// Clear thumbnail preview
				var thumbnailPreview = _root.Q<VisualElement>("thumbnail-preview");
				var thumbnailStatus = _root.Q<Label>("thumbnail-status");
				if (thumbnailPreview != null && thumbnailStatus != null) {
					thumbnailPreview.Clear();
					thumbnailPreview.Add(thumbnailStatus);
					thumbnailStatus.text = "No world attached";
				}
				
				SetDisplay(DisplayFlags.WorldNotFound);
				};
			}

			// var deleteInfoButton = _root.Q<Button>("info-delete");
			// deleteInfoButton.clicked += async () =>
			// {
			//     if (_world == null || !_lastDisplay.HasFlag(DisplayFlags.World)) return;
			//     var confirm = EditorUtility.DisplayDialog("Delete World", "Are you sure you want to delete this world?", "Yes", "No");
			//     if (!confirm) return;
			//     SetDisplay(DisplayFlags.Loading);
			//     // var success = await WorldEditor._api.NetworkAPI.WorldAPI.DeleteWorld(_world.server, _world.id);
			//     // if (success)
			//     // {
			//     //     _world = null;
			//     //     UpdateWorld();
			//     //     SetDisplay(DisplayFlags.WorldNotFound);
			//     //     EditorUtility.DisplayDialog("Success", "World deleted successfully.", "Ok");
			//     // }
			//     // else
			//     // {
			//     //     EditorUtility.DisplayDialog("Error", "An error occured while deleting the world.", "Ok");
			//     //     SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
			//     // }
			//     throw new NotImplementedException();
			// };
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
							var target = WorldBuilderPanel.Descriptors.Length > 0 ? WorldBuilderPanel.Descriptors[0] : null;
							if (!target) return;
							target.publishVersion = (ushort)e.newValue;
							EditorUtility.SetDirty(target);
						}
					);
			}

			var publishButton = _root.Q<Button>("publish-button");
			if (publishButton != null)
				publishButton.clicked += () => OnPublishAsync().Forget();

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
			UpdateWorld();
			return _root;
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
		var thumbnailPreview = _root.Q<VisualElement>("thumbnail-preview");
		var thumbnailStatus = _root.Q<Label>("thumbnail-status");
		var thumbnailImage = _root.Q<Image>("thumbnail-image");
		var thumbnailFixButton = _root.Q<Button>("thumbnail-fix-button");
		
		if (thumbnailPreview == null || thumbnailStatus == null || thumbnailImage == null || thumbnailFixButton == null) return;

		// Hide all elements initially
		thumbnailStatus.style.display = DisplayStyle.None;
		thumbnailImage.style.display = DisplayStyle.None;
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
				
				thumbnailStatus.style.display = DisplayStyle.Flex;
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
					
					thumbnailStatus.style.display = DisplayStyle.Flex;
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
			
			thumbnailImage.style.display = DisplayStyle.Flex;
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
			importer.isReadable = true;
			importer.textureType = TextureImporterType.Default;
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			
			// Apply the changes
			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			AssetDatabase.Refresh();

			// Test if the texture can now be encoded
			var testData = texture.EncodeToPNG();
			if (testData == null || testData.Length == 0) {
				EditorUtility.DisplayDialog("Warning", 
					$"Texture '{texture.name}' is now readable but still cannot be encoded to PNG.\n" +
					"You may need to manually adjust the texture format in import settings.", "Ok");
			}
			
			// Refresh the preview
			UpdateThumbnailPreviewWithTexture(texture);
			
			Logger.Log($"Made texture '{texture.name}' readable for thumbnail upload.");
			
		} catch (System.Exception ex) {
				EditorUtility.DisplayDialog("Error", 
					$"Failed to make texture readable: {ex.Message}", "Ok");
				Logger.LogError($"Failed to make texture readable: {ex.Message}");
			}
		}

		private async UniTask OnThumbnailUploadAsync() {
			if (_world == null) {
				EditorUtility.DisplayDialog("Error", "No world attached.", "Ok");
				Logger.LogError("No world attached for thumbnail upload.");
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
				EditorUtility.DisplayDialog("Error", 
					"Texture cannot be encoded to PNG. This may be due to:\n" +
					"• Unsupported texture format\n" +
					"• Compressed texture that can't be read\n" +
					"• Non-power-of-2 dimensions on some platforms\n\n" +
					"Try:\n" +
					"• Setting texture format to 'RGBA32' or 'RGB24'\n" +
					"• Enabling 'Read/Write Enabled'\n" +
					"• Using power-of-2 dimensions", "Ok");
				Logger.LogError("Texture encoding test failed - EncodeToPNG returned null.");
				return;
			}
		} catch (System.Exception ex) {
			EditorUtility.DisplayDialog("Error", 
				$"Texture encoding test failed: {ex.Message}\n\n" +
				"Please check texture import settings.", "Ok");
			Logger.LogError($"Texture encoding test failed: {ex.Message}");
			return;
		}

			try {
				SetDisplay(DisplayFlags.Loading);
				Logger.Log("Uploading thumbnail...");

				var success = await Main.Instance.Network.UploadThumbnail(_world.GetId(), texture, _world.GetServerAddress());
				
				if (success) {
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
				SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
			}
		}

		private async UniTask OnPublishAsync() {
			var descriptor = WorldBuilderPanel.Descriptors.Length > 0 ? WorldBuilderPanel.Descriptors[0] : null;
			if (!descriptor) {
				EditorUtility.DisplayDialog("Error", "No descriptor found.", "Ok");
				Logger.LogError("No descriptor found.");
				return;
			}

			var target = descriptor.target;
			if (!target.IsSupported()) {
				EditorUtility.DisplayDialog("Error", $"{target.GetPlatformName()} is not supported.", "Ok");
				Logger.LogError(
					$"Platform \"{target.GetPlatformName()}\" ({target.GetBuildTarget()}) is not supported."
				);
				return;
			}

			var assetVersionField = _root.Q<UnsignedIntegerField>("asset-version");
			if (assetVersionField == null) {
				EditorUtility.DisplayDialog("Error", "Asset version field not found.", "Ok");
				Logger.LogError("Asset version field not found.");
				return;
			}

			var version = (ushort)assetVersionField.value;

			if (version >= ushort.MaxValue) {
				EditorUtility.DisplayDialog("Error", "Version must be less than " + ushort.MaxValue, "Ok");
				Logger.LogError("Version must be less than "                      + ushort.MaxValue);
				return;
			}

			Logger.Log("Checking world...");
			SetDisplay(DisplayFlags.Loading);
			_world = await Main.Instance.Network.Fetch(_world.GetId(), _world.GetServerAddress());
			if (_world == null) {
				EditorUtility.DisplayDialog("Error", "An error occured while fetching the world.", "Ok");
				Logger.LogError("An error occured while fetching the world.");
				SetDisplay(DisplayFlags.WorldNotFound);
				return;
			}

			var config        = Config.Load();
			var autoVersion   = config.Get("sdk.auto_version", true);
			var strictVersion = config.Get("sdk.strict_version", true);

			var search = await Main.Instance.Network.SearchAssets(
				_world.GetId(), new AssetSearchRequest {
					versions   = new[] { version },
					platforms  = new[] { target.GetPlatformName() },
					engines    = new[] { Constants.CurrentEngine.GetEngineName() },
					show_empty = true,
					limit      = 1,
					offset     = 0
				}, _world.GetServerAddress()
			);

			if (search == null) {
				EditorUtility.DisplayDialog("Error", "An error occured while fetching the assets.", "Ok");
				Logger.LogError("An error occured while fetching the assets.");
				SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
				return;
			}

			var asset = search.GetAssets().FirstOrDefault();
			if (asset != null && autoVersion && !asset.IsEmpty())
				while (asset != null && !asset.IsEmpty()) {
					version++;
					search = await Main.Instance.Network.SearchAssets(
						_world.GetId(), new AssetSearchRequest {
							versions   = new[] { version },
							platforms  = new[] { target.GetPlatformName() },
							engines    = new[] { Constants.CurrentEngine.GetEngineName() },
							show_empty = true,
							limit      = 1,
							offset     = 0
						}, _world.GetServerAddress()
					);
					if (search == null) {
						EditorUtility.DisplayDialog("Error", "An error occured while fetching the assets.", "Ok");
						Logger.LogError("An error occured while fetching the assets.");
						SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
						return;
					}

					asset = search.GetAssets().FirstOrDefault();
				}

			_root.Q<UnsignedIntegerField>("asset-version").value = version;
			if (asset != null && strictVersion && !asset.IsEmpty()) {
				EditorUtility.DisplayDialog("Error", "Asset already exists.", "Ok");
				Logger.LogError("Asset already exists.");
				Logger.LogError("Asset: " + asset);
				SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
				return;
			}

			Logger.Log("Building world...");
			// var result = MainDescriptorEditor.BuildWorld(descriptor, target.GetBuildTarget(), false);
			// if (result is not { Success: true } || string.IsNullOrWhiteSpace(result.path)) {
			// EditorUtility.DisplayDialog("Error", "An error occured while building the world.", "Ok");
			// Logger.LogError("An error occured while building the world.");
			// SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
			// return;
			// }
			EditorUtility.DisplayDialog("Error", "Not implemented yet.", "Ok");
			SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
			return;

			Logger.Log("Uploading asset...");
			Logger.Log("Asset version: " + version);
			Logger.Log($"Asset platform: {target.GetPlatformName()} ({target.GetBuildTarget()})");

			asset ??= await Main.Instance.Network.CreateAsset(
				_world.GetId(), new CreateAssetRequest {
					Version  = version,
					Engine   = Constants.CurrentEngine.GetEngineName(),
					Platform = target.GetPlatformName()
				}, _world.GetServerAddress()
			);

			if (asset == null) {
				EditorUtility.DisplayDialog("Error", "An error occured while creating the asset.", "Ok");
				Logger.LogError("An error occured while creating the asset.");
				SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
				return;
			}

			Logger.Log("Asset created successfully.");
			SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
		}
	}

	[Flags]
	public enum DisplayFlags {
		None          = 0,
		NotLogged     = 1,
		WorldNotFound = 2,
		World         = 4,
		WorldAsset    = 8,
		Loading       = 16,
		NoDescriptor  = 32
	}
}
#endif