#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using api.nox.avatar.builder;
using Cysharp.Threading.Tasks;
using Nox.CCK.Avatars;
using Nox.CCK.Language;
using Nox.CCK.Mods.Panels;
using Nox.CCK.Utils;
using Nox.Users;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;

namespace api.nox.avatar.editor {
	public class AvatarBuilderPanel : IEditorPanelBuilder, IDisposable {
		
		public string GetId()
			=> "builder";

		public string GetName()
			=> "Avatar/Builder";

		public string GetTitle()
			=> LanguageManager.Get("avatar.builder.title");

		public bool IsHidden()
			=> false;

		private          string        _lastHashNotify = "";
		private readonly VisualElement _root           = new();
		private          ObjectField   _descriptorField;
		private          EnumField     _platformField;
		private          TextField     _outputFolderField;
		private          Button        _buildButton;
		private          VisualElement _progressContainer;
		private          ProgressBar   _progressBar;
		private          Label         _progressLabel;
		private          VisualElement _notificationList;

		// Cache for commonly used strings
		private static readonly Dictionary<string, string> NotificationIds = new() {
			["NoAvatarDescriptor"]        = "NoAvatarDescriptor",
			["MultipleAvatarDescriptors"] = "MultipleAvatarDescriptors",
			["NoUser"]                    = "NoUser",
			["User"]                      = "User",
			["NoSpawns"]                  = "NoSpawns",
			["PlayMode"]                  = "PlayMode",
			["UseActivePlatform"]         = "UseActivePlatform",
			["UnsupportedPlatform"]       = "UnsupportedPlatform",
			["PlatformMismatch"]          = "PlatformMismatch"
		};


		public string OutputFolder {
			get => Config.LoadEditor().Get("avatar.builder.output_folder", "");
			set {
				var config = Config.LoadEditor();
				config.Set("avatar.builder.output_folder", value);
				config.Save();
			}
		}


		public VisualElement Make(Dictionary<string, object> data) {
			NotificationManager.Clear();
			_lastHashNotify = "";
			_root.ClearBindings();
			_root.Clear();

			// S'abonner aux événements de l'AvatarEditorHelper
			AvatarEditorHelper.OnAvatarSelected.AddListener(OnAvatarSelected);

			var child = Editor.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("builder.uxml").CloneTree();
			child.style.flexGrow = 1;
			_root.Add(child);

			// Cache UI elements
			_descriptorField   = _root.Q<ObjectField>("descriptor-field");
			_platformField     = _root.Q<EnumField>("platform-field");
			_outputFolderField = _root.Q<TextField>("output-folder-field");
			_buildButton       = _root.Q<Button>("build-button");
			_progressContainer = _root.Q<VisualElement>("progress-container");
			_progressBar       = _root.Q<ProgressBar>("progress-bar");
			_progressLabel     = _root.Q<Label>("progress-label");
			var versionLabel         = _root.Q<Label>("version");
			var gotoPublisherButton  = _root.Q<Button>("goto-publisher");
			var detectPlatformButton = _root.Q<Button>("detect-platform");
			var browseOutputButton   = _root.Q<Button>("browse-output-button");

			// Initialize components
			if (versionLabel != null)
				versionLabel.text = "v" + Editor.CoreAPI.ModMetadata.GetVersion();

			// Utiliser l'avatar courant de l'AvatarEditorHelper
			var descriptor = AvatarEditorHelper.CurrentAvatar;

			if (_platformField != null) {
				_platformField.Init(descriptor?.target ?? Platform.None);
				_platformField.RegisterValueChangedCallback(OnPlatformChanged);
			}

			if (_descriptorField != null) {
				_descriptorField.value = descriptor;
				_descriptorField.RegisterValueChangedCallback(OnDescriptorFieldChanged);
			}

			// Initialize output folder field
			if (_outputFolderField != null) {
				_outputFolderField.value = OutputFolder;
				_outputFolderField.RegisterValueChangedCallback(OnOutputFolderChanged);
			}

			gotoPublisherButton?.RegisterCallback<ClickEvent>(OnGoto);
			detectPlatformButton?.RegisterCallback<ClickEvent>(OnDetectPlatformClicked);
			browseOutputButton?.RegisterCallback<ClickEvent>(OnBrowseOutputClicked);
			_buildButton?.RegisterCallback<ClickEvent>(OnBuildButtonClicked);

			return _root;
		}

