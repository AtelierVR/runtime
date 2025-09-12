#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using api.nox.world.builder;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Panels;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using Nox.Users;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;

namespace api.nox.world {
	public class WorldBuilderPanel : IEditorPanelBuilder, IDisposable {
		public string GetId()
			=> "builder";

		public string GetName()
			=> "World/Builder";

		public string GetTitle()
			=> LanguageManager.Get("world.builder.title");

		public bool IsHidden()
			=> false;

		public VisualElement[] GetHeaders() {
			var button = new Button { text = "Publisher" };
			button.AddToClassList("nox-transparent");
			button.RegisterCallback<ClickEvent>(OnGoto);
			return new VisualElement[] { button };
		}

		private void OnGoto(ClickEvent evt) {
			Editor.CoreAPI.PanelAPI.SetActivePanel("api.nox.world.publisher");
			evt.StopPropagation();
		}

		private          string        _lastHashNotify = "";
		private readonly VisualElement _root           = new();

		// Cache for UI elements to avoid repeated queries
		private ObjectField   _descriptorField;
		private EnumField     _platformField;
		private TextField     _outputFolderField;
		private VisualElement _notificationList;
		private VisualElement _progressContainer;
		private ProgressBar   _progressBar;
		private Label         _progressLabel;
		private Button        _buildButton;

		// Cache for commonly used strings
		private static readonly Dictionary<string, string> _notificationIds = new() {
			["NoWorldDescriptor"]        = "NoWorldDescriptor",
			["MultipleWorldDescriptors"] = "MultipleWorldDescriptors",
			["NoUser"]                   = "NoUser",
			["User"]                     = "User",
			["NoNetworkObjects"]         = "NoNetworkObjects",
			["NoSpawns"]                 = "NoSpawns",
			["NoScenes"]                 = "NoScenes",
			["PlayMode"]                 = "PlayMode",
			["UseActivePlatform"]        = "UseActivePlatform",
			["UnsupportedPlatform"]      = "UnsupportedPlatform",
			["PlatformMismatch"]         = "PlatformMismatch"
		};

		internal static WorldDescriptor[] Descriptors
			=> ComponentExtension.GetComponentsInChildren<WorldDescriptor>();

		// Helper methods for notification management
		private static void SetNotificationIfNotExists(string uid, NotificationType type, string messageKey, params object[] args) {
			if (!NotificationManager.Has(uid))
				NotificationManager.Set(
					new Notification {
						Uid     = uid,
						Type    = type,
						Content = new Label(LanguageManager.Get(messageKey, args))
					}
				);
		}

		private static void SetNotificationWithActions(string uid,     NotificationType type, string messageKey,
			List<VisualElement>                               actions, params object[]  args) {
			if (!NotificationManager.Has(uid))
				NotificationManager.Set(
					new Notification {
						Uid     = uid,
						Type    = type,
						Content = new Label(LanguageManager.Get(messageKey, args)),
						Actions = actions
					}
				);
		}

		private static void RemoveNotificationIfExists(string uid) {
			if (NotificationManager.Has(uid))
				NotificationManager.Remove(uid);
		}

		private Button CreateNormalizeButton(Action normalizeAction)
			=> new Button(normalizeAction) { text = LanguageManager.Get("world.builder.normalize") };

		internal void Update() {
			if (!Editor.HasOnePanelOpened() || _root.childCount == 0) return;

			var descriptors = Descriptors;
			var descriptor  = descriptors.Length > 0 ? descriptors[0] : null;
			var user        = Main.Instance.UserAPI.GetCurrent();

			// Check if a scene has a world descriptor
			if (!descriptor)
				SetNotificationWithActions(
					_notificationIds["NoWorldDescriptor"], NotificationType.Error,
					"world.builder.no_descriptor", new List<VisualElement> {
						new Button(WorldDescriptorExtension.MakeMainSceneDescriptor)
							{ text = LanguageManager.Get("world.builder.create") }
					}
				);
			else RemoveNotificationIfExists(_notificationIds["NoWorldDescriptor"]);

			if (descriptor) {
				CheckMultipleDescriptors(descriptors);
				CheckUserLogin(user);
				// CheckNetworkObjects(descriptor);
				CheckSpawns(descriptor);
				CheckScenes(descriptor);
			}

			CheckPlayMode();
			CheckBuildPlatform(descriptor);
			CheckUnsupportedPlatform(descriptor);
			CheckPlatformMismatch(descriptor);
			UpdateUI(descriptor);
		}

