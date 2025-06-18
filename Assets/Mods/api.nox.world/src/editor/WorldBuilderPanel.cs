#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
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
		private VisualElement _notificationList;

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
			["UnsupportedPlatform"]      = "UnsupportedPlatform"
		};

		internal static MainSceneDescriptor[] Descriptors
			=> SceneDescriptorExtension.GetDescriptors<MainSceneDescriptor>();

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
						new Button(SceneDescriptorExtension.MakeMainSceneDescriptor)
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
			UpdateUI(descriptor);
		}

		private void CheckMultipleDescriptors(MainSceneDescriptor[] descriptors) {
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

		private void CheckSpawns(MainSceneDescriptor descriptor) {
			var esSpawn = descriptor.EstimateSpawns();

			if (esSpawn.Count == 1 && esSpawn[0] == descriptor.gameObject)
				SetNotificationIfNotExists(_notificationIds["NoSpawns"], NotificationType.Info, "world.builder.no_spawns");
			else RemoveNotificationIfExists(_notificationIds["NoSpawns"]);

			var spawns = descriptor.GetSpawns();
			for (var i = 0; i < spawns.Length; i++) {
				var spawn                  = spawns[i];
				var nullNotificationId     = $"SpawnIsNull-{i}";
				var estimateNotificationId = $"SpawnEstimate-{i}";

				if (!spawn) {
					SetNotificationWithActions(
						nullNotificationId, NotificationType.Warning,
						"world.builder.spawn_null", new List<VisualElement> {
							CreateNormalizeButton(() => descriptor.spawns = descriptor.EstimateSpawns().Values.ToArray())
						}, i
					);
					RemoveNotificationIfExists(estimateNotificationId);
				} else {
					RemoveNotificationIfExists(nullNotificationId);

					var estimate = esSpawn.FirstOrDefault(e => e.Value == spawn);
					if (estimate.Key != i)
						SetNotificationWithActions(
							estimateNotificationId, NotificationType.Warning,
							"world.builder.spawn_estimate", new List<VisualElement> {
								CreateNormalizeButton(() => descriptor.spawns = descriptor.EstimateSpawns().Values.ToArray())
							}, spawn.name, i, estimate.Key, i
						);
					else RemoveNotificationIfExists(estimateNotificationId);
				}
			}
		}

		private void CheckScenes(MainSceneDescriptor descriptor) {
			var esScene = descriptor.EstimateScenes();

			if (esScene.Count == 1)
				SetNotificationIfNotExists(_notificationIds["NoScenes"], NotificationType.Success, "world.builder.no_scenes");
			else RemoveNotificationIfExists(_notificationIds["NoScenes"]);

			var scenes = descriptor.GetScenes().Skip(1).ToList();
			for (var i = 0; i < scenes.Count; i++) {
				var scene                  = scenes[i];
				var nullNotificationId     = $"SceneIsNull-{i}";
				var estimateNotificationId = $"SceneEstimate-{i}";

				if (string.IsNullOrEmpty(scene)) {
					SetNotificationWithActions(
						nullNotificationId, NotificationType.Warning,
						"world.builder.scene_null", new List<VisualElement> {
							CreateNormalizeButton(() => descriptor.sceneAssets = descriptor.EstimateScenes().Values.ToList())
						}, i
					);
					RemoveNotificationIfExists(estimateNotificationId);
				} else {
					RemoveNotificationIfExists(nullNotificationId);

					var estimate = esScene.FirstOrDefault(e => AssetDatabase.GetAssetPath(e.Value) == scene);
					if (estimate.Key != i)
						SetNotificationWithActions(
							estimateNotificationId, NotificationType.Warning,
							"world.builder.scene_estimate", new List<VisualElement> {
								CreateNormalizeButton(() => descriptor.sceneAssets = descriptor.EstimateScenes().Values.ToList())
							}, scene, i, estimate.Key, i
						);
					else RemoveNotificationIfExists(estimateNotificationId);
				}
			}
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

		private void CheckBuildPlatform(MainSceneDescriptor descriptor) {
			var buildPlatform = descriptor?.target ?? Platform.None;

			if (buildPlatform == Platform.None)
				SetNotificationWithActions(
					_notificationIds["UseActivePlatform"], NotificationType.Info,
					"world.builder.use_active_platform", new List<VisualElement> {
						new Button(
							() => {
								if (_platformField != null)
									_platformField.value = PlatformExtensions.GetCurrentTarget();
							}
						) { text = LanguageManager.Get("world.builder.detect") }
					}
				);
			else RemoveNotificationIfExists(_notificationIds["UseActivePlatform"]);
		}

		private void CheckUnsupportedPlatform(MainSceneDescriptor descriptor) {
			// Determine the platform to check
			var platformToCheck = descriptor?.target ?? Platform.None;

			// If descriptor target is None, use current build target
			if (platformToCheck == Platform.None) {
				platformToCheck = PlatformExtensions.GetCurrentTarget().GetPlatform();
			}

			// Check if the platform is supported
			if (!platformToCheck.IsSupported()) {
				SetNotificationWithActions(
					_notificationIds["UnsupportedPlatform"], NotificationType.Error,
					"world.builder.unsupported_platform", new List<VisualElement> {
						new Button(
							() => {
								// Try to find a supported platform and set it
								var supportedPlatforms = System.Enum.GetValues(typeof(Platform))
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
									_platformField.value = PlatformExtensions.GetCurrentTarget();
							}
						) { text = LanguageManager.Get("world.builder.detect") }
					}, platformToCheck.GetPlatformName()
				);
			} else {
				RemoveNotificationIfExists(_notificationIds["UnsupportedPlatform"]);
			}
		}

		private void UpdateUI(MainSceneDescriptor descriptor) {
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

		private void CreateNotificationItem(Notification notification, VisualTreeAsset asset) {
			var item = CreateModernNotificationItem(notification, asset.CloneTree());
			_notificationList.Add(item);
		}

		private VisualElement CreateModernNotificationItem(Notification notification, VisualElement item) {
			var content = item.Q<VisualElement>("content");
			// var actions = item.Q<VisualElement>("actions");
			content.Add(notification.Content);
			item.AddToClassList($"notification-{notification.Type.ToString().ToLowerInvariant()}");
			// actions.Clear();
			return item;
		}

		public VisualElement Make(Dictionary<string, object> data) {
			NotificationManager.Clear();
			_lastHashNotify = "";
			_root.ClearBindings();
			_root.Clear();

			// Reset cached UI elements
			_descriptorField  = null;
			_platformField    = null;
			_notificationList = null;

			var child = Editor.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("builder.uxml").CloneTree();
			child.style.flexGrow = 1;
			_root.Add(child);

			// Cache UI elements
			_descriptorField  = _root.Q<ObjectField>("descriptor-field");
			_platformField    = _root.Q<EnumField>("platform-field");
			_notificationList = _root.Q<VisualElement>("notifications");
			var versionLabel         = _root.Q<Label>("version");
			var gotoPublisherButton  = _root.Q<Button>("goto-publisher");
			var detectPlatformButton = _root.Q<Button>("detect-platform");
			var buildButton          = _root.Q<Button>("build-button");

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

			if (gotoPublisherButton != null)
				gotoPublisherButton.clicked += () => Editor.CoreAPI.PanelAPI.SetActivePanel("api.nox.world.publisher");

			if (detectPlatformButton != null)
				detectPlatformButton.clicked += OnDetectPlatformClicked;

			buildButton?.RegisterCallback<ClickEvent>(OnBuildButtonClicked);

			return _root;
		}

		private void OnPlatformChanged(ChangeEvent<Enum> e) {
			var descriptors    = Descriptors;
			var mainDescriptor = descriptors.Length > 0 ? descriptors[0] : null;
			if (!mainDescriptor) return;

			var plat = (Platform)e.newValue;
			if (!plat.IsSupported()) {
				ShowErrorDialog("world.builder.error_platform_not_supported", plat.GetPlatformName());
				Logger.LogError($"Platform \"{plat.GetPlatformName()}\" ({plat.GetBuildTarget()}) is not supported.");

				_platformField?.SetValueWithoutNotify(e.previousValue ?? mainDescriptor.target);
			} else mainDescriptor.target = plat;
		}

		private void OnDetectPlatformClicked() {
			if (_platformField != null)
				_platformField.value = PlatformExtensions.GetCurrentTarget().GetPlatform();
		}

		private void OnBuildButtonClicked(ClickEvent _) {
			var descriptors    = Descriptors;
			var mainDescriptor = descriptors.Length > 0 ? descriptors[0] : null;

			if (!mainDescriptor) {
				ShowErrorDialog("world.builder.error_no_descriptor");
				Logger.LogError("No world descriptor found.");
				return;
			}

			var target = mainDescriptor.target;
			if (!target.IsSupported()) {
				ShowErrorDialog("world.builder.error_unsupported_target");
				Logger.LogError("Unsupported build target.");
				return;
			}

			// var result = Builder.Bui(mainDescriptor, target.GetBuildTarget(), false);
			// if (result.Success) {
			// 	ShowSuccessDialog("world.builder.success_build");
			// 	Logger.Log("Build success.");
			// } else {
			// 	ShowErrorDialog(result.ErrorMessage, useDirectMessage: true);
			// 	Logger.LogError(result.ErrorMessage);
			// }
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
			_descriptorField  = null;
			_platformField    = null;
			_notificationList = null;

			// Clear notifications and UI
			NotificationManager.Clear();
			_root.Clear();
			_root.ClearBindings();
			_lastHashNotify = "";
		}
	}
}
#endif