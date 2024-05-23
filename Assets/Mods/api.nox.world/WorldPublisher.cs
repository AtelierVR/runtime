// #if UNITY_EDITOR
// using System;
// using System.Linq;
// using Cysharp.Threading.Tasks;
// using Nox.CCK;
// using Nox.CCK.Editor;
// using Nox.CCK.Editor.Worlds;
// using Nox.CCK.Worlds;
// using UnityEditor;
// using UnityEditor.UIElements;
// using UnityEngine;
// using UnityEngine.UIElements;

// namespace api.nox.world
// {
//     public class WorldPublisher
//     {
//         internal VisualElement _root = new();
//         private CCKWorld CCK;
//         private DisplayFlags _displayFlags;

//         public WorldPublisher(CCKWorld world)
//         {
//             CCK = world;
//         }

//         public void OnLoad()
//         {
//             SetDisplay(DisplayFlags.NotLogged);
//             _displayFlags = DisplayFlags.None;
//             _lastDisplay = DisplayFlags.None;
//             _world = null;
//             _root.Clear();
//             _root.ClearBindings();
//         }

//         public void OnUnload()
//         {
//             _displayFlags = DisplayFlags.None;
//             _lastDisplay = DisplayFlags.None;
//             _world = null;
//             _root.Clear();
//             _root.ClearBindings();
//         }

//         private World _world = null;
//         private async UniTask OnLogged()
//         {
//             var descriptor = CCK.Descriptors.Length > 0 ? CCK.Descriptors[0] : null;
//             if (descriptor == null)
//             {
//                 SetDisplay(DisplayFlags.NoDescrpitor);
//                 return;
//             }
//             SetDisplay(DisplayFlags.Loading);
//             var worldId = descriptor.IdPublisher;
//             var serverAddress = descriptor.ServerPublisher;
//             await AttachWorld(serverAddress, worldId, false);
//         }

//         private async UniTask<World> AttachWorld(string server, uint id, bool create = false)
//         {
//             var descriptor = CCK.Descriptors.Length > 0 ? CCK.Descriptors[0] : null;
//             if (descriptor == null)
//             {
//                 SetDisplay(DisplayFlags.NoDescrpitor);
//                 _world = null;
//                 return null;
//             }
//             SetDisplay(DisplayFlags.Loading);
//             var world = await Net.GetWorld(server, id, true);
//             if (world == null && create)
//                 world = await Net.CreateWorld(new Net.CreateWorldData()
//                 {
//                     server = server,
//                     id = id,
//                 });

//             if (world == null)
//             {
//                 SetDisplay(DisplayFlags.WorldNotFound);
//                 _world = null;
//                 return null;
//             }
//             SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
//             descriptor.IdPublisher = world.id;
//             descriptor.ServerPublisher = world.server;
//             EditorUtility.SetDirty(descriptor);
//             _world = world;
//             UpdateWorld();
//             return world;
//         }

//         private void UpdateWorld()
//         {
//             if (_root.childCount == 0) return;
//             _root.Q<TextField>("info-server").value = _world != null ? _world.server : "";
//             _root.Q<UnsignedIntegerField>("info-id").value = _world != null ? _world.id : 0;
//             _root.Q<TextField>("info-title").value = _world != null ? _world.title : "";
//             _root.Q<TextField>("info-description").value = _world != null ? _world.description : "";
//             _root.Q<UnsignedIntegerField>("info-capacity").value = (ushort)(_world != null ? _world.capacity : 0);
//             var listtags = _root.Q<ListView>("info-tags");
//             listtags.makeItem = () =>
//             {
//                 var label = new Label();
//                 label.style.marginLeft = 4;
//                 label.style.marginRight = 4;
//                 return label;
//             };
//             listtags.bindItem = (e, i) => (e as Label).text = _world.tags[i];
//             listtags.itemsSource = _world != null ? _world.tags : new string[0];
//             return;
//         }


