#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.network.Worlds;
using api.nox.network.Worlds.Assets;
using Cysharp.Threading.Tasks;
using Nox.CCK.Worlds;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using UnityEngine.UIElements;
using Nox.CCK.Mods.Panels;
using Nox.CCK.Utils;

namespace api.nox.world
{
    public class WorldPublisherPanel : EditorPanelBuilder
    {
        public string Id { get; } = "publisher";
        public string Name { get; } = "World/Publisher";
        public bool Hidded { get; } = false;
        private readonly VisualElement _root = new();
        internal MainDescriptor[] Descriptors => MainDescriptorEditor.GetWorldDescriptors(false);
        private readonly WorldEditorMod _mod;
        private DisplayFlags _displayFlags;
        private DisplayFlags _lastDisplay;
        private World _world;

        internal WorldPublisherPanel(WorldEditorMod mod) => _mod = mod;
        private void SetDisplay(DisplayFlags flags) => _displayFlags = flags;

        public void OnClosed()
        {
            Logger.Log("Panel Example closed!");
        }

        internal void OnUpdate()
        {
            if (!_mod.HasOnePanelOpenned() || _root.childCount == 0) return;
            var user = _mod.NetworkAPI.GetField("User").CallMethod("GetCurrentUser");
            if (_lastDisplay == DisplayFlags.NotLogged && user != null)
                OnLogged().Forget();
            if (_lastDisplay != DisplayFlags.NotLogged && user == null)
                SetDisplay(DisplayFlags.NotLogged);
            var descriptor = _mod._builder.Descriptors.Length > 0 ? _mod._builder.Descriptors[0] : null;
            _root.Q<ObjectField>("descriptor-field").value = descriptor;
            var version = descriptor?.VersionPublisher ?? 0;
            var assetVersion = _root.Q<UnsignedIntegerField>("asset-version");
            if (assetVersion.value != version)
                assetVersion.value = version;
            var notify = NotificationManager.Notifications;
            foreach (var type in Enum.GetValues(typeof(NotificationType)).Cast<NotificationType>())
            {
                var container = _root.Q<VisualElement>("notification-container-" + type.ToString().ToLower());
                if (container == null) continue;
                var label = container.Q<Label>("notification-label-" + type.ToString().ToLower());
                var count = notify.Count(n => n.Type == type);
                container.style.display = count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                label.text = count.ToString();
            }

            if (_lastDisplay != _displayFlags)
            {
                SetDisplay(_displayFlags);
                _lastDisplay = _displayFlags;
                _root.Q<VisualElement>("world-not-found").style.display =
                    _displayFlags.HasFlag(DisplayFlags.WorldNotFound) ? DisplayStyle.Flex : DisplayStyle.None;
                _root.Q<VisualElement>("world").style.display = _displayFlags.HasFlag(DisplayFlags.World)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                _root.Q<VisualElement>("world-asset").style.display = _displayFlags.HasFlag(DisplayFlags.WorldAsset)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                _root.Q<VisualElement>("not-logged").style.display = _displayFlags.HasFlag(DisplayFlags.NotLogged)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        private async UniTask OnLogged()
        {
            var descriptor = _mod._builder.Descriptors.Length > 0 ? _mod._builder.Descriptors[0] : null;
            if (!descriptor)
            {
                SetDisplay(DisplayFlags.NoDescriptor);
                return;
            }

            SetDisplay(DisplayFlags.Loading);
            var worldId = descriptor.IdPublisher;
            var serverAddress = descriptor.ServerPublisher;
            await AttachWorld(serverAddress, worldId);
        }

        private async UniTask<World> AttachWorld(string server, uint id, bool create = false)
        {
            return await AttachWorld(server, id.ToString(), create);
        }


        private async UniTask<World> AttachWorld(string server, string id, bool create = false)
        {
            var descriptor = _mod._builder.Descriptors.Length > 0 ? _mod._builder.Descriptors[0] : null;
            if (!descriptor)
            {
                SetDisplay(DisplayFlags.NoDescriptor);
                _world = null;
                return null;
            }

            SetDisplay(DisplayFlags.Loading);

            World world = null;
            if (!string.IsNullOrWhiteSpace(id) && uint.TryParse(id, out var idParsed))
                world = await _mod.NetworkAPI.GetField<INoxObject>("World")
                    .CallAsyncMethod<World>("GetWorld", server, idParsed);

            if (world == null && create)
                world = await _mod.NetworkAPI.GetField("World")
                    .CallAsyncMethod<World>("CreateWorld",
                        !string.IsNullOrWhiteSpace(id) && uint.TryParse(id, out var id1)
                            ? new Dictionary<string, object>
                            {
                                { "server", server },
                                { "id", id1 },
                                { "custom_id", true }
                            }
                            : new Dictionary<string, object> { { "server", server } }
                    );

            if (world != null)
            {
                var user = _mod.NetworkAPI.GetField("User").CallMethod("GetCurrentUser");
                if (user == null || !user.CallMethod<bool>("MatchRef", world.owner, world.server))
                    world = null;
            }

            if (world == null)
            {
                if (create)
                {
                    EditorUtility.DisplayDialog("Error", "An error occured while creating the world.", "Ok");
                    Logger.LogError(
                        "An error occured while creating the world, please check the server and your permissions.");
                }

                SetDisplay(DisplayFlags.WorldNotFound);
                _world = null;
                return null;
            }

            SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
            descriptor.IdPublisher = world.id;
            descriptor.ServerPublisher = world.server;
            EditorUtility.SetDirty(descriptor);
            _world = world;
            UpdateWorld();
            return world;
        }

        private void UpdateWorld()
        {
            if (_root.childCount == 0) return;
            _root.Q<TextField>("info-server").value = _world != null ? _world.server : "";
            _root.Q<UnsignedIntegerField>("info-id").value = _world?.id ?? 0;
            _root.Q<TextField>("info-title").value = _world != null ? _world.title : "";
            _root.Q<TextField>("info-description").value = _world != null ? _world.description : "";
            _root.Q<UnsignedIntegerField>("info-capacity").value = (_world?.capacity ?? 0);
            var tagsList = _root.Q<ListView>("info-tags");
            tagsList.makeItem = () => new Label { style = { marginLeft = 4, marginRight = 4 } };
            tagsList.bindItem = (e, i) => ((Label)e).text = _world.tags[i];
            tagsList.itemsSource = _world != null ? _world.tags : Array.Empty<string>();
        }

        public VisualElement OnOpenned(Dictionary<string, object> data)
        {
            _root.ClearBindings();
            _root.Clear();

            var child = _mod._api.AssetAPI.GetAsset<VisualTreeAsset>("publisher.uxml").CloneTree();
            child.style.flexGrow = 1;
            _root.Add(child);

            _root.Q<Label>("version").text = "v" + _mod._api.ModMetadata.GetVersion();
            _root.Q<Image>("assigned-icon").image = _mod._api.AssetAPI.GetAsset<Texture2D>("game", "icons/warning.png");
            var descriptor = _mod._builder.Descriptors.Length > 0 ? _mod._builder.Descriptors[0] : null;
            _root.Q<EnumField>("platform-field").Init(descriptor?.GetBuildPlatform() ?? Platform.None);
            _root.Q<Button>("detect-platform").clicked += () =>
                _root.Q<EnumField>("platform-field").value = PlatformExtensions.GetCurrentTarget().GetPlatform();
            _root.Q<ObjectField>("descriptor-field").value = descriptor;
            _root.Q<EnumField>("platform-field").RegisterValueChangedCallback(e =>
            {
                var mainDescriptor = _mod._builder.Descriptors.Length > 0 ? _mod._builder.Descriptors[0] : null;
                if (!mainDescriptor) return;
                var plat = (Platform)e.newValue;
                if (plat.IsSupported())
                    mainDescriptor.Target = plat;
                else
                {
                    EditorUtility.DisplayDialog("Error", $"{plat.GetPlatformName()} is not supported.", "Ok");
                    Logger.LogError(
                        $"Platform \"{plat.GetPlatformName()}\" ({plat.GetBuildTarget()}) is not supported.");
                    _root.Q<EnumField>("platform-field").value = e.previousValue;
                }
            });
            _root.Q<Button>("goto-builder").clicked += () => _mod._api.PanelAPI.SetActivePanel("api.nox.world.builder");
            _root.Q<Button>("goto-login").clicked += () => _mod._api.PanelAPI.SetActivePanel("api.nox.user.login");
            var notifications = _root.Q<VisualElement>("notifications");
            foreach (var type in new[] { NotificationType.Error, NotificationType.Warning, NotificationType.Info })
            {
                var container = _mod._api.AssetAPI.GetAsset<VisualTreeAsset>("notification.uxml").CloneTree();
                container.style.display = DisplayStyle.None;
                container.style.marginLeft = 2;
                container.style.marginRight = 2;
                container.name = "notification-container-" + type.ToString().ToLower();
                container.Q<VisualElement>("content").Add(new Label(type.ToString())
                    { name = "notification-label-" + type.ToString().ToLower() });
                container.Q<VisualElement>("actions").style.display = DisplayStyle.None;
                container.Q<Image>("icon").image =
                    _mod._api.AssetAPI.GetAsset<Texture2D>("game", "icons/" + type.ToString() + ".png");
                notifications.Add(container);
            }

            _lastDisplay = DisplayFlags.None;
            var attachId = _root.Q<TextField>("attach-id");
            var attachServer = _root.Q<TextField>("attach-server");
            var attachButton = _root.Q<Button>("attach-world");
            attachButton.clicked += async () =>
            {
                var id = attachId.value;
                var server = attachServer.value;
                if (string.IsNullOrWhiteSpace(server))
                    server = _mod.NetworkAPI.GetField("Auth").CallMethod<string>("GetCurrentServerAddress");
                await AttachWorld(server, id, true);
            };
            var fetchInfoButton = _root.Q<Button>("info-fetch");
            fetchInfoButton.clicked += async () =>
            {
                if (_world == null) return;
                var e = await AttachWorld(_world.server, _world.id);
                if (e == null)
                    EditorUtility.DisplayDialog("Error", "An error occured while fetching the world.", "Ok");
            };
            var updateInfoButton = _root.Q<Button>("info-update");
            updateInfoButton.clicked += async () =>
            {
                Logger.Log("Update" + _world);
                if (_world == null || !_lastDisplay.HasFlag(DisplayFlags.World)) return;
                Logger.Log("Update1" + _world);
                var title = _root.Q<TextField>("info-title").value;
                var description = _root.Q<TextField>("info-description").value;
                var capacity = _root.Q<UnsignedIntegerField>("info-capacity").value;
                if (capacity > ushort.MaxValue)
                {
                    EditorUtility.DisplayDialog("Error", "Capacity must be less than " + ushort.MaxValue, "Ok");
                    return;
                }

                SetDisplay(DisplayFlags.Loading);
                var success = await _mod.NetworkAPI.GetField("World").CallAsyncMethod<World>("UpdateWorld",
                    new Dictionary<string, object>
                    {
                        { "server", _world.server },
                        { "world_id", _world.id },
                        { "title", title },
                        { "description", description },
                        { "capacity", (ushort)capacity }
                    });
                if (success != null)
                {
                    EditorUtility.DisplayDialog("Success", "World updated successfully.", "Ok");
                    Logger.Log("World updated successfully.");
                    Logger.Log(success.description);
                    _world = success;
                    UpdateWorld();
                    SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
                }
                else EditorUtility.DisplayDialog("Error", "An error occured while updating the world.", "Ok");
            };
            var detachInfoButton = _root.Q<Button>("info-detach");
            detachInfoButton.clicked += () =>
            {
                if (_world == null || !_lastDisplay.HasFlag(DisplayFlags.World)) return;
                var target = _mod._builder.Descriptors.Length > 0 ? _mod._builder.Descriptors[0] : null;
                if (!target) return;
                target.IdPublisher = 0;
                target.ServerPublisher = "";
                EditorUtility.SetDirty(target);
                _world = null;
                UpdateWorld();
                SetDisplay(DisplayFlags.WorldNotFound);
            };
            // var deleteInfoButton = _root.Q<Button>("info-delete");
            // deleteInfoButton.clicked += async () =>
            // {
            //     if (_world == null || !_lastDisplay.HasFlag(DisplayFlags.World)) return;
            //     var confirm = EditorUtility.DisplayDialog("Delete World", "Are you sure you want to delete this world?", "Yes", "No");
            //     if (!confirm) return;
            //     SetDisplay(DisplayFlags.Loading);
            //     // var success = await _mod._api.NetworkAPI.WorldAPI.DeleteWorld(_world.server, _world.id);
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

            var config = Config.Load();
            _root.Q<Toggle>("asset-auto-version").value = config.Get("sdk.auto_version", true);
            _root.Q<Toggle>("asset-auto-version").RegisterValueChangedCallback(e =>
            {
                var load = Config.Load();
                load.Set("sdk.auto_version", e.newValue);
                load.Save();
            });
            _root.Q<Toggle>("asset-strict").value = config.Get("sdk.strict_version", true);
            _root.Q<Toggle>("asset-strict").RegisterValueChangedCallback(e =>
            {
                var load = Config.Load();
                load.Set("sdk.strict_version", e.newValue);
                load.Save();
            });
            _root.Q<UnsignedIntegerField>("asset-version").RegisterValueChangedCallback(e =>
            {
                var target = _mod._builder.Descriptors.Length > 0 ? _mod._builder.Descriptors[0] : null;
                if (!target) return;
                target.VersionPublisher = (ushort)e.newValue;
                EditorUtility.SetDirty(target);
            });
            _root.Q<Button>("publish-button").clicked += () => OnPublishAsync().Forget();
            SetDisplay(DisplayFlags.NotLogged);
            UpdateWorld();
            return _root;
        }

        private async UniTask OnPublishAsync()
        {
            var descriptor = _mod._builder.Descriptors.Length > 0 ? _mod._builder.Descriptors[0] : null;
            if (!descriptor)
            {
                EditorUtility.DisplayDialog("Error", "No descriptor found.", "Ok");
                Logger.LogError("No descriptor found.");
                return;
            }

            var target = descriptor.GetBuildPlatform();
            if (!target.IsSupported())
            {
                EditorUtility.DisplayDialog("Error", $"{target.GetPlatformName()} is not supported.", "Ok");
                Logger.LogError(
                    $"Platform \"{target.GetPlatformName()}\" ({target.GetBuildTarget()}) is not supported.");
                return;
            }

            var version = (ushort)_root.Q<UnsignedIntegerField>("asset-version").value;

            if (version >= ushort.MaxValue)
            {
                EditorUtility.DisplayDialog("Error", "Version must be less than " + ushort.MaxValue, "Ok");
                Logger.LogError("Version must be less than " + ushort.MaxValue);
                return;
            }

            Logger.Log("Checking world...");
            SetDisplay(DisplayFlags.Loading);
            var worldAPI = _mod.NetworkAPI.GetField("World");
            _world = await worldAPI.CallAsyncMethod<World>("GetWorld", _world.server, _world.id);
            if (_world == null)
            {
                EditorUtility.DisplayDialog("Error", "An error occured while fetching the world.", "Ok");
                Logger.LogError("An error occured while fetching the world.");
                SetDisplay(DisplayFlags.WorldNotFound);
                return;
            }

            var config = Config.Load();
            var autoVersion = config.Get("sdk.auto_version", true);
            var strictVersion = config.Get("sdk.strict_version", true);

            var assetAPI = _mod.NetworkAPI.GetField("Asset");
            var search = await assetAPI.CallAsyncMethod<SearchResponse>("SearchAssets",
                new Dictionary<string, object>
                {
                    { "server", _world.server },
                    { "world_id", _world.id },
                    { "versions", new[] { version } },
                    { "platforms", new[] { target.GetPlatformName() } },
                    { "engines", new[] { Constants.CurrentEngine.GetEngineName() } },
                    { "with_empty", true },
                    { "limit", 1 },
                    { "offset", 0 }
                });

            if (search == null)
            {
                EditorUtility.DisplayDialog("Error", "An error occured while fetching the assets.", "Ok");
                Logger.LogError("An error occured while fetching the assets.");
                SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
                return;
            }

            var asset = search.assets.FirstOrDefault();
            if (asset != null && autoVersion && !asset.IsEmpty())
                while (asset != null && !asset.IsEmpty())
                {
                    version++;
                    search = await assetAPI.CallAsyncMethod<SearchResponse>("SearchAssets",
                        new Dictionary<string, object>
                        {
                            { "server", _world.server },
                            { "world_id", _world.id },
                            { "versions", new[] { version } },
                            { "platforms", new[] { target.GetPlatformName() } },
                            { "engines", new[] { Constants.CurrentEngine.GetEngineName() } },
                            { "with_empty", true },
                            { "limit", 1 },
                            { "offset", 0 }
                        });
                    if (search == null)
                    {
                        EditorUtility.DisplayDialog("Error", "An error occured while fetching the assets.", "Ok");
                        Logger.LogError("An error occured while fetching the assets.");
                        SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
                        return;
                    }

                    asset = search.assets.FirstOrDefault();
                }

            _root.Q<UnsignedIntegerField>("asset-version").value = version;
            if (asset != null && strictVersion && !asset.IsEmpty())
            {
                EditorUtility.DisplayDialog("Error", "Asset already exists.", "Ok");
                Logger.LogError("Asset already exists.");
                Logger.LogError("Asset: " + asset);
                SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
                return;
            }

            Logger.Log("Building world...");
            var result = MainDescriptorEditor.BuildWorld(descriptor, target.GetBuildTarget(), false);
            if (result == null || !result.Success || string.IsNullOrWhiteSpace(result.path))
            {
                EditorUtility.DisplayDialog("Error", "An error occured while building the world.", "Ok");
                Logger.LogError("An error occured while building the world.");
                SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
                return;
            }

            Logger.Log("Uploading asset...");
            Logger.Log("Asset version: " + version);
            Logger.Log($"Asset platform: {target.GetPlatformName()} ({target.GetBuildTarget()})");

            asset ??= await assetAPI.CallAsyncMethod<WorldAsset>("CreateAsset",
                new Dictionary<string, object>
                {
                    { "server", _world.server },
                    { "world_id", _world.id },
                    { "version", version },
                    { "engine", Constants.CurrentEngine.GetEngineName() },
                    { "platform", target.GetPlatformName() }
                });

            if (asset == null)
            {
                EditorUtility.DisplayDialog("Error", "An error occured while creating the asset.", "Ok");
                Logger.LogError("An error occured while creating the asset.");
                SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
                return;
            }

            var res = await assetAPI.CallAsyncMethod<bool>("UploadAssetFile",
                _world.server, _world.id, asset.id, result.path
            );

            if (!res)
            {
                EditorUtility.DisplayDialog("Error", "An error occured while uploading the asset.", "Ok");
                Logger.LogError("An error occured while uploading the asset.");
                SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
                return;
            }

            Logger.Log("Asset created successfully, Refreshing world...");
            _world = await worldAPI.CallAsyncMethod<World>("GetWorld", _world.server, _world.id);
            if (_world == null)
            {
                EditorUtility.DisplayDialog("Error", "An error occured while fetching the world.", "Ok");
                Logger.LogError("An error occured while fetching the world.");
                SetDisplay(DisplayFlags.WorldNotFound);
                return;
            }

            EditorUtility.DisplayDialog("Success", "Asset created successfully.", "Ok");
            Logger.Log("Asset created successfully.");
            SetDisplay(DisplayFlags.World | DisplayFlags.WorldAsset);
        }
    }

    [Flags]
    public enum DisplayFlags
    {
        None = 0,
        NotLogged = 1,
        WorldNotFound = 2,
        World = 4,
        WorldAsset = 8,
        Loading = 16,
        NoDescriptor = 32
    }
}
#endif