		private void OnAvatarSelected(AvatarDescriptor newAvatar) {
			// Mettre à jour l'UI quand un nouvel avatar est sélectionné
			if (_descriptorField != null)
				_descriptorField.SetValueWithoutNotify(newAvatar);
			
			if (_platformField != null)
				_platformField.SetValueWithoutNotify(newAvatar?.target ?? Platform.None);
		}

		private void OnDescriptorFieldChanged(ChangeEvent<Object> e) {
			// Quand l'utilisateur change l'avatar dans le champ, mettre à jour l'AvatarEditorHelper
			if (e.newValue is AvatarDescriptor newDescriptor) {
				AvatarEditorHelper.CurrentAvatar = newDescriptor;
			}
		}

		private async UniTask OnBuildButtonClickedAsync() {
			// Utiliser l'avatar courant de l'AvatarEditorHelper
			var descriptor = AvatarEditorHelper.CurrentAvatar;

			if (!descriptor) {
				ShowErrorDialog("avatar.builder.error_no_descriptor");
				Logger.LogError("No avatar descriptor found.");
				return;
			}

			var target = descriptor.target;

			// If platform is None, use the current build target
			if (target == Platform.None) {
				target = PlatformExtensions.CurrentPlatform;
				Logger.Log($"Platform is set to None, using current build target: {target.GetPlatformName()}");
			}

			if (!target.IsSupported()) {
				ShowErrorDialog("avatar.builder.error_unsupported_target");
				Logger.LogError("Unsupported build target.");
				return;
			}

			// Get the output folder from the UI field
			var outputFolder = _outputFolderField?.value ?? "";
			if (string.IsNullOrEmpty(outputFolder)) {
				ShowErrorDialog("avatar.builder.error_no_output_folder");
				Logger.LogError("No output folder specified.");
				return;
			}

			// Ensure output folder ends with a slash
			if (!outputFolder.EndsWith("/") && !outputFolder.EndsWith("\\"))
				outputFolder += "/";

			// Convert to absolute path if it's relative
			var absoluteOutputPath = outputFolder;
			if (!Path.IsPathRooted(outputFolder)) {
				var projectPath = Path.GetDirectoryName(Application.dataPath) ?? "Assets/";
				absoluteOutputPath = Path.Combine(projectPath, outputFolder).Replace('\\', '/');
				if (!absoluteOutputPath.EndsWith("/"))
					absoluteOutputPath += "/";
			}

			Logger.Log($"Building avatar '{descriptor.name}' to output folder: {absoluteOutputPath}");

			// Show progress bar
			ShowProgress(0f, "Initializing build...");

			try {
				// Create build data
				var buildData = new BuildData {
					Descriptor       = descriptor,
					Target           = target,
					OutputPath       = absoluteOutputPath,
					ShowDialog       = false, // We'll handle the dialog ourselves
					ProgressCallback = ShowProgress
				};

				// Start the build process
				Logger.Log($"Building avatar '{descriptor.name}'");
				var result = await Builder.Build(buildData);
				Logger.Log($"Build result: {result.Type}, Message: {result.Message}");

				// Hide progress bar
				HideProgress();

				// Handle the result
				switch (result.Type) {
					case BuildResultType.Success:
						ShowSuccessDialog("avatar.builder.success_build");
						Logger.Log("Build completed successfully.");
						break;
					case BuildResultType.Failed:
						ShowErrorDialog(result.Message, useDirectMessage: true);
						Logger.LogError($"Build failed: {result.Message}");
						break;
					case BuildResultType.AlreadyBuilding:
						ShowErrorDialog("avatar.builder.error_already_building");
						Logger.LogWarning("A build is already in progress.");
						break;
					case BuildResultType.EditorCompiling:
						ShowErrorDialog("avatar.builder.error_editor_compiling");
						Logger.LogWarning("Unity is currently compiling scripts.");
						break;
					case BuildResultType.InvalidTarget:
						ShowErrorDialog("avatar.builder.error_invalid_target");
						Logger.LogError("Invalid build target specified.");
						break;
					case BuildResultType.UnsupportedTarget:
						ShowErrorDialog("avatar.builder.error_unsupported_target");
						Logger.LogError($"Build target {target} is not supported.");
						break;
					case BuildResultType.InvalidGameObject:
						ShowErrorDialog("avatar.builder.error_invalid_game_object");
						Logger.LogError("The scene is not valid for building.");
						break;
					default:
						ShowErrorDialog("avatar.builder.error_unknown");
						Logger.LogError($"Unknown build result: {result.Type}");
						break;
				}
			} catch (Exception e) {
				// Hide progress bar on exception
				HideProgress();
				ShowErrorDialog($"Build failed with exception: {e.Message}", useDirectMessage: true);
				Logger.LogError($"Build exception: {e}");
			}
		}