		private void CheckMultipleDescriptors(WorldDescriptor[] descriptors) {
			if (descriptors.Length > 1)
				SetNotificationWithActions(
					_notificationIds["MultipleWorldDescriptors"], NotificationType.Warning,
					"world.builder.multiple_descriptors", new List<VisualElement> {
						new Button(
							() => {
								var target = _descriptorField?.value;
								Selection.activeObject = target;
							}
						) { text = LanguageManager.Get("world.builder.select") },
						new Button(
							() => {
								for (var i = 1; i < descriptors.Length; i++)
									Object.DestroyImmediate(descriptors[i].gameObject);
							}
						) { text = LanguageManager.Get("world.builder.remove_other") }
					}
				);
			else RemoveNotificationIfExists(_notificationIds["MultipleWorldDescriptors"]);
		}

		private void CheckUserLogin(IUser user) {
			if (user == null) {
				SetNotificationWithActions(
					_notificationIds["NoUser"], NotificationType.Warning,
					"world.builder.no_user", new List<VisualElement> {
						new Button(() => Editor.CoreAPI.PanelAPI.SetActivePanel("api.nox.user.login"))
							{ text = LanguageManager.Get("world.builder.login") }
					}
				);
				RemoveNotificationIfExists(_notificationIds["User"]);
			} else {
				RemoveNotificationIfExists(_notificationIds["NoUser"]);
				NotificationManager.Set(
					new Notification {
						Uid  = _notificationIds["User"],
						Type = NotificationType.Info,
						Content = new Label(
							LanguageManager.Get(
								"world.builder.logged_in",
								new object[] { user.GetDisplay() ?? user.GetUsername(), user.ToIdentifier().ToString() }
							)
						)
					}
				);
			}
		}

		private void CheckSpawns(WorldDescriptor descriptor) {
			// var esSpawn = descriptor.EstimateSpawns();
			//
			// if (esSpawn.Count == 1 && esSpawn[0] == descriptor.gameObject)
			// 	SetNotificationIfNotExists(_notificationIds["NoSpawns"], NotificationType.Info, "world.builder.no_spawns");
			// else RemoveNotificationIfExists(_notificationIds["NoSpawns"]);
			//
			// var spawns = descriptor.GetSpawns();
			// for (var i = 0; i < spawns.Length; i++) {
			// 	var spawn                  = spawns[i];
			// 	var nullNotificationId     = $"SpawnIsNull-{i}";
			// 	var estimateNotificationId = $"SpawnEstimate-{i}";
			//
			// 	if (!spawn) {
			// 		SetNotificationWithActions(
			// 			nullNotificationId, NotificationType.Warning,
			// 			"world.builder.spawn_null", new List<VisualElement> {
			// 				CreateNormalizeButton(() => descriptor.spawns = descriptor.EstimateSpawns().Values.ToArray())
			// 			}, i
			// 		);
			// 		RemoveNotificationIfExists(estimateNotificationId);
			// 	} else {
			// 		RemoveNotificationIfExists(nullNotificationId);
			//
			// 		var estimate = esSpawn.FirstOrDefault(e => e.Value == spawn);
			// 		if (estimate.Key != i)
			// 			SetNotificationWithActions(
			// 				estimateNotificationId, NotificationType.Warning,
			// 				"world.builder.spawn_estimate", new List<VisualElement> {
			// 					CreateNormalizeButton(() => descriptor.spawns = descriptor.EstimateSpawns().Values.ToArray())
			// 				}, spawn.name, i, estimate.Key, i
			// 			);
			// 		else RemoveNotificationIfExists(estimateNotificationId);
			// 	}
			// }
		}