//         private DisplayFlags _lastDisplay = DisplayFlags.None;
//         public void OnUpdate()
//         {
//             if (!CCKPanel.HasTab("api.nox.world.publisher")) return;
//             if (_lastDisplay == DisplayFlags.NotLogged && Net.User != null)
//                 OnLogged().Forget();
//             if (_lastDisplay != DisplayFlags.NotLogged && Net.User == null)
//                 SetDisplay(DisplayFlags.NotLogged);
//             var descriptor = CCK.Descriptors.Length > 0 ? CCK.Descriptors[0] : null;
//             _root.Q<ObjectField>("descriptor-field").value = descriptor;
//             var version = descriptor?.VersionPublisher ?? 0;
//             var assetVersion = _root.Q<UnsignedIntegerField>("asset-version");
//             if (assetVersion.value != version)
//                 assetVersion.value = version;
//             var notif = NotificationManager.Notifications;
//             foreach (var type in Enum.GetValues(typeof(NotificationType)).Cast<NotificationType>())
//             {
//                 var container = _root.Q<VisualElement>("notfiication-container-" + type.ToString().ToLower());
//                 if (container == null) continue;
//                 var label = container.Q<Label>("notification-label-" + type.ToString().ToLower());
//                 var count = notif.Count(n => n.Type == type);
//                 container.style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
//                 label.text = count.ToString();
//             }
//             if (_lastDisplay != _displayFlags)
//             {
//                 SetDisplay(_displayFlags);
//                 _lastDisplay = _displayFlags;
//                 _root.Q<VisualElement>("world-not-found").style.display = _displayFlags.HasFlag(DisplayFlags.WorldNotFound) ? DisplayStyle.Flex : DisplayStyle.None;
//                 _root.Q<VisualElement>("world").style.display = _displayFlags.HasFlag(DisplayFlags.World) ? DisplayStyle.Flex : DisplayStyle.None;
//                 _root.Q<VisualElement>("world-asset").style.display = _displayFlags.HasFlag(DisplayFlags.WorldAsset) ? DisplayStyle.Flex : DisplayStyle.None;
//                 _root.Q<VisualElement>("not-logged").style.display = _displayFlags.HasFlag(DisplayFlags.NotLogged) ? DisplayStyle.Flex : DisplayStyle.None;

//             }
//         }

//         public void SetDisplay(DisplayFlags flags) => _displayFlags = flags;