		private void ShowProgress(float progress, string status) {
			if (_progressContainer != null)
				_progressContainer.style.display = DisplayStyle.Flex;
			if (_progressBar != null)
				_progressBar.value = progress * 100f;
			if (_progressLabel != null)
				_progressLabel.text = status;
			_buildButton?.SetEnabled(false);
			Logger.Log($"Progress: {progress * 100f:F1}%, Status: {status}");
		}

		private void HideProgress() {
			if (_progressContainer != null)
				_progressContainer.style.display = DisplayStyle.None;

			// Re-enable the build button
			_buildButton?.SetEnabled(true);
		}

		private void OnDetectPlatformClicked(ClickEvent evt) {
			if (_platformField == null) return;
			_platformField.value = PlatformExtensions.CurrentPlatform;
		}

		private void OnBuildButtonClicked(ClickEvent _)
			=> OnBuildButtonClickedAsync().Forget();

		private void OnBrowseOutputClicked(ClickEvent evt) {
			var currentFolder  = _outputFolderField?.value ?? "";
			var selectedFolder = EditorUtility.OpenFolderPanel("Select Output Folder", currentFolder, "");

			if (!string.IsNullOrEmpty(selectedFolder) && _outputFolderField != null)
				_outputFolderField.value = selectedFolder;
		}

		private void ShowErrorDialog(string messageKey, bool useDirectMessage, params object[] args) {
			var message = useDirectMessage ? messageKey : LanguageManager.Get(messageKey, args);
			EditorUtility.DisplayDialog(
				LanguageManager.Get("avatar.builder.error"),
				message,
				LanguageManager.Get("avatar.builder.ok")
			);
		}

		private void ShowSuccessDialog(string messageKey, params object[] args)
			=> EditorUtility.DisplayDialog(
				LanguageManager.Get("avatar.builder.success"),
				LanguageManager.Get(messageKey, args),
				LanguageManager.Get("avatar.builder.ok")
			);

		private void ShowErrorDialog(string messageKey, params object[] args)
			=> ShowErrorDialog(messageKey, false, args);

		private void OnPlatformChanged(ChangeEvent<Enum> e) {
			// Utiliser l'avatar courant de l'AvatarEditorHelper
			var mainDescriptor = AvatarEditorHelper.CurrentAvatar;
			if (!mainDescriptor) return;

			var plat = (Platform)e.newValue;
			// Allow Platform.None as a valid selection (no specific platform)
			if (plat != Platform.None && !plat.IsSupported()) {
				ShowErrorDialog("avatar.builder.error_platform_not_supported", plat.GetPlatformName());
				Logger.LogError($"Platform \"{plat.GetPlatformName()}\" ({plat.GetBuildTarget()}) is not supported.");
				_platformField?.SetValueWithoutNotify(e.previousValue ?? mainDescriptor.target);
				return;
			}

			// Update descriptor target
			mainDescriptor.target = plat;

			// Check if we should switch Unity's current platform
			var currentPlatform = PlatformExtensions.CurrentPlatform;
			if (plat != Platform.None && plat != currentPlatform && plat.IsSupported()) {
				if (EditorUtility.DisplayDialog(
					    "Switch Platform",
					    $"Do you want to switch Unity's build target from {currentPlatform.GetPlatformName()} to {plat.GetPlatformName()}?",
					    "Yes", "No"
				    )) {
					PlatformExtensions.CurrentPlatform = plat;
					Logger.Log($"Switched Unity build target to {plat.GetPlatformName()}");
				}
			}

			// Check for platform mismatch after changing the descriptor target
			CheckPlatformMismatch(mainDescriptor);
		}

