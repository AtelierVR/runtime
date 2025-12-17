/*using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using api.nox.game.UI;
using api.nox.network.Servers;
using api.nox.network.Worlds;
using api.nox.network.Worlds.Assets;
using Cysharp.Threading.Tasks;
using Logger = Nox.CCK.Utils.Logger;
using Nox.CCK.Mods.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Nox.CCK.Utils;
using Nox.CCK.Language;
using Transform = UnityEngine.Transform;

namespace api.nox.game.Tiles
{
    internal class MakeInstanceTileManager
    {
        private readonly EventSubscription _worldFetchSub;
        private readonly EventSubscription _worldAssetFetchSub;
        private readonly EventSubscription _serverFetchSub;

        [Serializable]
        public class WorldFetchedEvent : UnityEvent<World>
        {
        }

        [Serializable]
        public class WorldAssetFetchedEvent : UnityEvent<WorldAsset>
        {
        }

        [Serializable]
        public class ServerFetchedEvent : UnityEvent<Server>
        {
        }

        private WorldFetchedEvent _onWorldFetched;
        private WorldAssetFetchedEvent _onWorldAssetFetched;
        private ServerFetchedEvent _onServerFetched;
        
        internal MakeInstanceTileManager()
        {
            _worldFetchSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("world_fetch", OnFetchWorld);
            _worldAssetFetchSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("world_asset_fetch", OnFetchWorldAsset);
            _serverFetchSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("server_fetch", OnFetchServer);
            _onWorldFetched = new WorldFetchedEvent();
            _onWorldAssetFetched = new WorldAssetFetchedEvent();
            _onServerFetched = new ServerFetchedEvent();
        }

        internal void OnDispose()
        {
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_worldFetchSub);
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_worldAssetFetchSub);
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(_serverFetchSub);
            _onWorldFetched?.RemoveAllListeners();
            _onWorldAssetFetched?.RemoveAllListeners();
            _onServerFetched?.RemoveAllListeners();
            _onWorldFetched = null;
            _onWorldAssetFetched = null;
            _onServerFetched = null;
        }

        private class MakeInstanceTileObject : TileObject
        {
            public UnityAction<World> OnWorldFetched;
            public UnityAction<WorldAsset> OnWorldAssetFetched;
            public UnityAction<Server> OnServerFetched;

            public Server Server
            {
                get => GetData<Server>(0);
                set => SetData(0, value);
            }

            public World World
            {
                get => GetData<World>(1);
                set => SetData(1, value);
            }

            public WorldAsset Asset
            {
                get => GetData<WorldAsset>(2);
                set => SetData(2, value);
            }
        }

        private void OnFetchWorld(EventData context)
        {
            if (context.Data[0] is not World world) return;
            _onWorldFetched?.Invoke(world);
        }

        private void OnFetchWorldAsset(EventData context)
        {
            if (context.Data[0] is not WorldAsset asset) return;
            _onWorldAssetFetched?.Invoke(asset);
        }

        private void OnFetchServer(EventData context)
        {
            if (context.Data[0] is not Server server) return;
            _onServerFetched?.Invoke(server);
        }

        /// <summary>
        /// Send a tile to the menu system
        /// </summary>
        /// <param name="context"></param>
        internal void SendTile(EventData context)
        {
            Logger.Log("MakeInstanceTileManager.SendTile");
            var tile = new MakeInstanceTileObject() { id = "api.nox.game.instance.make", context = context };
            tile.GetContent = tf => OnGetContent(tile, tf);
            tile.onDisplay = (_, gameObject) => OnDisplay(tile, gameObject);
            tile.onOpen = _ => OnOpen(tile, tile.content);
            tile.onHide = _ => OnHide();
            MenuManager.Instance.SendTile(tile.MenuId, tile);
        }

        private GameObject OnGetContent(MakeInstanceTileObject tile, Transform tf)
        {
            Logger.Log("MakeInstanceTileManager.OnGetContent");
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/game.instance.make.prefab");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);

            if (tile.OnWorldFetched != null)
                _onWorldFetched.RemoveListener(tile.OnWorldFetched);
            tile.OnWorldFetched = world => OnWorldTileUpdate(tile, content, world);
            _onWorldFetched.AddListener(tile.OnWorldFetched);

            if (tile.OnWorldAssetFetched != null)
                _onWorldAssetFetched.RemoveListener(tile.OnWorldAssetFetched);
            tile.OnWorldAssetFetched = asset => OnWorldAssetTileUpdate(tile, content, asset);
            _onWorldAssetFetched.AddListener(tile.OnWorldAssetFetched);

            if (tile.OnServerFetched != null)
                _onServerFetched.RemoveListener(tile.OnServerFetched);
            tile.OnServerFetched = server => OnServerTileUpdate(tile, content, server);
            _onServerFetched.AddListener(tile.OnServerFetched);

            return content;
        }

        private static void OnHide()
        {
            Logger.Log("MakeInstanceTileManager.OnHide");
        }

        private void OnOpen(MakeInstanceTileObject tile, GameObject content)
        {
            Logger.Log("MakeInstanceTileManager.OnOpen");
            var world = tile.World;

            if (world == null)
            {
                Logger.LogError("World is null");
                return;
            }

            SetPasswordRequired(content, false);
            SetMinMaxCapacity(content, 0, world.capacity == 0 ? (uint)100 : world.capacity);
            SetCapacity(content, 0);
            SetExposition(content, 0);
        }

        private void OnDisplay(MakeInstanceTileObject tile, GameObject content)
        {
            Logger.Log("MakeInstanceTileManager.OnDisplay");
            UpdateContent(tile, content);
        }

        private void UpdateContent(MakeInstanceTileObject tile, GameObject content)
        {
            Logger.Log("MakeInstanceTileManager.UpdateContent");

            var capacity = Reference.GetReference("capacity_slider", content).GetComponent<Slider>();
            capacity.onValueChanged.RemoveAllListeners();
            capacity.onValueChanged.AddListener(_ => UpdateCapacity(content));


            var toggle = Reference.GetReference("password_toggle", content).GetComponent<Toggle>();
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(value => SetPasswordRequired(content, value));

            var button = Reference.GetReference("create_instance", content).GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnCreateClicked(tile, content).Forget());

            UpdateWorld(tile, content);
            UpdateServer(tile, content);
            UpdateWorldAsset(tile, content);
        }

        private void UpdateWorld(MakeInstanceTileObject tile, GameObject content)
        {
            var world = tile.World;
            var worldButton = Reference.GetReference("world_button", content).GetComponent<Button>();
            worldButton.onClick.RemoveAllListeners();
            Reference.GetReference("world_button_label", content).GetComponent<TextLanguage>()
                .UpdateText(new[] { world.title });
        }

        private void UpdateServer(MakeInstanceTileObject tile, GameObject content)
        {
            var server = tile.Server;
            var serverButton = Reference.GetReference("server_button", content).GetComponent<Button>();
            serverButton.onClick.RemoveAllListeners();
            Reference.GetReference("server_button_label", content).GetComponent<TextLanguage>().UpdateText(new[]
            {
                server?.title ?? "First available server"
            });
        }

        private void UpdateWorldAsset(MakeInstanceTileObject tile, GameObject content)
        {
            var asset = tile.Asset;
            var assetButton = Reference.GetReference("asset_button", content).GetComponent<Button>();
            assetButton.onClick.RemoveAllListeners();
            Reference.GetReference("asset_button_label", content).GetComponent<TextLanguage>().UpdateText(new []
            {
                asset?.version.ToString() ?? "Latest version"
            });
        }

        private void SetExposition(GameObject content, int index)
        {
            var dropdown = Reference.GetReference("expose_dropdown", content).GetComponent<TMPro.TMP_Dropdown>();
            dropdown.value = index;
        }

        private void SetMinMaxCapacity(GameObject content, uint min, uint max)
        {
            var slider = Reference.GetReference("capacity_slider", content).GetComponent<Slider>();
            if (slider.value < min) SetCapacity(content, min);
            if (slider.value > max) SetCapacity(content, max);
            slider.minValue = min;
            slider.maxValue = max;

            UpdateCapacity(content);
        }

        private void UpdateCapacity(GameObject content)
        {
            var slider = Reference.GetReference("capacity_slider", content).GetComponent<Slider>();
            var value = slider.value;
            var min = slider.minValue;
            var max = slider.maxValue;

            Reference.GetReference("capacity_value", content).GetComponent<TextLanguage>()
                .UpdateText(new[]
                {
                    value >= slider.maxValue
                        ? "Unlimited"
                        : (value <= slider.minValue ? "World Default" : value.ToString(CultureInfo.CurrentCulture))
                });

            Reference.GetReference("capacity_range", content).GetComponent<TextLanguage>()
                .UpdateText(new[] { min.ToString(CultureInfo.CurrentCulture), max.ToString(CultureInfo.CurrentCulture) });
        }


        private void SetCapacity(GameObject content, uint capacity)
        {
            var slider = Reference.GetReference("capacity_slider", content).GetComponent<Slider>();
            slider.value = capacity;

            UpdateCapacity(content);
        }


        private void SetPasswordRequired(GameObject content, bool required)
        {
            var toggle = Reference.GetReference("password_toggle", content).GetComponent<Toggle>();
            toggle.isOn = required;

            var input = Reference.GetReference("password_input", content).GetComponent<TMPro.TMP_InputField>();
            var show = Reference.GetReference("password_visibility", content).GetComponent<Button>();

            input.interactable = required;
            show.interactable = required;
            show.onClick.RemoveAllListeners();

            if (!required)
                SetPasswordVisibility(content, false);
            else
                show.onClick.AddListener(() =>
                    SetPasswordVisibility(content,
                        input.contentType == TMPro.TMP_InputField.ContentType.Password));
        }


        private void SetPasswordVisibility(GameObject content, bool visible)
        {
            Logger.Log("MakeInstanceTileManager.SetPasswordVisibility " + visible);
            var input = Reference.GetReference("password_input", content).GetComponent<TMPro.TMP_InputField>();
            var show = Reference.GetReference("password_visibility", content);

            input.contentType =
                visible ? TMPro.TMP_InputField.ContentType.Standard : TMPro.TMP_InputField.ContentType.Password;
            input.ForceLabelUpdate();

            var o = Reference.GetReference("password_hide", show);
            var p = Reference.GetReference("password_show", show);

            o.SetActive(visible);
            p.SetActive(!visible);
        }


        private async UniTask OnCreateClicked(MakeInstanceTileObject tile, GameObject content)
        {
            var button = Reference.GetReference("create_instance", content).GetComponent<Button>();
            if (!button.interactable) return;
            button.interactable = false;

            var password = Reference.GetReference("password_input", content).GetComponent<TMPro.TMP_InputField>().text;
            var usePassword = Reference.GetReference("password_toggle", content).GetComponent<Toggle>().isOn;
            var slider = Reference.GetReference("capacity_slider", content).GetComponent<Slider>();
            var expose = Reference.GetReference("expose_dropdown", content).GetComponent<TMPro.TMP_Dropdown>();

            var capacity = slider.value >= slider.maxValue
                ? ushort.MaxValue
                : slider.value <= slider.minValue
                    ? tile.World.capacity
                    : (ushort)slider.value;
            capacity = capacity == ushort.MinValue ? tile.World.capacity : capacity;

            var created = await GameClientSystem.NetworkAPI
                .GetField<INoxObject>("Instance")
                .CallMethod<UniTask<INoxObject>>("CreateInstance", new Dictionary<string, object>()
                {
                    { "World", tile.World.ToIdentifier().ToFullString(tile.World.server) },
                    { "Server", tile.Server?.address ?? GetHosts().FirstOrDefault() },
                    { "Password", password },
                    { "UsePassword", usePassword },
                    { "Capacity", capacity },
                    { "UseWhitelist", false },
                    { "Expose", expose.options[expose.value].text },
                    { "Title", tile.World.title },
                    { "Description", tile.World.description },
                    { "Thumbnail", tile.World.thumbnail }
                });

            if (created == null)
            {
                button.interactable = true;
                Logger.LogError("Failed to create instance");
                return;
            }

            MenuManager.Instance.SendGotoTile(tile.MenuId, "game.instance", created, tile.World, tile.Asset);
        }

        private void OnWorldTileUpdate(MakeInstanceTileObject tile, GameObject content, World world)
        {
            var cWorld = tile.World;
            if (cWorld == null) return;
            if (cWorld.id != world.id) return;
            if (cWorld.server != world.server) return;

            tile.World = world;
            UpdateWorld(tile, content);
        }

        private void OnWorldAssetTileUpdate(MakeInstanceTileObject tile, GameObject content, WorldAsset asset)
        {
            var cAsset = tile.Asset;
            if (cAsset == null) return;
            if (cAsset.id != asset.id) return;
            if (cAsset.server != asset.server) return;
            if (cAsset.version != asset.version) return;

            tile.Asset = asset;
            UpdateWorldAsset(tile, content);
        }

        private void OnServerTileUpdate(MakeInstanceTileObject tile, GameObject content, Server server)
        {
            var cServer = tile.Server;
            if (cServer == null) return;
            if (cServer.address != server.address) return;

            tile.Server = server;
            UpdateServer(tile, content);
        }

        private static string[] GetHosts()
        {
            var config = Config.Load();
            var serversT = config.Get("servers");
            if (serversT == null) return Array.Empty<string>();
            var serverD = serversT.ToObject<Dictionary<string, NavigationWorkerInfo>>();
            var servers = serverD.Values.ToArray();
            return servers.Where(x =>
                    (x.navigation || x.address == config.Get("server", ""))
                    && x.features.Contains("instance"))
                .Select(x => x.address).ToArray();
        }
    }
}*/