		private void CheckScenes(WorldDescriptor descriptor) {
			// var esScene = descriptor.EstimateScenes();
			//
			// if (esScene.Count == 1)
			// 	SetNotificationIfNotExists(_notificationIds["NoScenes"], NotificationType.Success, "world.builder.no_scenes");
			// else RemoveNotificationIfExists(_notificationIds["NoScenes"]);
			//
			// var scenes = descriptor.GetScenes().Skip(1).ToList();
			// for (var i = 0; i < scenes.Count; i++) {
			// 	var scene                  = scenes[i];
			// 	var nullNotificationId     = $"SceneIsNull-{i}";
			// 	var estimateNotificationId = $"SceneEstimate-{i}";
			//
			// 	if (string.IsNullOrEmpty(scene)) {
			// 		SetNotificationWithActions(
			// 			nullNotificationId, NotificationType.Warning,
			// 			"world.builder.scene_null", new List<VisualElement> {
			// 				CreateNormalizeButton(() => descriptor.sceneAssets = descriptor.EstimateScenes().Values.ToList())
			// 			}, i
			// 		);
			// 		RemoveNotificationIfExists(estimateNotificationId);
			// 	} else {
			// 		RemoveNotificationIfExists(nullNotificationId);
			//
			// 		var estimate = esScene.FirstOrDefault(e => AssetDatabase.GetAssetPath(e.Value) == scene);
			// 		if (estimate.Key != i)
			// 			SetNotificationWithActions(
			// 				estimateNotificationId, NotificationType.Warning,
			// 				"world.builder.scene_estimate", new List<VisualElement> {
			// 					CreateNormalizeButton(() => descriptor.sceneAssets = descriptor.EstimateScenes().Values.ToList())
			// 				}, scene, i, estimate.Key, i
			// 			);
			// 		else RemoveNotificationIfExists(estimateNotificationId);
			// 	}
			// }
		}

		private void CheckPlayMode() {
			if (Application.isPlaying)
				SetNotificationWithActions(
					_notificationIds["PlayMode"], NotificationType.Error,
					"world.builder.play_mode", new List<VisualElement> {
						new Button(() => EditorApplication.isPlaying = false) { text = LanguageManager.Get("world.builder.exit") }
					}
				);
			else RemoveNotificationIfExists(_notificationIds["PlayMode"]);
		}

		private void CheckBuildPlatform(WorldDescriptor descriptor) {
			var buildPlatform = descriptor?.target ?? Platform.None;

			if (buildPlatform == Platform.None)
				SetNotificationWithActions(
					_notificationIds["UseActivePlatform"], NotificationType.Info,
					"world.builder.use_active_platform", new List<VisualElement> {
						new Button(
							() => {
								if (_platformField != null)
									_platformField.value = PlatformExtensions.CurrentPlatform;
							}
						) { text = LanguageManager.Get("world.builder.detect") }
					}, PlatformExtensions.CurrentPlatform.GetPlatformName()
				);
			else RemoveNotificationIfExists(_notificationIds["UseActivePlatform"]);
		}

		private void CheckUnsupportedPlatform(WorldDescriptor descriptor) {
			// Determine the platform to check
			var platformToCheck = descriptor?.target ?? Platform.None;

			// If platform is None, it's intentionally set to no specific platform, so don't show warnings
			if (platformToCheck == Platform.None) {
				RemoveNotificationIfExists(_notificationIds["UnsupportedPlatform"]);
				return;
			}

			// Check if the platform is supported
			if (!platformToCheck.IsSupported()) {
				SetNotificationWithActions(
					_notificationIds["UnsupportedPlatform"], NotificationType.Error,
					"world.builder.unsupported_platform", new List<VisualElement> {
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
						) { text = LanguageManager.Get("world.builder.use_supported") },
						new Button(
							() => {
								if (_platformField != null)
									_platformField.value = PlatformExtensions.CurrentPlatform;
							}
						) { text = LanguageManager.Get("world.builder.detect") }
					}, platformToCheck.GetPlatformName()
				);
			} else {
				RemoveNotificationIfExists(_notificationIds["UnsupportedPlatform"]);
			}
		}