//         public VisualElement OpenTab(bool selected = false)
//         {
//             if (!selected)
//             {
//                 _root.ClearBindings();
//                 _root.Clear();
//                 return null;
//             }
//             _root.ClearBindings();
//             _root.Clear();
//             _root.Add(Resources.Load<VisualTreeAsset>("api.nox.world.publisher").CloneTree());
//             _root.Q<Label>("version").text = "v" + CCK.Version;
//             var descriptor = CCK.Descriptors.Length > 0 ? CCK.Descriptors[0] : null;
//             _root.Q<EnumField>("platform-field").Init(descriptor?.GetBuildPlatform() ?? SuppordBuildTarget.NoTarget);
//             _root.Q<ObjectField>("descriptor-field").value = descriptor;
//             _root.Q<EnumField>("platform-field").RegisterValueChangedCallback(e =>
//             {
//                 var descriptor = CCK.Descriptors.Length > 0 ? CCK.Descriptors[0] : null;
//                 if (descriptor == null) return;
//                 descriptor.target = (SuppordBuildTarget)e.newValue;
//             });
//             _root.Q<Button>("goto-builder").clicked += () => CCKPanel.Goto("api.nox.world.builder");
//             var notifications = _root.Q<VisualElement>("notifications");
//             foreach (var type in new NotificationType[] { NotificationType.Error, NotificationType.Warning, NotificationType.Info })
//             {
//                 var container = Resources.Load<VisualTreeAsset>("api.nox.world.notification").CloneTree();
//                 container.style.display = DisplayStyle.None;
//                 container.style.marginLeft = 2;
//                 container.style.marginRight = 2;
//                 container.name = "notfiication-container-" + type.ToString().ToLower();
//                 container.Q<VisualElement>("content").Add(new Label(type.ToString()) { name = "notification-label-" + type.ToString().ToLower() });
//                 container.Q<VisualElement>("actions").style.display = DisplayStyle.None;
//                 container.Q<Image>("icon").image = Resources.Load<Texture2D>(type.ToString());
//                 notifications.Add(container);
//             }
//             _lastDisplay = DisplayFlags.None;
//             var attachId = _root.Q<UnsignedIntegerField>("attach-id");
//             var attachServer = _root.Q<TextField>("attach-server");
//             var attachButton = _root.Q<Button>("attach-world");
//             attachButton.clicked += async () =>
//             {
//                 var id = attachId.value;
//                 var server = attachServer.value;
//                 if (string.IsNullOrWhiteSpace(server))
//                     server = Net.User.server;
//                 await AttachWorld(server, id, true);
//             };
//             var infofetch = _root.Q<Button>("info-fetch");
//             infofetch.clicked += async () =>
//             {
//                 if (_world == null) return;
//                 var e = await AttachWorld(_world.server, _world.id);
//                 if (e == null)
//                     EditorUtility.DisplayDialog("Error", "An error occured while fetching the world.", "Ok");
//             };
//             var infoupdate = _root.Q<Button>("info-update");
//             infoupdate.clicked += async () =>
//             {
//                 Debug.Log("Update" + _world);
//                 if (_world == null || !_lastDisplay.HasFlag(DisplayFlags.World)) return;
//                 Debug.Log("Update1" + _world);
//                 var title = _root.Q<TextField>("info-title").value;
//                 var description = _root.Q<TextField>("info-description").value;
//                 var capacity = _root.Q<UnsignedIntegerField>("info-capacity").value;
//                 if (capacity > ushort.MaxValue)
//                 {
//                     EditorUtility.DisplayDialog("Error", "Capacity must be less than " + ushort.MaxValue, "Ok");
//                     return;
//                 }
//                 SetDisplay(DisplayFlags.Loading);
//                 var sucess = await Net.UpdateWorld(new Net.UpdateWorldData()
//                 {
//                     server = _world.server,
//                     id = _world.id,
//                     title = title,
//                     description = description,
//                     capacity = (ushort)capacity,
//                 }, true);
//                 if (sucess != null)
//                 {
//                     EditorUtility.DisplayDialog("Success", "World updated successfully.", "Ok");
//                     Debug.Log("World updated successfully.");
//                     Debug.Log(sucess.description);
//                     _world = sucess;
//                     UpdateWorld();
//                     SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
//                 }
//                 else EditorUtility.DisplayDialog("Error", "An error occured while updating the world.", "Ok");
//             };
//             var infodetach = _root.Q<Button>("info-detach");
//             infodetach.clicked += () =>
//             {
//                 if (_world == null || !_lastDisplay.HasFlag(DisplayFlags.World)) return;
//                 var descriptor = CCK.Descriptors.Length > 0 ? CCK.Descriptors[0] : null;
//                 if (descriptor == null) return;
//                 descriptor.IdPublisher = 0;
//                 descriptor.ServerPublisher = "";
//                 EditorUtility.SetDirty(descriptor);
//                 _world = null;
//                 UpdateWorld();
//                 SetDisplay(DisplayFlags.WorldNotFound);
//             };
//             var infodelete = _root.Q<Button>("info-delete");
//             infodelete.clicked += async () =>
//             {
//                 if (_world == null || !_lastDisplay.HasFlag(DisplayFlags.World)) return;
//                 var confirm = EditorUtility.DisplayDialog("Delete World", "Are you sure you want to delete this world?", "Yes", "No");
//                 if (!confirm) return;
//                 SetDisplay(DisplayFlags.Loading);
//                 var sucess = await Net.DeleteWorld(_world.server, _world.id);
//                 if (sucess)
//                 {
//                     _world = null;
//                     UpdateWorld();
//                     SetDisplay(DisplayFlags.WorldNotFound);
//                     EditorUtility.DisplayDialog("Success", "World deleted successfully.", "Ok");
//                 }
//                 else
//                 {
//                     EditorUtility.DisplayDialog("Error", "An error occured while deleting the world.", "Ok");
//                     SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
//                 }
//             };

