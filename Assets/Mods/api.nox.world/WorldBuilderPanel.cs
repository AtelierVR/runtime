#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Nox.CCK;
using Nox.CCK.Editor;
using Nox.CCK.Worlds;
using Nox.Editor.Worlds;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace api.nox.world
{
    public class WorldBuilderPanel : EditorPanelBuilder
    {
        public string Id { get; } = "builder";
        public string Name { get; } = "World/Builder";
        public bool Hidded { get; } = false;
        private string lasthashnotif = "";
        internal VisualElement _root = new();
        internal MainDescriptor[] Descriptors => MainDescriptorEditor.GetWorldDescriptors(false);

        internal WorldEditorMod _mod;
        internal WorldBuilderPanel(WorldEditorMod mod) => _mod = mod;

        internal void OnUpdate()
        {
            if (!_mod.HasOnePanelOpenned() || _root.childCount == 0) return;
            var descriptor = Descriptors.Length > 0 ? Descriptors[0] : null;
            SimplyUserMe user = _mod.NetworkAPI.GetCurrentUser();

            // Check if a scene has a world descriptor
            if (descriptor == null && !NotificationManager.Has("NoWorldDescriptor"))
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
            else if (descriptor != null && NotificationManager.Has("NoWorldDescriptor"))
                NotificationManager.Remove("NoWorldDescriptor");

            if (descriptor != null)
            {
                // Check if a scene has multiple world descriptors
                if (Descriptors.Length > 1 && !NotificationManager.Has("MultipleWorldDescriptors"))
                    NotificationManager.Set(new Notification
                    {
                        Uid = "MultipleWorldDescriptors",
                        Type = NotificationType.Warning,
                        Content = new Label("Multiple world descriptors found.\nThe first one will be used.\nOnly one is allowed."),
                        Actions = new List<VisualElement>
                        {
                            new Button(() =>{
                                var descriptor = _root.Q<ObjectField>("descriptor-field").value;
                                Selection.activeObject = descriptor;
                            }) { text = "Select" },
                            new Button(() => {
                                var descriptors = Descriptors;
                                for (var i = 1; i < descriptors.Length; i++)
                                    GameObject.DestroyImmediate(descriptors[i].gameObject);
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
                            new Button(() => _mod._api.PanelAPI.SetActivePanel("api.nox.user.login")) { text = "Login" }
                        }
                    });
                else if (user != null && NotificationManager.Has("NoUser"))
                    NotificationManager.Remove("NoUser");
                if (user != null)
                    NotificationManager.Set(new Notification
                    {
                        Uid = "User",
                        Type = NotificationType.Info,
                        Content = new Label(string.Format("Logged in as {0} ({1}@{2}).", user.display, user.id, user.server))
                    });
                else if (user == null && NotificationManager.Has("User"))
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
                    if (netObj == null && !NotificationManager.Has("NetworkObjectIsNull-" + i))
                        NotificationManager.Set(new Notification
                        {
                            Uid = "NetworkObjectIsNull-" + i,
                            Type = NotificationType.Warning,
                            Content = new Label(string.Format("The connected object at position {0} is null.", i)),
                            Actions = new List<VisualElement>
                            {
                                new Button(() => descriptor.NetworkObjects = descriptor.EstimateNetworkObjects().Values.ToList()) { text = "Normalize" }
                            }
                        });
                    else if (netObj != null && NotificationManager.Has("NetworkObjectIsNull-" + i))
                        NotificationManager.Remove("NetworkObjectIsNull-" + i);
                    if (netObj == null) continue;
                    var estimate = esNet.FirstOrDefault(e => e.Value == netObj);
                    if (estimate.Key != netObj.networkId && !NotificationManager.Has("NetworkObjectEstimate-" + i))
                        NotificationManager.Set(new Notification
                        {
                            Uid = "NetworkObjectEstimate-" + i,
                            Type = NotificationType.Warning,
                            Content = new Label(string.Format("The id of the connected object {0} will be changed from {1} to {2} at position {3}.\n"
                                + "This may be due to a duplicate element or another element already using this id.",
                                netObj.name, netObj.networkId, estimate.Key, i)),
                            Actions = new List<VisualElement>
                            {
                                new Button(() => descriptor.NetworkObjects = descriptor.EstimateNetworkObjects().Values.ToList()) { text = "Normalize" }
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
                    if (spawn == null && !NotificationManager.Has("SpawnIsNull-" + i))
                        NotificationManager.Set(new Notification
                        {
                            Uid = "SpawnIsNull-" + i,
                            Type = NotificationType.Warning,
                            Content = new Label(string.Format("The spawn at position {0} is null.", i)),
                            Actions = new List<VisualElement>
                            {
                                new Button(() => descriptor.Spawns = descriptor.EstimateSpawns().Values.ToList()) { text = "Normalize" }
                            }
                        });
                    else if (spawn != null && NotificationManager.Has("SpawnIsNull-" + i))
                        NotificationManager.Remove("SpawnIsNull-" + i);
                    if (spawn == null) continue;
                    var estimate = esSpawn.FirstOrDefault(e => e.Value == spawn);
                    if (estimate.Key != i && !NotificationManager.Has("SpawnEstimate-" + i))
                        NotificationManager.Set(new Notification
                        {
                            Uid = "SpawnEstimate-" + i,
                            Type = NotificationType.Warning,
                            Content = new Label(string.Format("The id of the spawn {0} will be changed from {1} to {2} at position {3}.\n"
                                + "This may be due to a duplicate element or another element already using this id.",
                                spawn.name, i, estimate.Key, i)),
                            Actions = new List<VisualElement>
                            {
                                new Button(() => descriptor.Spawns = descriptor.EstimateSpawns().Values.ToList()) { text = "Normalize" }
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
                            Content = new Label(string.Format("The scene at position {0} is null.", i)),
                            Actions = new List<VisualElement>
                            {
                                new Button(() => descriptor.Scenes = descriptor.EstimateScenes().Values.ToList()) { text = "Normalize" }
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
                            Content = new Label(string.Format("The id of the scene {0} will be changed from {1} to {2} at position {3}.\n"
                                + "This may be due to a duplicate element or another element already using this id.",
                                scene, i, estimate.Key, i)),
                            Actions = new List<VisualElement>
                            {
                                new Button(() => descriptor.Scenes = descriptor.EstimateScenes().Values.ToList()) { text = "Normalize" }
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

            var buildPlatform = descriptor != null ? descriptor.GetBuildPlatform() : SupportBuildTarget.NoTarget;
            if (buildPlatform == SupportBuildTarget.NoTarget && !NotificationManager.Has("NoBuildPlatform"))
                NotificationManager.Set(new Notification
                {
                    Uid = "NoBuildPlatform",
                    Type = NotificationType.Error,
                    Content = new Label("No build platform selected.\nA build platform is required."),
                    Actions = new List<VisualElement>
                    {
                        new Button(() => _root.Q<EnumField>("platform-field").value = SuppordTarget.GetCurrentTarget()) { text = "Detect" }
                    }
                });
            else if (buildPlatform != SupportBuildTarget.NoTarget && NotificationManager.Has("NoBuildPlatform"))
                NotificationManager.Remove("NoBuildPlatform");

            if (_root.childCount > 0)
            {
                _root.Q<ObjectField>("descriptor-field").value = descriptor;
                var target = _root.Q<EnumField>("platform-field");
                if (descriptor != null && descriptor.target != (SupportBuildTarget)target.value)
                    target.SetValueWithoutNotify(descriptor.target);

                var notifs = NotificationManager.Notifications;
                var hash = notifs.Aggregate("", (current, notification) => current + ":" + notification.Uid);
                if (hash != lasthashnotif)
                {
                    var notificationList = _root.Q<VisualElement>("notifications");
                    notificationList.Clear();
                    notifs.Sort((a, b) => a.Type.CompareTo(b.Type));
                    foreach (var notification in notifs)
                    {
                        var item = _mod._api.AssetAPI.GetLocalAsset<VisualTreeAsset>("notification").CloneTree();
                        item.Q<VisualElement>("content").Add(notification.Content);
                        if (item.Q<VisualElement>("typing") != null)
                            if (notification.Type == NotificationType.Error)
                                item.Q<Image>("icon").style.backgroundImage = _mod._api.AssetAPI.GetAsset<Texture2D>("api.nox.game", "icons/error");
                            else if (notification.Type == NotificationType.Warning)
                                item.Q<Image>("icon").style.backgroundImage = _mod._api.AssetAPI.GetAsset<Texture2D>("api.nox.game", "icons/warning");
                            else if (notification.Type == NotificationType.Info)
                                item.Q<Image>("icon").style.backgroundImage = _mod._api.AssetAPI.GetAsset<Texture2D>("api.nox.game", "icons/info");
                            else if (notification.Type == NotificationType.Good)
                                item.Q<Image>("icon").style.backgroundImage = _mod._api.AssetAPI.GetAsset<Texture2D>("api.nox.game", "icons/good");
                            else item.Q<VisualElement>("typing").style.display = DisplayStyle.None;
                        if (notification.Actions != null && notification.Actions.Count > 0)
                            foreach (var action in notification.Actions)
                                item.Q<VisualElement>("actionbox").Add(action);
                        else item.Q<VisualElement>("actions").style.display = DisplayStyle.None;
                        notificationList.Add(item);
                    }
                    var hasErrors = notifs.Any(n => n.Type == NotificationType.Error);
                    foreach (var element in _root.Query(null, "disable-on-error").ToList())
                        element.SetEnabled(!hasErrors);
                    lasthashnotif = hash;
                }
            }
        }

        public VisualElement OnOpenned(Dictionary<string, object> data)
        {
            lasthashnotif = "";
            _root.ClearBindings();
            _root.Clear();
            _root.Add(_mod._api.AssetAPI.GetLocalAsset<VisualTreeAsset>("builder").CloneTree());
            _root.Q<Label>("version").text = "v" + _mod._api.ModMetadata.GetVersion();
            var descriptor = Descriptors.Length > 0 ? Descriptors[0] : null;
            _root.Q<EnumField>("platform-field").Init(descriptor?.GetBuildPlatform() ?? SupportBuildTarget.NoTarget);
            _root.Q<ObjectField>("descriptor-field").value = descriptor;
            _root.Q<Button>("goto-publisher").clicked += () => _mod._api.PanelAPI.SetActivePanel("api.nox.world.publisher");
            _root.Q<EnumField>("platform-field").RegisterValueChangedCallback(e =>
            {
                var descriptor = Descriptors.Length > 0 ? Descriptors[0] : null;
                if (descriptor == null) return;
                descriptor.target = (SupportBuildTarget)e.newValue;
            });

            _root.Q<Button>("build-button").RegisterCallback<ClickEvent>(e =>
            {
                var descriptor = Descriptors.Length > 0 ? Descriptors[0] : null;
                if (descriptor == null)
                {
                    Debug.LogError("No world descriptor found.");
                    return;
                }
                var result = MainDescriptorEditor.BuildWorld(descriptor, descriptor.GetBuildPlatform(), false);
                if (result.Success)
                {
                    Debug.Log("Build success.");
                }
                else Debug.LogError(result.ErrorMessage);
            });
            return _root;
        }
        public void OnClosed()
        {
            Debug.Log("Panel Example closed!");
        }
    }
}
#endif