		private void CheckPlatformMismatch(WorldDescriptor descriptor) {
			// Skip check if descriptor is null or target is None
			if (!descriptor || descriptor.target == Platform.None) {
				RemoveNotificationIfExists(_notificationIds["PlatformMismatch"]);
				return;
			}

			var currentPlatform = PlatformExtensions.CurrentPlatform;
			var targetPlatform  = descriptor.target;

			// Check if current platform differs from target platform
			if (currentPlatform != targetPlatform) {
				SetNotificationWithActions(
					_notificationIds["PlatformMismatch"], NotificationType.Warning,
					"world.builder.platform_mismatch", new List<VisualElement> {
						new Button(
							() => {
								if (_platformField != null)
									_platformField.value = currentPlatform;
							}
						) { text = LanguageManager.Get("world.builder.use_current") },
						new Button(
							() => {
								// Switch Unity's build target to match the descriptor
								var targetBuildTarget = targetPlatform.GetBuildTarget();
								if (targetBuildTarget != BuildTarget.NoTarget)
									PlatformExtensions.CurrentPlatform = targetPlatform;
							}
						) { text = LanguageManager.Get("world.builder.switch_platform") }
					}, currentPlatform.GetPlatformName(), targetPlatform.GetPlatformName()
				);
			} else RemoveNotificationIfExists(_notificationIds["PlatformMismatch"]);
		}