		private void CheckPlatformMismatch(AvatarDescriptor descriptor) {
			// Skip check if descriptor is null or target is None
			if (!descriptor || descriptor.target == Platform.None) {
				RemoveNotificationIfExists(NotificationIds["PlatformMismatch"]);
				return;
			}

			var currentPlatform = PlatformExtensions.CurrentPlatform;
			var targetPlatform  = descriptor.target;

			// Check if current platform differs from target platform
			if (currentPlatform != targetPlatform) {
				SetNotificationWithActions(
					NotificationIds["PlatformMismatch"], NotificationType.Warning,
					"avatar.builder.platform_mismatch", new List<VisualElement> {
						new Button(
							() => {
								if (_platformField != null)
									_platformField.value = currentPlatform;
							}
						) { text = LanguageManager.Get("avatar.builder.use_current") },
						new Button(
							() => {
								// Switch Unity's build target to match the descriptor
								var targetBuildTarget = targetPlatform.GetBuildTarget();
								if (targetBuildTarget != BuildTarget.NoTarget)
									PlatformExtensions.CurrentPlatform = targetPlatform;
							}
						) { text = LanguageManager.Get("avatar.builder.switch_platform") }
					}, currentPlatform.GetPlatformName(), targetPlatform.GetPlatformName()
				);
			} else RemoveNotificationIfExists(NotificationIds["PlatformMismatch"]);
		}

		private static void RemoveNotificationIfExists(string uid) {
			if (!NotificationManager.Has(uid)) return;
			NotificationManager.Remove(uid);
		}

		private static void SetNotificationWithActions(string uid, NotificationType type, string messageKey, List<VisualElement> actions, params object[] args) {
			if (NotificationManager.Has(uid)) return;
			NotificationManager.Set(
				new Notification {
					Uid     = uid,
					Type    = type,
					Content = new Label(LanguageManager.Get(messageKey, args)),
					Actions = actions
				}
			);
		}

		public VisualElement[] GetHeaders() {
			var button = new Button { text = "Publisher" };
			button.AddToClassList("nox-transparent");
			button.RegisterCallback<ClickEvent>(OnGoto);
			return new VisualElement[] { button };
		}

		private void OnGoto(ClickEvent evt) {
			Editor.CoreAPI.PanelAPI.SetActivePanel(Main.Instance.CoreAPI.ModMetadata.GetId() + ".publisher");
			evt.StopPropagation();
		}

		private void OnOutputFolderChanged(ChangeEvent<string> e) {
			var path           = e.newValue;
			var normalizedPath = NormalizePath(path);
			if (normalizedPath != path && _outputFolderField != null)
				_outputFolderField.SetValueWithoutNotify(normalizedPath);
			OutputFolder = normalizedPath;
			Logger.Log($"Output folder updated: {normalizedPath}");
		}

		private static string NormalizePath(string path) {
			if (string.IsNullOrEmpty(path))
				return path;

			// Convert to forward slashes for consistency
			path = path.Replace('\\', '/');

			// Get project root path (parent of Assets folder)
			var projectPath = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
			if (string.IsNullOrEmpty(projectPath))
				return path;

			// If path is absolute and starts with project path, make it relative
			if (Path.IsPathRooted(path)) {
				// Ensure project path ends with /
				if (!projectPath.EndsWith("/"))
					projectPath += "/";

				// Check if path is within the project
				if (path.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
					path = path[projectPath.Length..];
			}

			// Ensure the path doesn't start with / or ./
			path = path.TrimStart('/', '.');
			if (path.StartsWith("/"))
				path = path[1..];

			// If the path is within Assets, ensure it starts with "Assets/"
			if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) || path.Equals("Assets", StringComparison.OrdinalIgnoreCase)) {
				// Path is already correctly formatted
				return path;
			}

			// Check if this is actually the Assets folder or a subfolder
			var assetsPath = Application.dataPath.Replace('\\', '/');
			var fullPath   = Path.GetFullPath(Path.Combine(projectPath, path)).Replace('\\', '/');

			if (fullPath.StartsWith(assetsPath, StringComparison.OrdinalIgnoreCase)) {
				// This path is within Assets, so normalize it
				var relativePath = fullPath[assetsPath.Length..].TrimStart('/');
				return string.IsNullOrEmpty(relativePath) ? "Assets" : $"Assets/{relativePath}";
			}

