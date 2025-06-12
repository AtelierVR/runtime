#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mods.Panels;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;

namespace api.nox.world
{
    public class WorldBuilderPanel : EditorPanelBuilder, IDisposable
    {
        
        public string GetId() => "builder";
        public string GetName() => "World/Builder";
        public bool IsHidden() => false;
        
        private string _lastHashNotify = "";
        private readonly VisualElement _root = new();
        
        internal static MainDescriptor[] Descriptors 
            => MainDescriptorEditor.GetWorldDescriptors(false);

        internal void Update()
        {
            if (!Editor.HasOnePanelOpened() || _root.childCount == 0) return;
            var descriptor = Descriptors.Length > 0 ? Descriptors[0] : null;
            var user = Editor.NetworkAPI.GetField("User").CallMethod("GetCurrentUser");

            // Check if a scene has a world descriptor
            if (!descriptor && !NotificationManager.Has("NoWorldDescriptor"))
                NotificationManager.Set(new Notification
                {
                    Uid = "NoWorldDescriptor",
                    Type = NotificationType.Error,
                    Content = new Label("No world descriptor found.\nA world descriptor is required."),
                    Actions = new List<VisualElement>
                    {
                        new Button(() => MainDescriptorEditor.MakeWorldDescriptor()) { text = "Create" }
                    }
                });
            else if (descriptor && NotificationManager.Has("NoWorldDescriptor"))
                NotificationManager.Remove("NoWorldDescriptor");

            if (descriptor)
            {
                // Check if a scene has multiple world descriptors
                if (Descriptors.Length > 1 && !NotificationManager.Has("MultipleWorldDescriptors"))
                    NotificationManager.Set(new Notification
                    {
                        Uid = "MultipleWorldDescriptors",
                        Type = NotificationType.Warning,
                        Content = new Label(
                            "Multiple world descriptors found.\nThe first one will be used.\nOnly one is allowed."),
                        Actions = new List<VisualElement>
                        {
                            new Button(() =>
                            {
                                var target = _root.Q<ObjectField>("descriptor-field").value;
                                Selection.activeObject = target;
                            }) { text = "Select" },
                            new Button(() =>
                            {
                                var descriptors = Descriptors;
                                for (var i = 1; i < descriptors.Length; i++)
                                    Object.DestroyImmediate(descriptors[i].gameObject);
                            }) { text = "Remove other" }
                        }
                    });
                else if (Descriptors.Length <= 1 && NotificationManager.Has("MultipleWorldDescriptors"))
                    NotificationManager.Remove("MultipleWorldDescriptors");

                // Check if user is logged in
                if (user == null && !NotificationManager.Has("NoUser"))
                    NotificationManager.Set(new Notification
                    {
                        Uid = "NoUser",
                        Type = NotificationType.Warning,
                        Content = new Label("No user found.\nA user is required to publish a world."),
                        Actions = new List<VisualElement>
                        {
                            new Button(() => Editor.CoreAPI.PanelAPI.SetActivePanel("api.nox.user.login")) { text = "Login" }
                        }
                    });
                else if (user != null && NotificationManager.Has("NoUser"))
                    NotificationManager.Remove("NoUser");
                if (user != null)
                    NotificationManager.Set(new Notification
                    {
                        Uid = "User",
                        Type = NotificationType.Info,
                        Content = new Label(
                            $"Logged in as {user.GetField<string>("display")} ({user.GetField<uint>("id")}@{user.GetField<string>("server")}).")
                    });
                else if (NotificationManager.Has("User"))
                    NotificationManager.Remove("User");

                // Check if a scene has network objects
                var esNet = descriptor.EstimateNetworkObjects();
                if (esNet.Count == 0 && !NotificationManager.Has("NoNetworkObjects"))
                    NotificationManager.Set(new Notification
                    {
                        Uid = "NoNetworkObjects",
                        Type = NotificationType.Good,
                        Content = new Label("No network objects found.\nNetwork objects are optional.")
                    });
                else if (esNet.Count > 0 && NotificationManager.Has("NoNetworkObjects"))
                    NotificationManager.Remove("NoNetworkObjects");
                var networkObjects = descriptor.GetNetworkObjects();
                for (var i = 0; i < networkObjects.Count; i++)
                {
                    var netObj = networkObjects[i];
                    if (!netObj && !NotificationManager.Has("NetworkObjectIsNull-" + i))
                        NotificationManager.Set(new Notification
                        {
                            Uid = "NetworkObjectIsNull-" + i,
                            Type = NotificationType.Warning,
                            Content = new Label($"The connected object at position {i} is null."),
                            Actions = new List<VisualElement>
                            {
                                new Button(() =>
                                        descriptor.NetworkObjects = descriptor.EstimateNetworkObjects().Values.ToList())
                                    { text = "Normalize" }
                            }
                        });
                    else if (netObj && NotificationManager.Has("NetworkObjectIsNull-" + i))
                        NotificationManager.Remove("NetworkObjectIsNull-" + i);
                    if (!netObj) continue;
                    var estimate = esNet.FirstOrDefault(e => e.Value == netObj);
                    if (estimate.Key != netObj.networkId && !NotificationManager.Has("NetworkObjectEstimate-" + i))
                        NotificationManager.Set(new Notification
                        {
                            Uid = "NetworkObjectEstimate-" + i,
                            Type = NotificationType.Warning,
                            Content = new Label(
                                $"The id of the connected object {netObj.name} will be changed from {netObj.networkId} to {estimate.Key} at position {i}.\n" +
                                "This may be due to a duplicate element or another element already using this id."),
                            Actions = new List<VisualElement>
                            {
                                new Button(() =>
                                        descriptor.NetworkObjects = descriptor.EstimateNetworkObjects().Values.ToList())
                                    { text = "Normalize" }
                            }
                        });
                    else if (estimate.Key == netObj.networkId && NotificationManager.Has("NetworkObjectEstimate-" + i))
                        NotificationManager.Remove("NetworkObjectEstimate-" + i);
                }

                // Check if a scene has spawns
                var esSpawn = descriptor.EstimateSpawns();
                if (esSpawn.Count == 1 && esSpawn[0] == descriptor.gameObject && !NotificationManager.Has("NoSpawns"))
                    NotificationManager.Set(new Notification
                    {
                        Uid = "NoSpawns",
                        Type = NotificationType.Info,
                        Content = new Label("No spawns found.\nThe default spawn is the world descriptor.")
                    });
                else if (esSpawn.Count > 1 && NotificationManager.Has("NoSpawns"))
                    NotificationManager.Remove("NoSpawns");
                var spawns = descriptor.GetSpawns();
                for (var i = 0; i < spawns.Count; i++)
                {
                    var spawn = spawns[i];
                    if (!spawn && !NotificationManager.Has("SpawnIsNull-" + i))
                        NotificationManager.Set(new Notification
                        {
                            Uid = "SpawnIsNull-" + i,
                            Type = NotificationType.Warning,
                            Content = new Label($"The spawn at position {i} is null."),
                            Actions = new List<VisualElement>
                            {
                                new Button(() => descriptor.Spawns = descriptor.EstimateSpawns().Values.ToList())
                                    { text = "Normalize" }
                            }
                        });
                    else if (spawn && NotificationManager.Has("SpawnIsNull-" + i))
                        NotificationManager.Remove("SpawnIsNull-" + i);
                    if (!spawn) continue;
                    var estimate = esSpawn.FirstOrDefault(e => e.Value == spawn);
                    if (estimate.Key != i && !NotificationManager.Has("SpawnEstimate-" + i))
                        NotificationManager.Set(new Notification
                        {
                            Uid = "SpawnEstimate-" + i,
                            Type = NotificationType.Warning,
                            Content = new Label(string.Format(
                                "The id of the spawn {0} will be changed from {1} to {2} at position {3}.\n"
                                + "This may be due to a duplicate element or another element already using this id.",
                                spawn.name, i, estimate.Key, i)),
                            Actions = new List<VisualElement>
                            {
                                new Button(() => descriptor.Spawns = descriptor.EstimateSpawns().Values.ToList())
                                    { text = "Normalize" }
                            }
                        });
                    else if (estimate.Key == i && NotificationManager.Has("SpawnEstimate-" + i))
                        NotificationManager.Remove("SpawnEstimate-" + i);
                }

                // Check if a scene has scenes
                var esScene = descriptor.EstimateScenes();
                if (esScene.Count == 1 && !NotificationManager.Has("NoScenes"))
                    NotificationManager.Set(new Notification
                    {
                        Uid = "NoScenes",
                        Type = NotificationType.Good,
                        Content = new Label("No additional scenes found.\nAdditional scenes are optional.")
                    });
                else if (esScene.Count > 1 && NotificationManager.Has("NoScenes"))
                    NotificationManager.Remove("NoScenes");

                var scenes = descriptor.GetScenes().Skip(1).ToList();
                for (var i = 0; i < scenes.Count; i++)
                {
                    var scene = scenes[i];
                    if (string.IsNullOrEmpty(scene) && !NotificationManager.Has("SceneIsNull-" + i))
                        NotificationManager.Set(new Notification
                        {
                            Uid = "SceneIsNull-" + i,
                            Type = NotificationType.Warning,
                            Content = new Label($"The scene at position {i} is null."),
                            Actions = new List<VisualElement>
                            {
                                new Button(() => descriptor.Scenes = descriptor.EstimateScenes().Values.ToList())
                                    { text = "Normalize" }
                            }
                        });
                    else if (!string.IsNullOrEmpty(scene) && NotificationManager.Has("SceneIsNull-" + i))
                        NotificationManager.Remove("SceneIsNull-" + i);
                    if (string.IsNullOrEmpty(scene)) continue;
                    var estimate = esScene.FirstOrDefault(e => AssetDatabase.GetAssetPath(e.Value) == scene);
                    if (estimate.Key != i && !NotificationManager.Has("SceneEstimate-" + i))
                        NotificationManager.Set(new Notification
                        {
                            Uid = "SceneEstimate-" + i,
                            Type = NotificationType.Warning,
                            Content = new Label(string.Format(
                                "The id of the scene {0} will be changed from {1} to {2} at position {3}.\n"
                                + "This may be due to a duplicate element or another element already using this id.",
                                scene, i, estimate.Key, i)),
                            Actions = new List<VisualElement>
                            {
                                new Button(() => descriptor.Scenes = descriptor.EstimateScenes().Values.ToList())
                                    { text = "Normalize" }
                            }
                        });
                    else if (estimate.Key == i && NotificationManager.Has("SceneEstimate-" + i))
                        NotificationManager.Remove("SceneEstimate-" + i);
                }
            }

            // detect play mode
            if (Application.isPlaying && !NotificationManager.Has("PlayMode"))
                NotificationManager.Set(new Notification
                {
                    Uid = "PlayMode",
                    Type = NotificationType.Error,
                    Content = new Label("Play mode is enabled.\nBuilding is disabled in play mode."),
                    Actions = new List<VisualElement>
                    {
                        new Button(() => EditorApplication.isPlaying = false) { text = "Exit" }
                    }
                });
            else if (!Application.isPlaying && NotificationManager.Has("PlayMode"))
                NotificationManager.Remove("PlayMode");

            var buildPlatform = descriptor ? descriptor.GetBuildPlatform() : Platform.None;
            if (buildPlatform == Platform.None && !NotificationManager.Has("NoBuildPlatform"))
                NotificationManager.Set(new Notification
                {
                    Uid = "NoBuildPlatform",
                    Type = NotificationType.Error,
                    Content = new Label("No build platform selected.\nA build platform is required."),
                    Actions = new List<VisualElement>
                    {
                        new Button(() =>
                                _root.Q<EnumField>("platform-field").value = PlatformExtensions.GetCurrentTarget())
                            { text = "Detect" }
                    }
                });
            else if (buildPlatform != Platform.None && NotificationManager.Has("NoBuildPlatform"))
                NotificationManager.Remove("NoBuildPlatform");

            if (_root.childCount > 0)
            {
                _root.Q<ObjectField>("descriptor-field").value = descriptor;
                var target = _root.Q<EnumField>("platform-field");
                if (descriptor && descriptor.Target != (Platform)target.value)
                    target.SetValueWithoutNotify(descriptor.Target);

                var notify = NotificationManager.Notifications;
                var hash = notify.Aggregate("", (current, notification) => current + ":" + notification.Uid);
                if (hash != _lastHashNotify)
                {
                    var notificationList = _root.Q<VisualElement>("notifications");
                    notificationList.Clear();
                    notify.Sort((a, b) => a.Type.CompareTo(b.Type));
                    var asset = Editor.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("notification.uxml");
                    foreach (var notification in notify)
                    {
                        var item = asset.CloneTree();
                        item.Q<VisualElement>("content").Add(notification.Content);
                        if (item.Q<VisualElement>("typing") != null)
                            if (notification.Type == NotificationType.Error)
                                item.Q<Image>("icon").style.backgroundImage =
                                    Editor.CoreAPI.AssetAPI.GetAsset<Texture2D>("api.nox.game", "icons/error.png");
                            else if (notification.Type == NotificationType.Warning)
                                item.Q<Image>("icon").style.backgroundImage =
                                    Editor.CoreAPI.AssetAPI.GetAsset<Texture2D>("api.nox.game", "icons/warning.png");
                            else if (notification.Type == NotificationType.Info)
                                item.Q<Image>("icon").style.backgroundImage =
                                    Editor.CoreAPI.AssetAPI.GetAsset<Texture2D>("api.nox.game", "icons/info.png");
                            else if (notification.Type == NotificationType.Good)
                                item.Q<Image>("icon").style.backgroundImage =
                                    Editor.CoreAPI.AssetAPI.GetAsset<Texture2D>("api.nox.game", "icons/good.png");
                            else item.Q<VisualElement>("typing").style.display = DisplayStyle.None;
                        if (notification.Actions is { Count: > 0 })
                            foreach (var action in notification.Actions)
                                item.Q<VisualElement>("action_bar").Add(action);
                        
                        else item.Q<VisualElement>("actions").style.display = DisplayStyle.None;

                        notificationList.Add(item);
                    }

                    var hasErrors = notify.Any(n => n.Type == NotificationType.Error);
                    foreach (var element in _root.Query(null, "disable-on-error").ToList())
                        element.SetEnabled(!hasErrors);
                    _lastHashNotify = hash;
                }
            }
        }