		private void UpdateUI(WorldDescriptor descriptor) {
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

		private void CreateNotificationItem(Notification notificationManager, VisualTreeAsset asset) {
			var item = CreateModernNotificationItem(notificationManager, asset.CloneTree());
			_notificationList.Add(item);
		}

		private VisualElement CreateModernNotificationItem(Notification notificationManager, VisualElement item) {
			var content = item.Q<VisualElement>("content");
			// var actions = item.Q<VisualElement>("actions");
			content.Add(notificationManager.Content);
			item.AddToClassList($"notification-{notificationManager.Type.ToString().ToLowerInvariant()}");
			// actions.Clear();
			return item;
		}

		public VisualElement Make(Dictionary<string, object> data) {
			NotificationManager.Clear();
			_lastHashNotify = "";
			_root.ClearBindings();
			_root.Clear();

			var child = Editor.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("builder.uxml").CloneTree();
			child.style.flexGrow = 1;
			_root.Add(child);

			// Cache UI elements
			_descriptorField   = _root.Q<ObjectField>("descriptor-field");
			_platformField     = _root.Q<EnumField>("platform-field");
			_outputFolderField = _root.Q<TextField>("output-folder-field");
			_notificationList  = _root.Q<VisualElement>("notifications");
			_progressContainer = _root.Q<VisualElement>("progress-container");
			_progressBar       = _root.Q<ProgressBar>("progress-bar");
			_progressLabel     = _root.Q<Label>("progress-label");
			_buildButton       = _root.Q<Button>("build-button");
			var versionLabel         = _root.Q<Label>("version");
			var gotoPublisherButton  = _root.Q<Button>("goto-publisher");
			var detectPlatformButton = _root.Q<Button>("detect-platform");
			var browseOutputButton   = _root.Q<Button>("browse-output-button");

			// Initialize components
			if (versionLabel != null)
				versionLabel.text = "v" + Editor.CoreAPI.ModMetadata.GetVersion();

			var descriptors = Descriptors;
			var descriptor  = descriptors.Length > 0 ? descriptors[0] : null;

			if (_platformField != null) {
				_platformField.Init(descriptor?.target ?? Platform.None);
				_platformField.RegisterValueChangedCallback(OnPlatformChanged);
			}

			if (_descriptorField != null)
				_descriptorField.value = descriptor;

			// Initialize output folder field
			if (_outputFolderField != null) {
				// Load saved output folder from EditorPrefs
				var savedOutputFolder = EditorPrefs.GetString("world.builder.output_folder", "");
				_outputFolderField.value = savedOutputFolder;
				_outputFolderField.RegisterValueChangedCallback(OnOutputFolderChanged);
			}

			if (gotoPublisherButton != null)
				gotoPublisherButton.clicked += () => Editor.CoreAPI.PanelAPI.SetActivePanel("api.nox.world.publisher");

			if (detectPlatformButton != null)
				detectPlatformButton.clicked += OnDetectPlatformClicked;

			if (browseOutputButton != null)
				browseOutputButton.clicked += OnBrowseOutputClicked;

			_buildButton?.RegisterCallback<ClickEvent>(OnBuildButtonClicked);

			return _root;
		}

		private void OnPlatformChanged(ChangeEvent<Enum> e) {
			var descriptors    = Descriptors;
			var mainDescriptor = descriptors.Length > 0 ? descriptors[0] : null;
			if (!mainDescriptor) return;

			var plat = (Platform)e.newValue;
			// Allow Platform.None as a valid selection (no specific platform)
			if (plat != Platform.None && !plat.IsSupported()) {
				ShowErrorDialog("world.builder.error_platform_not_supported", plat.GetPlatformName());
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

		private void OnDetectPlatformClicked() {
			if (_platformField != null)
				_platformField.value = PlatformExtensions.CurrentPlatform;
		}

		private void OnBrowseOutputClicked() {
			var currentFolder  = _outputFolderField?.value ?? "";
			var selectedFolder = EditorUtility.OpenFolderPanel("Select Output Folder", currentFolder, "");

			if (!string.IsNullOrEmpty(selectedFolder) && _outputFolderField != null)
				_outputFolderField.value = selectedFolder;
		}

		private void OnOutputFolderChanged(ChangeEvent<string> e) {
			var path           = e.newValue;
			var normalizedPath = NormalizePath(path);

			// If the path was normalized, update the field display
			if (normalizedPath != path && _outputFolderField != null) {
				_outputFolderField.SetValueWithoutNotify(normalizedPath);
			}

			// Save the normalized path to EditorPrefs
			EditorPrefs.SetString("world.builder.output_folder", normalizedPath);

			Logger.Log($"Output folder updated: {normalizedPath}");
		}

		private string NormalizePath(string path) {
			if (string.IsNullOrEmpty(path)) {
				return path;
			}

			// Convert to forward slashes for consistency
			path = path.Replace('\\', '/');

			// Get project root path (parent of Assets folder)
			var projectPath = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
			if (string.IsNullOrEmpty(projectPath)) {
				return path;
			}

			// If path is absolute and starts with project path, make it relative
			if (Path.IsPathRooted(path)) {
				// Ensure project path ends with /
				if (!projectPath.EndsWith("/")) {
					projectPath += "/";
				}

				// Check if path is within the project
				if (path.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase)) {
					path = path.Substring(projectPath.Length);
				}
			}

			// Ensure the path doesn't start with / or ./
			path = path.TrimStart('/', '.');
			if (path.StartsWith("/")) {
				path = path.Substring(1);
			}

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
				var relativePath = fullPath.Substring(assetsPath.Length).TrimStart('/');
				return string.IsNullOrEmpty(relativePath) ? "Assets" : $"Assets/{relativePath}";
			}

			return path;
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
			if (_progressContainer != null) {
				_progressContainer.style.display = DisplayStyle.None;
			}

			// Re-enable the build button
			if (_buildButton != null) {
				_buildButton.SetEnabled(true);
			}
		}

		private void OnBuildButtonClicked(ClickEvent _)
			=> OnBuildButtonClickedAsync().Forget();

		private async UniTaskVoid OnBuildButtonClickedAsync() {
			var descriptors    = Descriptors;
			var mainDescriptor = descriptors.Length > 0 ? descriptors[0] : null;

			if (!mainDescriptor) {
				ShowErrorDialog("world.builder.error_no_descriptor");
				Logger.LogError("No world descriptor found.");
				return;
			}

			var target = mainDescriptor.target;

			// If platform is None, use the current build target
			if (target == Platform.None) {
				target = PlatformExtensions.CurrentPlatform;
				Logger.Log($"Platform is set to None, using current build target: {target.GetPlatformName()}");
			}

			if (!target.IsSupported()) {
				ShowErrorDialog("world.builder.error_unsupported_target");
				Logger.LogError("Unsupported build target.");
				return;
			}

			// Get the output folder from the UI field
			var outputFolder = _outputFolderField?.value ?? "";
			if (string.IsNullOrEmpty(outputFolder)) {
				ShowErrorDialog("world.builder.error_no_output_folder");
				Logger.LogError("No output folder specified.");
				return;
			}

			// Ensure output folder ends with a slash
			if (!outputFolder.EndsWith("/") && !outputFolder.EndsWith("\\"))
				outputFolder += "/";

			// Convert to absolute path if it's relative
			var absoluteOutputPath = outputFolder;
			if (!Path.IsPathRooted(outputFolder)) {
				var projectPath = Path.GetDirectoryName(Application.dataPath);
				absoluteOutputPath = Path.Combine(projectPath, outputFolder).Replace('\\', '/');
				if (!absoluteOutputPath.EndsWith("/"))
					absoluteOutputPath += "/";
			}

			Logger.Log($"Building world '{mainDescriptor.name}' to output folder: {absoluteOutputPath}");

			// Show progress bar
			ShowProgress(0f, "Initializing build...");

			try {
				// Create build data
				var buildData = new BuildData {
					Descriptor       = mainDescriptor,
					Target           = target,
					OutputPath       = absoluteOutputPath,
					ShowDialog       = false, // We'll handle the dialog ourselves
					ProgressCallback = ShowProgress
				};

				// Start the build process
				Logger.Log($"Building world '{mainDescriptor.name}'");
				var result = await Builder.Build(buildData);
				Logger.Log($"Build result: {result.Type}, Message: {result.Message}");

				// Hide progress bar
				HideProgress();

				// Handle the result
				switch (result.Type) {
					case BuildResultType.Success:
						ShowSuccessDialog("world.builder.success_build");
						Logger.Log("Build completed successfully.");
						break;
					case BuildResultType.Failed:
						ShowErrorDialog(result.Message, useDirectMessage: true);
						Logger.LogError($"Build failed: {result.Message}");
						break;
					case BuildResultType.AlreadyBuilding:
						ShowErrorDialog("world.builder.error_already_building");
						Logger.LogWarning("A build is already in progress.");
						break;
					case BuildResultType.EditorCompiling:
						ShowErrorDialog("world.builder.error_editor_compiling");
						Logger.LogWarning("Unity is currently compiling scripts.");
						break;
					case BuildResultType.InvalidTarget:
						ShowErrorDialog("world.builder.error_invalid_target");
						Logger.LogError("Invalid build target specified.");
						break;
					case BuildResultType.UnsupportedTarget:
						ShowErrorDialog("world.builder.error_unsupported_target");
						Logger.LogError($"Build target {target} is not supported.");
						break;
					case BuildResultType.InvalidScene:
						ShowErrorDialog("world.builder.error_invalid_scene");
						Logger.LogError("The scene is not valid for building.");
						break;
					default:
						ShowErrorDialog("world.builder.error_unknown");
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

		private void ShowErrorDialog(string messageKey, params object[] args)
			=> ShowErrorDialog(messageKey, false, args);

		private void ShowErrorDialog(string messageKey, bool useDirectMessage, params object[] args) {
			var message = useDirectMessage ? messageKey : LanguageManager.Get(messageKey, args);
			EditorUtility.DisplayDialog(
				LanguageManager.Get("world.builder.error"),
				message,
				LanguageManager.Get("world.builder.ok")
			);
		}

		private void ShowSuccessDialog(string messageKey, params object[] args)
			=> EditorUtility.DisplayDialog(
				LanguageManager.Get("world.builder.success"),
				LanguageManager.Get(messageKey, args),
				LanguageManager.Get("world.builder.ok")
			);

		public void Dispose() {
			// Clear cached references
			_descriptorField   = null;
			_platformField     = null;
			_outputFolderField = null;
			_notificationList  = null;
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
	}
}
#endif