//             var config = Config.Load();
//             _root.Q<Toggle>("asset-auto-version").value = config.Get("sdk.auto_version", true);
//             _root.Q<Toggle>("asset-auto-version").RegisterValueChangedCallback(e =>
//             {
//                 var config = Config.Load();
//                 config.Set("sdk.auto_version", e.newValue);
//                 config.Save();
//             });
//             _root.Q<Toggle>("asset-strict").value = config.Get("sdk.strict_version", true);
//             _root.Q<Toggle>("asset-strict").RegisterValueChangedCallback(e =>
//             {
//                 var config = Config.Load();
//                 config.Set("sdk.strict_version", e.newValue);
//                 config.Save();
//             });
//             _root.Q<UnsignedIntegerField>("asset-version").RegisterValueChangedCallback(e =>
//             {
//                 var descriptor = CCK.Descriptors.Length > 0 ? CCK.Descriptors[0] : null;
//                 if (descriptor == null) return;
//                 descriptor.VersionPublisher = (ushort)e.newValue;
//                 EditorUtility.SetDirty(descriptor);
//             });
//             _root.Q<Button>("publish-button").clicked += OnPublish;
//             SetDisplay(DisplayFlags.NotLogged);
//             UpdateWorld();
//             return _root;
//         }

//         private async void OnPublish()
//         {
//             var descriptor = CCK.Descriptors.Length > 0 ? CCK.Descriptors[0] : null;
//             if (descriptor == null)
//             {
//                 Debug.LogError("No descriptor found.");
//                 return;
//             }
//             var version = _root.Q<UnsignedIntegerField>("asset-version").value;
//             if (version > ushort.MaxValue)
//             {
//                 Debug.LogError("Version must be less than " + ushort.MaxValue);
//                 return;
//             }

//             Debug.Log("Checking world...");
//             SetDisplay(DisplayFlags.Loading);
//             _world = await Net.GetWorld(_world.server, _world.id, true);
//             if (_world == null)
//             {
//                 Debug.LogError("An error occured while fetching the world.");
//                 SetDisplay(DisplayFlags.WorldNotFound);
//                 return;
//             }
//             foreach (var assetl in _world.assets)
//                 Debug.Log(assetl);


//             var config = Config.Load();
//             var autoVersion = config.Get("sdk.auto_version", true);
//             var strictVersion = config.Get("sdk.strict_version", true);
//             var asset = _world.GetAsset((ushort)version, descriptor.target);
//             if (asset != null && autoVersion && !asset.IsEmpty())
//                 while (asset != null && !asset.IsEmpty())
//                 {
//                     version++;
//                     asset = _world.GetAsset((ushort)version, descriptor.target);
//                 }
//             _root.Q<UnsignedIntegerField>("asset-version").value = version;
//             if (asset != null && strictVersion && !asset.IsEmpty())
//             {
//                 Debug.LogError("Asset already exists.");
//                 Debug.LogError("Asset: " + asset);
//                 SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
//                 return;
//             }

//             Debug.Log("Building world...");
//             var result = MainDescriptorEditor.BuildWorld(descriptor, descriptor.GetBuildPlatform(), false);
//             if (result == null || !result.Success || string.IsNullOrWhiteSpace(result.path))
//             {
//                 Debug.LogError("An error occured while building the world.");
//                 SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
//                 return;
//             }

//             Debug.Log("Uploading asset...");
//             Debug.Log("Asset version: " + version);
//             Debug.Log("Asset platform: " + SuppordTarget.GetTargetName(descriptor.target));

//             if (asset == null)
//                 asset = await Net.CreateAsset(new Net.CreateAssetData()
//                 {
//                     worldId = _world.id,
//                     server = _world.server,
//                     version = (ushort)version,
//                     engine = "unity",
//                     platform = SuppordTarget.GetTargetName(descriptor.target),
//                 });

//             if (asset == null || !asset.IsEmpty())
//             {
//                 Debug.LogError("An error occured while creating the asset.");
//                 SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
//                 return;
//             }

//             var res = await Net.UploadAssetFile(_world.server, _world.id, asset.id, result.path);
//             if (!res)
//             {
//                 Debug.LogError("An error occured while uploading the asset.");
//                 SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
//                 return;
//             }

//             Debug.Log("Asset created successfully, Refreshing world...");
//             _world = await Net.GetWorld(_world.server, _world.id, true);
//             if (_world == null)
//             {
//                 Debug.LogError("An error occured while fetching the world.");
//                 SetDisplay(DisplayFlags.WorldNotFound);
//                 return;
//             }

//             Debug.Log("Asset created successfully.");
//             SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
//         }
//     }

//     [Flags]
//     public enum DisplayFlags
//     {
//         None = 0,
//         NotLogged = 1,
//         WorldNotFound = 2,
//         World = 4,
//         WorldAsset = 8,
//         Loading = 16,
//         NoDescrpitor = 32
//     }
// }
// #endif