			return path;
		}

		internal static AvatarDescriptor[] Descriptors {
			get {
				if (AvatarEditorHelper.CurrentAvatar != null) {
					return new[] { AvatarEditorHelper.CurrentAvatar };
				}
				return ComponentExtension.GetComponentsInChildren<AvatarDescriptor>();
			}
		}

		public void Dispose() {
			// Se désabonner des événements
			AvatarEditorHelper.OnAvatarSelected.RemoveListener(OnAvatarSelected);
			
			// Clear cached references
			_descriptorField   = null;
			_platformField     = null;
			_outputFolderField = null;
			_progressContainer = null;
			_progressBar       = null;
			_progressLabel     = null;
			_buildButton       = null;

			// Clear notifications and UI
			NotificationManager.Clear();
			_root.Clear();
			_root.ClearBindings();
			_lastHashNotify = "";
		}

		internal void Update() {
			if (!Editor.HasOnePanelOpened() || _root.childCount == 0) return;

			// Utiliser l'avatar courant de l'AvatarEditorHelper
			var descriptor = AvatarEditorHelper.CurrentAvatar;
			var user        = Main.Instance.UserAPI.GetCurrent();

			// Check if a scene has a avatar descriptor
			if (!descriptor)
				SetNotificationWithActions(
					NotificationIds["NoAvatarDescriptor"], NotificationType.Error,
					"avatar.builder.no_descriptor", new List<VisualElement>()
				);
			else RemoveNotificationIfExists(NotificationIds["NoAvatarDescriptor"]);

			if (descriptor) {
				CheckMultipleDescriptors(Descriptors);
				CheckUserLogin(user);
			}

			CheckPlayMode();
			CheckBuildPlatform(descriptor);
			CheckUnsupportedPlatform(descriptor);
			CheckPlatformMismatch(descriptor);
			UpdateUI(descriptor);
		}

		private void CheckMultipleDescriptors(AvatarDescriptor[] descriptors) {
			if (descriptors.Length > 1)
				SetNotificationWithActions(
					NotificationIds["MultipleAvatarDescriptors"], NotificationType.Warning,
					"avatar.builder.multiple_descriptors", new List<VisualElement> {
						new Button(
							() => {
								var target = _descriptorField?.value;
								Selection.activeObject = target;
							}
						) { text = LanguageManager.Get("avatar.builder.select") },
						new Button(
							() => {
								for (var i = 1; i < descriptors.Length; i++)
									Object.DestroyImmediate(descriptors[i].gameObject);
							}
						) { text = LanguageManager.Get("avatar.builder.remove_other") }
					}
				);
			else RemoveNotificationIfExists(NotificationIds["MultipleAvatarDescriptors"]);
		}

		private void CheckUserLogin(IUser user) {
			if (user == null) {
				SetNotificationWithActions(
					NotificationIds["NoUser"], NotificationType.Warning,
					"avatar.builder.no_user", new List<VisualElement> {
						new Button(() => Editor.CoreAPI.PanelAPI.SetActivePanel("api.nox.user.login"))
							{ text = LanguageManager.Get("avatar.builder.login") }
					}
				);
				RemoveNotificationIfExists(NotificationIds["User"]);
				return;
			}

			RemoveNotificationIfExists(NotificationIds["NoUser"]);
			NotificationManager.Set(
				new Notification {
					Uid  = NotificationIds["User"],
					Type = NotificationType.Info,
					Content = new Label(
						LanguageManager.Get(
							"avatar.builder.logged_in",
							new object[] { user.GetDisplay() ?? user.GetUsername(), user.ToIdentifier().ToString() }
						)
					)
				}
			);
		}

		private void CheckBuildPlatform(AvatarDescriptor descriptor) {
			var buildPlatform = descriptor?.target ?? Platform.None;

			if (buildPlatform == Platform.None)
				SetNotificationWithActions(
					NotificationIds["UseActivePlatform"], NotificationType.Info,
					"avatar.builder.use_active_platform", new List<VisualElement> {
						new Button(
							() => {
								if (_platformField != null)
									_platformField.value = PlatformExtensions.CurrentPlatform;
							}
						) { text = LanguageManager.Get("avatar.builder.detect") }
					}, PlatformExtensions.CurrentPlatform.GetPlatformName()
				);
			else RemoveNotificationIfExists(NotificationIds["UseActivePlatform"]);
		}