        public VisualElement Make(Dictionary<string, object> data)
        {
            NotificationManager.Clear();
            _lastHashNotify = "";
            _root.ClearBindings();
            _root.Clear();

            var child = Editor.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("builder.uxml").CloneTree();
            child.style.flexGrow = 1;
            _root.Add(child);

            _root.Q<Label>("version").text = "v" + Editor.CoreAPI.ModMetadata.GetVersion();
            var descriptor = Descriptors.Length > 0 ? Descriptors[0] : null;
            _root.Q<EnumField>("platform-field").Init(descriptor?.GetBuildPlatform() ?? Platform.None);
            _root.Q<ObjectField>("descriptor-field").value = descriptor;
            _root.Q<Button>("goto-publisher").clicked +=
                () => Editor.CoreAPI.PanelAPI.SetActivePanel("api.nox.world.publisher");
            _root.Q<Button>("detect-platform").clicked += () =>
                _root.Q<EnumField>("platform-field").value = PlatformExtensions.GetCurrentTarget().GetPlatform();
            _root.Q<EnumField>("platform-field").RegisterValueChangedCallback(e =>
            {
                var mainDescriptor = Descriptors.Length > 0 ? Descriptors[0] : null;
                if (!mainDescriptor) return;
                var plat = (Platform)e.newValue;
                if (!plat.IsSupported())
                {
                    EditorUtility.DisplayDialog("Error", $"{plat.GetPlatformName()} is not supported.", "Ok");
                    Logger.LogError(
                        $"Platform \"{plat.GetPlatformName()}\" ({plat.GetBuildTarget()}) is not supported.");
                    _root.Q<EnumField>("platform-field")
                        .SetValueWithoutNotify(e.previousValue ?? mainDescriptor.GetBuildPlatform());
                }
                else mainDescriptor.Target = plat;
            });

            _root.Q<Button>("build-button").RegisterCallback<ClickEvent>(_ =>
            {
                var mainDescriptor = Descriptors.Length > 0 ? Descriptors[0] : null;
                if (!mainDescriptor)
                {
                    EditorUtility.DisplayDialog("Error", "No world descriptor found.", "Ok");
                    Logger.LogError("No world descriptor found.");
                    return;
                }

                var target = mainDescriptor.GetBuildPlatform();

                if (!target.IsSupported())
                {
                    EditorUtility.DisplayDialog("Error", "Unsupported build target.", "Ok");
                    Logger.LogError("Unsupported build target.");
                    return;
                }

                var result = MainDescriptorEditor.BuildWorld(mainDescriptor, target.GetBuildTarget(), false);
                if (result.Success)
                {
                    EditorUtility.DisplayDialog("Success", "Build success.", "Ok");
                    Logger.Log("Build success.");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", result.ErrorMessage, "Ok");
                    Logger.LogError(result.ErrorMessage);
                }
            });
            return _root;
        }

        public void Dispose()
        {
            _root.Clear();
            _root.ClearBindings();
        }
    }
}
#endif