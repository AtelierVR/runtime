using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Events;
using Nox.SimplyLibs;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

namespace api.nox.game
{
    internal class MakeInstanceTileManager
    {
        internal GameClientSystem clientMod;
        private GameObject tile;


        internal MakeInstanceTileManager(GameClientSystem clientMod)
        {
            this.clientMod = clientMod;
        }


        internal void SendTile(EventData context)
        {
            Debug.Log("MakeInstanceTileManager.SendTile");
            var li = context.Data[1] as object[];
            var world = (li[0] as ShareObject).Convert<SimplyWorld>();
            var asset = (li.Length > 1) ? (li[1] as ShareObject)?.Convert<SimplyWorldAsset>() : null;
            var server = (li.Length > 2) ? (li[2] as ShareObject)?.Convert<SimplyServer>() : null;
            var tile = new TileObject()
            {
                id = "api.nox.game.instance.make",
                onRemove = () => this.tile = null,
                GetContent = (Transform tf) =>
                {
                    var pf = clientMod.coreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/game.instance.make");
                    pf.SetActive(false);
                    this.tile = Object.Instantiate(pf, tf);

                    SetPasswordRequired(this.tile, false);
                    var toggle = Reference.GetReference("imptb", this.tile).GetComponent<Toggle>();
                    toggle.onValueChanged.AddListener((value) => SetPasswordRequired(this.tile, value));
                    SetCapacity(this.tile, 0);
                    var slider = Reference.GetReference("imcs", this.tile).GetComponent<Slider>();
                    slider.onValueChanged.AddListener((value) => SetCapacity(this.tile, (uint)value));
                    var expose = Reference.GetReference("imex", this.tile).GetComponent<TMPro.TMP_Dropdown>();
                    expose.value = 0;
                    UpdateContent(this.tile, world, asset, server);
                    return this.tile;
                }
            };

            clientMod.coreAPI.EventAPI.Emit("game.tile", tile);
        }

        private void UpdateContent(GameObject tile, SimplyWorld world, SimplyWorldAsset asset, SimplyServer server)
        {
            Debug.Log("MakeInstanceTileManager.UpdateContent");
            SetWorldAsset(tile, asset);
            SetServer(tile, server);
            SetWorld(tile, world);
            SetMinMaxCapacity(tile, 0, world.capacity);

            var createbutton = Reference.GetReference("imcb", this.tile).GetComponent<Button>();
            createbutton.onClick.RemoveAllListeners();
            createbutton.onClick.AddListener(() => OnCreateClicked(this.tile, world, asset, server).Forget());
        }

        private async UniTask OnCreateClicked(GameObject tile, SimplyWorld world, SimplyWorldAsset asset, SimplyServer server)
        {
            var createbutton = Reference.GetReference("imcb", this.tile).GetComponent<Button>();
            if (!createbutton.interactable) return;
            Debug.Log("MakeInstanceTileManager.OnCreateClicked");
            var password = Reference.GetReference("impi", tile).GetComponent<TMPro.TMP_InputField>().text;
            var use_password = Reference.GetReference("imptb", tile).GetComponent<Toggle>().isOn;
            var capacity = Reference.GetReference("imcs", tile).GetComponent<Slider>().value;
            var expose = Reference.GetReference("imex", tile).GetComponent<TMPro.TMP_Dropdown>();
            var instance = new SimplyCreateInstanceData()
            {
                world = world.ToMinimalString(server?.address),
                server = server?.address ?? GetHosts().FirstOrDefault(),
                password = password,
                use_password = use_password,
                capacity = capacity == 0 ? world.capacity : (capacity == world.capacity ? (ushort)0 : (ushort)capacity),
                use_whitelist = false,
                expose = expose.options[expose.value].text,
                title = world.title,
                description = world.description
            };
            createbutton.interactable = false;
            var created = await clientMod.NetworkAPI.Instance.CreateInstance(instance);
            if (created == null)
            {
                createbutton.interactable = true;
                Debug.LogError("Failed to create instance");
                return;
            }
            clientMod.GotoTile("game.instance", created);
        }

        private void SetWorldAsset(GameObject tile, SimplyWorldAsset asset)
        {
            var versiontext = Reference.GetReference("imwvv", tile).GetComponent<TextLanguage>();
            versiontext.arguments = new string[] { asset == null ? "Lastest" : asset.version.ToString() };
            versiontext.UpdateText();
        }

        private void SetServer(GameObject tile, SimplyServer server)
        {
            var servertext = Reference.GetReference("imsv", tile).GetComponent<TextLanguage>();
            servertext.arguments = new string[] { server == null ? "No Server" : server.title ?? server.address };
            servertext.UpdateText();
        }

        private void SetWorld(GameObject tile, SimplyWorld world)
        {
            var worldtext = Reference.GetReference("imwv", tile).GetComponent<TextLanguage>();
            worldtext.arguments = new string[] { world.title };
            worldtext.UpdateText();
        }

        private void SetPasswordRequired(GameObject tile, bool required)
        {
            var toggle = Reference.GetReference("imptb", tile).GetComponent<Toggle>();
            toggle.isOn = required;

            var input = Reference.GetReference("impi", tile).GetComponent<TMPro.TMP_InputField>();
            var show = Reference.GetReference("impsb", tile).GetComponent<Button>();

            input.interactable = required;
            show.interactable = required;
            show.onClick.RemoveAllListeners();

            if (!required)
                SetPasswordVisibility(tile, false);
            else show.onClick.AddListener(() => SetPasswordVisibility(tile, input.contentType == TMPro.TMP_InputField.ContentType.Password));

        }

        private void SetPasswordVisibility(GameObject tile, bool visible)
        {
            Debug.Log("MakeInstanceTileManager.SetPasswordVisibility " + visible);
            var input = Reference.GetReference("impi", tile).GetComponent<TMPro.TMP_InputField>();
            var show = Reference.GetReference("impsb", tile);

            input.contentType = visible ? TMPro.TMP_InputField.ContentType.Standard : TMPro.TMP_InputField.ContentType.Password;
            input.ForceLabelUpdate();

            var hideicon = Reference.GetReference("impsh", show);
            var showicon = Reference.GetReference("impss", show);

            hideicon.SetActive(visible);
            showicon.SetActive(!visible);
        }

        private void SetMinMaxCapacity(GameObject tile, uint min, uint max)
        {
            var slider = Reference.GetReference("imcs", tile).GetComponent<Slider>();
            if (slider.value < min) SetCapacity(tile, min);
            if (slider.value > max) SetCapacity(tile, max);
            slider.minValue = min;
            slider.maxValue = max;

            var rangetext = Reference.GetReference("imcr", tile).GetComponent<TextLanguage>();
            rangetext.arguments = new string[] { min.ToString(), max.ToString() };
            rangetext.UpdateText();
        }

        private void SetCapacity(GameObject tile, uint capacity)
        {
            var slider = Reference.GetReference("imcs", tile).GetComponent<Slider>();
            slider.value = capacity;

            var capacitytext = Reference.GetReference("imcv", tile).GetComponent<TextLanguage>();
            capacitytext.arguments = new string[] { capacity == slider.maxValue ? "Unlimited" : (capacity == slider.minValue ? "World Default" : capacity.ToString()) };
            capacitytext.UpdateText();
        }

        private string[] GetHosts()
        {
            var config = Config.Load();
            var servers = config.Get("navigation.servers", new WorkerInfo[0]);
            return servers.Where(x => x.features.Contains("instance")).Select(x => x.address).ToArray();
        }
    }
}