		private void CheckPlayMode() {
			if (Application.isPlaying)
				SetNotificationWithActions(
					NotificationIds["PlayMode"], NotificationType.Error,
					"avatar.builder.play_mode", new List<VisualElement> {
						new Button(() => EditorApplication.isPlaying = false) {
							text = LanguageManager.Get("avatar.builder.exit")
						}
					}
				);
			else RemoveNotificationIfExists(NotificationIds["PlayMode"]);
		}

		private void CheckUnsupportedPlatform(AvatarDescriptor descriptor) {
			// Determine the platform to check
			var platformToCheck = descriptor?.target ?? Platform.None;

			// If platform is None, it's intentionally set to no specific platform, so don't show warnings
			if (platformToCheck == Platform.None) {
				RemoveNotificationIfExists(NotificationIds["UnsupportedPlatform"]);
				return;
			}

			// Check if the platform is supported
			if (!platformToCheck.IsSupported()) {
				SetNotificationWithActions(
					NotificationIds["UnsupportedPlatform"], NotificationType.Error,
					"avatar.builder.unsupported_platform", new List<VisualElement> {
						new Button(
							() => {
								// Try to find a supported platform and set it
								var supportedPlatforms = Enum.GetValues(typeof(Platform))
									.Cast<Platform>()
									.Where(p => p != Platform.None && p.IsSupported())
									.ToList();

								if (supportedPlatforms.Count > 0 && _platformField != null) {
									_platformField.value = supportedPlatforms.First();
								}
							}
						) { text = LanguageManager.Get("avatar.builder.use_supported") },
						new Button(
							() => {
								if (_platformField != null)
									_platformField.value = PlatformExtensions.CurrentPlatform;
							}
						) { text = LanguageManager.Get("avatar.builder.detect") }
					}, platformToCheck.GetPlatformName()
				);
			} else RemoveNotificationIfExists(NotificationIds["UnsupportedPlatform"]);
		}

		private void UpdateUI(AvatarDescriptor descriptor) {
			if (_root.childCount == 0) return;

			// Cache UI elements if not already cached
			_descriptorField  ??= _root.Q<ObjectField>("descriptor-field");
			_platformField    ??= _root.Q<EnumField>("platform-field");
			_notificationList ??= _root.Q<VisualElement>("notifications");

			// Update descriptor field
			if (_descriptorField != null)
				_descriptorField.value = descriptor;

			// Update platform field
			if (_platformField != null && descriptor?.target != (Platform)_platformField.value)
				_platformField.SetValueWithoutNotify(descriptor?.target ?? Platform.None);

			UpdateNotificationDisplay();
		}

		private void UpdateNotificationDisplay() {
			var notify = NotificationManager.Notifications;
			var hash   = string.Join(":", notify.Select(n => n.Uid));

			if (hash == _lastHashNotify || _notificationList == null) return;

			_notificationList.Clear();
			notify.Sort((a, b) => a.Type.CompareTo(b.Type));

			var child = Resources.Load<VisualTreeAsset>("nox.cck.notification");
			foreach (var notification in notify)
				CreateNotificationItem(notification, child);

			var hasErrors              = notify.Any(n => n.Type == NotificationType.Error);
			var disableOnErrorElements = _root.Query(null, "disable-on-error").ToList();
			foreach (var element in disableOnErrorElements)
				element.SetEnabled(!hasErrors);

			_lastHashNotify = hash;
		}

		private VisualElement CreateModernNotificationItem(Notification notificationManager, VisualElement item) {
			var content = item.Q<VisualElement>("content");
			// var actions = item.Q<VisualElement>("actions");
			content.Add(notificationManager.Content);
			item.AddToClassList($"notification-{notificationManager.Type.ToString().ToLowerInvariant()}");
			// actions.Clear();
			return item;
		}

		private void CreateNotificationItem(Notification notificationManager, VisualTreeAsset asset) {
			var item = CreateModernNotificationItem(notificationManager, asset.CloneTree());
			_notificationList.Add(item);
		}
	}
}
#endif
