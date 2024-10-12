using System;
using System.Linq;
using api.nox.game.UI;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using Nox.SimplyLibs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace api.nox.game.Tiles
{
    internal class MakeInstanceTileManager
    {
        private EventSubscription WorldFetchSub;
        private EventSubscription WorldAssetFetchSub;
        private EventSubscription ServerFetchSub;
        [Serializable] public class WorldFetchedEvent : UnityEvent<SimplyWorld> { }
        [Serializable] public class WorldAssetFetchedEvent : UnityEvent<SimplyWorldAsset> { }
        [Serializable] public class ServerFetchedEvent : UnityEvent<SimplyServer> { }
        public WorldFetchedEvent OnWorldFetched;
        public WorldAssetFetchedEvent OnWorldAssetFetched;
        public ServerFetchedEvent OnServerFetched;


        internal MakeInstanceTileManager()
        {
            WorldFetchSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("world_fetch", OnFetchWorld);
            WorldAssetFetchSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("world_asset_fetch", OnFetchWorldAsset);
            ServerFetchSub = GameClientSystem.CoreAPI.EventAPI.Subscribe("server_fetch", OnFetchServer);
            OnWorldFetched = new WorldFetchedEvent();
            OnWorldAssetFetched = new WorldAssetFetchedEvent();
            OnServerFetched = new ServerFetchedEvent();
        }

        internal void OnDispose()
        {
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(WorldFetchSub);
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(WorldAssetFetchSub);
            GameClientSystem.CoreAPI.EventAPI.Unsubscribe(ServerFetchSub);
            OnWorldFetched?.RemoveAllListeners();
            OnWorldAssetFetched?.RemoveAllListeners();
            OnServerFetched?.RemoveAllListeners();
            OnWorldFetched = null;
            OnWorldAssetFetched = null;
            OnServerFetched = null;
        }

        internal class MakeInstanceTileObject : TileObject
        {
            public UnityAction<SimplyWorld> OnWorldFetched;
            public UnityAction<SimplyWorldAsset> OnWorldAssetFetched;
            public UnityAction<SimplyServer> OnServerFetched;
            public SimplyServer Server
            {
                get => GetData<ShareObject>(0)?.Convert<SimplyServer>();
                set => SetData(0, value);
            }
            public SimplyWorld World
            {
                get => GetData<ShareObject>(1)?.Convert<SimplyWorld>();
                set => SetData(1, value);
            }
            public SimplyWorldAsset Asset
            {
                get => GetData<ShareObject>(2)?.Convert<SimplyWorldAsset>();
                set => SetData(2, value);
            }
        }

        private void OnFetchWorld(EventData context)
        {
            var world = (context.Data[0] as ShareObject).Convert<SimplyWorld>();
            if (world == null) return;
            OnWorldFetched?.Invoke(world);
        }

        private void OnFetchWorldAsset(EventData context)
        {
            var asset = (context.Data[0] as ShareObject).Convert<SimplyWorldAsset>();
            if (asset == null) return;
            OnWorldAssetFetched?.Invoke(asset);
        }

        private void OnFetchServer(EventData context)
        {
            var server = (context.Data[0] as ShareObject).Convert<SimplyServer>();
            if (server == null) return;
            OnServerFetched?.Invoke(server);
        }

        /// <summary>
        /// Send a tile to the menu system
        /// </summary>
        /// <param name="context"></param>
        internal void SendTile(EventData context)
        {
            Debug.Log("MakeInstanceTileManager.SendTile");
            var tile = new MakeInstanceTileObject() { id = "api.nox.game.instance.make", context = context };
            tile.GetContent = (Transform tf) => OnGetContent(tile, tf);
            tile.onDisplay = (str, gameObject) => OnDisplay(tile, gameObject);
            tile.onOpen = (str) => OnOpen(tile, tile.content);
            tile.onHide = (str) => OnHide(tile, tile.content);
            MenuManager.Instance.SendTile(tile.MenuId, tile);
        }

        private GameObject OnGetContent(MakeInstanceTileObject tile, Transform tf)
        {
            Debug.Log("MakeInstanceTileManager.OnGetContent");
            var pf = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.instance.make");
            pf.SetActive(false);
            var content = Object.Instantiate(pf, tf);

            if (tile.OnWorldFetched != null)
                OnWorldFetched.RemoveListener(tile.OnWorldFetched);
            tile.OnWorldFetched = (world) => OnWorldTileUpdate(tile, content, world);
            OnWorldFetched.AddListener(tile.OnWorldFetched);

            if (tile.OnWorldAssetFetched != null)
                OnWorldAssetFetched.RemoveListener(tile.OnWorldAssetFetched);
            tile.OnWorldAssetFetched = (asset) => OnWorldAssetTileUpdate(tile, content, asset);
            OnWorldAssetFetched.AddListener(tile.OnWorldAssetFetched);

            if (tile.OnServerFetched != null)
                OnServerFetched.RemoveListener(tile.OnServerFetched);
            tile.OnServerFetched = (server) => OnServerTileUpdate(tile, content, server);
            OnServerFetched.AddListener(tile.OnServerFetched);

            return content;
        }

        private void OnHide(MakeInstanceTileObject tile, GameObject content)
        {
            Debug.Log("MakeInstanceTileManager.OnHide");
        }

        private void OnOpen(MakeInstanceTileObject tile, GameObject content)
        {
            Debug.Log("MakeInstanceTileManager.OnOpen");
            var world = tile.World;
            var server = tile.Server;
            var asset = tile.Asset;

            if (world == null)
            {
                Debug.LogError("World is null");
                return;
            }

            SetPasswordRequired(tile, content, false);
            SetMinMaxCapacity(tile, content, 0, world.capacity == 0 ? (uint)100 : world.capacity);
            SetCapacity(tile, content, 0);
            SetExposition(tile, content, 0);
        }

        private void OnDisplay(MakeInstanceTileObject tile, GameObject content)
        {
            Debug.Log("MakeInstanceTileManager.OnDisplay");
            UpdateContent(tile, content);
        }

        private void UpdateContent(MakeInstanceTileObject tile, GameObject content)
        {
            Debug.Log("MakeInstanceTileManager.UpdateContent");

            var capacity = Reference.GetReference("capacity_slider", content).GetComponent<Slider>();
            capacity.onValueChanged.RemoveAllListeners();
            capacity.onValueChanged.AddListener((value) => UpdateCapacity(tile, content));

            
            var toggle = Reference.GetReference("password_toggle", content).GetComponent<Toggle>();
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener((value) => SetPasswordRequired(tile, content, value));

            var createbutton = Reference.GetReference("create_instance", content).GetComponent<Button>();
            createbutton.onClick.RemoveAllListeners();
            createbutton.onClick.AddListener(() => OnCreateClicked(tile, content).Forget());

            UpdateWorld(tile, content);
            UpdateServer(tile, content);
            UpdateWorldAsset(tile, content);
        }

        private void UpdateWorld(MakeInstanceTileObject tile, GameObject content)
        {
            var world = tile.World;
            var world_button = Reference.GetReference("world_button", content).GetComponent<Button>();
            world_button.onClick.RemoveAllListeners();
            Reference.GetReference("world_button_label", content).GetComponent<TextLanguage>().UpdateText(new string[] { world.title });


        }

        private void UpdateServer(MakeInstanceTileObject tile, GameObject content)
        {
            var server = tile.Server;
            var server_button = Reference.GetReference("server_button", content).GetComponent<Button>();
            server_button.onClick.RemoveAllListeners();
            Reference.GetReference("server_button_label", content).GetComponent<TextLanguage>().UpdateText(new string[] {
                server?.title ?? "First available server"
            });
        }

        private void UpdateWorldAsset(MakeInstanceTileObject tile, GameObject content)
        {
            var asset = tile.Asset;
            var asset_button = Reference.GetReference("asset_button", content).GetComponent<Button>();
            asset_button.onClick.RemoveAllListeners();
            Reference.GetReference("asset_button_label", content).GetComponent<TextLanguage>().UpdateText(new string[] {
                asset?.version.ToString() ?? "Lastest version"
            });
        }

        private void SetExposition(MakeInstanceTileObject tile, GameObject content, int index)
        {
            var dropdown = Reference.GetReference("expose_dropdown", content).GetComponent<TMPro.TMP_Dropdown>();
            dropdown.value = index;
        }

        private void SetMinMaxCapacity(MakeInstanceTileObject tile, GameObject content, uint min, uint max)
        {
            var slider = Reference.GetReference("capacity_slider", content).GetComponent<Slider>();
            if (slider.value < min) SetCapacity(tile, content, min);
            if (slider.value > max) SetCapacity(tile, content, max);
            slider.minValue = min;
            slider.maxValue = max;

            UpdateCapacity(tile, content);
        }

        private void UpdateCapacity(MakeInstanceTileObject tile, GameObject content)
        {
            var slider = Reference.GetReference("capacity_slider", content).GetComponent<Slider>();
            var value = slider.value;
            var min = slider.minValue;
            var max = slider.maxValue;

            Reference.GetReference("capacity_value", content).GetComponent<TextLanguage>()
                .UpdateText(new string[] { value >= slider.maxValue ? "Unlimited" : (value <= slider.minValue ? "World Default" : value.ToString()) });

            Reference.GetReference("capacity_range", content).GetComponent<TextLanguage>()
                .UpdateText(new string[] { min.ToString(), max.ToString() });
        }


        private void SetCapacity(MakeInstanceTileObject tile, GameObject content, uint capacity)
        {
            var slider = Reference.GetReference("capacity_slider", content).GetComponent<Slider>();
            slider.value = capacity;

            UpdateCapacity(tile, content);
        }
















        private void SetPasswordRequired(MakeInstanceTileObject tile, GameObject content, bool required)
        {
            var toggle = Reference.GetReference("password_toggle", content).GetComponent<Toggle>();
            toggle.isOn = required;

            var input = Reference.GetReference("password_input", content).GetComponent<TMPro.TMP_InputField>();
            var show = Reference.GetReference("password_visibility", content).GetComponent<Button>();

            input.interactable = required;
            show.interactable = required;
            show.onClick.RemoveAllListeners();

            if (!required)
                SetPasswordVisibility(tile, content, false);
            else show.onClick.AddListener(() => SetPasswordVisibility(tile, content, input.contentType == TMPro.TMP_InputField.ContentType.Password));
        }


        private void SetPasswordVisibility(MakeInstanceTileObject tile, GameObject content, bool visible)
        {
            Debug.Log("MakeInstanceTileManager.SetPasswordVisibility " + visible);
            var input = Reference.GetReference("password_input", content).GetComponent<TMPro.TMP_InputField>();
            var show = Reference.GetReference("password_visibility", content);

            input.contentType = visible ? TMPro.TMP_InputField.ContentType.Standard : TMPro.TMP_InputField.ContentType.Password;
            input.ForceLabelUpdate();

            var hideicon = Reference.GetReference("password_hide", show);
            var showicon = Reference.GetReference("password_show", show);

            hideicon.SetActive(visible);
            showicon.SetActive(!visible);
        }



        private async UniTask OnCreateClicked(MakeInstanceTileObject tile, GameObject content)
        {
            var createbutton = Reference.GetReference("create_instance", content).GetComponent<Button>();
            if (!createbutton.interactable) return;
            createbutton.interactable = false;

            var password = Reference.GetReference("password_input", content).GetComponent<TMPro.TMP_InputField>().text;
            var use_password = Reference.GetReference("password_toggle", content).GetComponent<Toggle>().isOn;
            var capacity = Reference.GetReference("capacity_slider", content).GetComponent<Slider>().value;
            var expose = Reference.GetReference("expose_dropdown", content).GetComponent<TMPro.TMP_Dropdown>();

            var instance = new SimplyCreateInstanceData()
            {
                world = tile.World.ToMinimalString(tile.Server?.address),
                server = tile.Server?.address ?? GetHosts().FirstOrDefault(),
                password = password,
                use_password = use_password,
                capacity = capacity == 0 ? tile.World.capacity : (capacity == tile.World.capacity ? (ushort)0 : (ushort)capacity),
                use_whitelist = false,
                expose = expose.options[expose.value].text,
                title = tile.World.title,
                description = tile.World.description,
                thumbnail = tile.World.thumbnail
            };

            var created = await GameClientSystem.Instance.NetworkAPI.Instance.CreateInstance(instance);
            if (created == null)
            {
                createbutton.interactable = true;
                Debug.LogError("Failed to create instance");
                return;
            }

            MenuManager.Instance.SendGotoTile(tile.MenuId, "game.instance", created, tile.World, tile.Asset);
        }

        private void OnWorldTileUpdate(MakeInstanceTileObject tile, GameObject content, SimplyWorld world)
        {
            var cWorld = tile.World;
            if (cWorld == null) return;
            if (cWorld.id != world.id) return;
            if (cWorld.server != world.server) return;

            tile.World = world;
            UpdateWorld(tile, content);
        }

        private void OnWorldAssetTileUpdate(MakeInstanceTileObject tile, GameObject content, SimplyWorldAsset asset)
        {
            var cAsset = tile.Asset;
            if (cAsset == null) return;
            if (cAsset.id != asset.id) return;
            if (cAsset.server != asset.server) return;
            if (cAsset.version != asset.version) return;

            tile.Asset = asset;
            UpdateWorldAsset(tile, content);
        }

        private void OnServerTileUpdate(MakeInstanceTileObject tile, GameObject content, SimplyServer server)
        {
            var cServer = tile.Server;
            if (cServer == null) return;
            if (cServer.address != server.address) return;

            tile.Server = server;
            UpdateServer(tile, content);
        }

        private string[] GetHosts()
        {
            var config = Config.Load();
            var servers = config.Get("navigation.servers", new WorkerInfo[0]);
            return servers.Where(x => x.features.Contains("instance")).Select(x => x.address).ToArray();
        }
    }
}