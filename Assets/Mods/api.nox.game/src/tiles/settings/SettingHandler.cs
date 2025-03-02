using System;
using System.Collections.Generic;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace api.nox.game.settings
{
    public abstract class SettingHandler
    {
        public string id;

        public UnityAction<TileObject, GameObject> OnSelected;
        public UnityAction<TileObject, GameObject> OnDeselected;

        public Func<SettingPage[]> GetPages;

        public virtual void Dispose()
        {
        }
    }

    public class SettingPage
    {
        public string id;
        public string title_key;
        public string text_key;
        public string description_key;
        public Texture2D icon;

        public UnityAction<TileObject, GameObject> OnSelected;
        public UnityAction<TileObject, GameObject> OnDeselected;

        public SettingGroup[] groups;
    }


    public class SettingGroup
    {
        public string id;
        public string title_key;
        public string description_key;
        public SettingEntry[] entries;
    }

    public class SettingEntry
    {
        public string id;
        public string title_key;
        public string description_key;
        public Texture2D icon;

        public virtual GameObject Make(TileObject tile, RectTransform parent) => null;
    }

    public class FPSSettingEntry : SettingEntry
    {
        public int value;
        public UnityAction<TileObject, RectTransform, GameObject, int> OnValueChanged;


        public override GameObject Make(TileObject tile, RectTransform parent)
        {
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/setting/fps.prefab");
            prefab.SetActive(false);
            var obj = Object.Instantiate(prefab, parent);
            obj.name = id;

            var capped_button = Reference.GetReference("capped_button", obj).GetComponent<Button>();
            var unlimited_button = Reference.GetReference("unlimited_button", obj).GetComponent<Button>();
            var vsync_button = Reference.GetReference("vsync_button", obj).GetComponent<Button>();

            var slider = Reference.GetReference("slider", obj).GetComponent<Slider>();
            slider.minValue = 4;
            slider.maxValue = 244;
            slider.value = value;
            slider.onValueChanged.AddListener((v) => UpdateValue(tile, parent, obj, (int)v));

            capped_button.onClick.AddListener(() => SetCappedFps(tile, parent, obj));
            unlimited_button.onClick.AddListener(() => SetUnlimitedFps(tile, parent, obj));
            vsync_button.onClick.AddListener(() => SetVSyncFps(tile, parent, obj));
            UpdateValue(tile, parent, obj, value);
            obj.SetActive(true);
            return obj;
        }

        void SetCappedFps(TileObject tile, RectTransform parent, GameObject obj)
        {
            if (IsCappedFps) return;

            var v = value;
            if (IsVSyncFps)
                v = GetVSyncTarget();
            else if (IsUlimitedFps)
                v = CurrentFps > GetVSyncTarget() ? GetVSyncTarget() : CurrentFps;

            var slider = Reference.GetReference("slider", obj).GetComponent<Slider>();
            slider.onValueChanged.AddListener((v) => UpdateValue(tile, parent, obj, (int)v));
            slider.SetValueWithoutNotify(v);

            UpdateValue(tile, parent, obj, v);
        }

        void SetUnlimitedFps(TileObject tile, RectTransform parent, GameObject obj)
        {
            if (IsUlimitedFps) return;

            UpdateValue(tile, parent, obj, -1);
        }

        void SetVSyncFps(TileObject tile, RectTransform parent, GameObject obj)
        {
            if (IsVSyncFps) return;

            UpdateValue(tile, parent, obj, 0);
        }

        int CurrentFps => (int)(1 / Time.deltaTime);

        int GetVSyncTarget()
        {
            var refreshRateRatio = Screen.currentResolution.refreshRateRatio;
            return (int)refreshRateRatio.value;
        }

        bool IsVSyncFps => value == 0;
        bool IsCappedFps => value > 0;
        bool IsUlimitedFps => value == -1;

        private void UpdateValue(TileObject tile, RectTransform parent, GameObject obj, int v)
        {
            if (value != v)
            {
                value = v;
                OnValueChanged?.Invoke(tile, parent, obj, v);
            }

            var capped = Reference.GetReference("capped", obj);
            var unlimited = Reference.GetReference("unlimited", obj);
            var vsync = Reference.GetReference("vsync", obj);
            var capped_button = Reference.GetReference("capped_button", obj).GetComponent<Button>();
            var unlimited_button = Reference.GetReference("unlimited_button", obj).GetComponent<Button>();
            var vsync_button = Reference.GetReference("vsync_button", obj).GetComponent<Button>();

            if (IsCappedFps)
            {
                capped.SetActive(true);
                unlimited.SetActive(false);
                vsync.SetActive(false);

                capped_button.interactable = false;
                unlimited_button.interactable = true;
                vsync_button.interactable = true;

                Reference.GetReference("value", obj)
                    .GetComponent<TextLanguage>()
                    .UpdateText(new[]
                    {
                        Mathf.RoundToInt(v).ToString(),
                        v.ToString("0.00"),
                        Mathf.Round(v * 100).ToString(),
                        Mathf.Round(v * 100).ToString("0.00"),
                        Convert.ToString((int)v, 2),
                        Convert.ToString((int)v, 16).ToUpper()
                    });
            }
            else if (IsUlimitedFps)
            {
                capped.SetActive(false);
                unlimited.SetActive(true);
                vsync.SetActive(false);

                capped_button.interactable = true;
                unlimited_button.interactable = false;
                vsync_button.interactable = true;
            }
            else if (IsVSyncFps)
            {
                capped.SetActive(false);
                unlimited.SetActive(false);
                vsync.SetActive(true);

                capped_button.interactable = true;
                unlimited_button.interactable = true;
                vsync_button.interactable = false;

                Reference.GetReference("max", vsync)
                    .GetComponent<TextLanguage>()
                    .UpdateText(new[] { GetVSyncTarget().ToString() });
            }
        }
    }

    public class ToggleSettingEntry : SettingEntry
    {
        public bool value;
        public UnityAction<TileObject, RectTransform, GameObject, bool> OnValueChanged;

        public override GameObject Make(TileObject tile, RectTransform parent)
        {
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/setting/toggle.prefab");
            prefab.SetActive(false);
            var obj = Object.Instantiate(prefab, parent);
            obj.name = id;

            Reference.GetReference("title", obj).GetComponent<TextLanguage>().UpdateText(title_key);
            var toggle = Reference.GetReference("toggle", obj).GetComponent<Toggle>();
            toggle.isOn = value;
            toggle.onValueChanged.AddListener((v) => UpdateValue(tile, parent, obj, v));
            UpdateValue(tile, parent, obj, value);

            obj.SetActive(true);
            return obj;
        }

        private void UpdateValue(TileObject tile, RectTransform parent, GameObject obj, bool v)
        {
            if (value == v)
                return;
            value = v;
            OnValueChanged?.Invoke(tile, parent, obj, v);
        }
    }

    public class SelectSettingEntry : SettingEntry
    {
        public int value;
        public UnityAction<TileObject, RectTransform, GameObject, int> OnValueChanged;
        public string[] options_text;
        public string[] option_keys;

        public override GameObject Make(TileObject tile, RectTransform parent)
        {
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/setting/dropdown.prefab");
            prefab.SetActive(false);
            var obj = Object.Instantiate(prefab, parent);
            obj.name = id;

            Reference.GetReference("title", obj).GetComponent<TextLanguage>().UpdateText(title_key);

            var dropdownGameobject = Reference.GetReference("dropdown", obj);

            if (dropdownGameobject.TryGetComponent<Dropdown>(out var dropdown))
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(new List<string>(options_text));
                dropdown.value = value;
                dropdown.onValueChanged.AddListener((v) => UpdateValue(tile, parent, obj, v));
            }
            else if (dropdownGameobject.TryGetComponent<TMPro.TMP_Dropdown>(out var tmpDropdown))
            {
                tmpDropdown.ClearOptions();
                tmpDropdown.AddOptions(new List<string>(options_text));
                tmpDropdown.value = value;
                tmpDropdown.onValueChanged.AddListener((v) => UpdateValue(tile, parent, obj, v));
            }

            obj.SetActive(true);
            return obj;
        }

        private void UpdateValue(TileObject tile, RectTransform parent, GameObject obj, int v)
        {
            if (value == v)
                return;
            value = v;
            OnValueChanged?.Invoke(tile, parent, obj, v);
        }
    }

    public class BarSettingEntry : SettingEntry
    {
    }

    public class RangeSettingEntry : SettingEntry
    {
        public float value;
        public float min;
        public float max;
        public float step;
        public string value_key;

        public UnityAction<TileObject, RectTransform, GameObject, float> OnValueChanged;

        public override GameObject Make(TileObject tile, RectTransform parent)
        {
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/setting/slider.prefab");
            prefab.SetActive(false);
            var obj = Object.Instantiate(prefab, parent);
            obj.name = id;

            Reference.GetReference("title", obj).GetComponent<TextLanguage>().UpdateText(title_key);
            var slider = Reference.GetReference("slider", obj).GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener((v) => UpdateValue(tile, parent, obj, v));
            UpdateValue(tile, parent, obj, value);

            obj.SetActive(true);
            return obj;
        }

        private void UpdateValue(TileObject tile, RectTransform parent, GameObject obj, float v)
        {
            if (step > 0)
                v = Mathf.Round(v / step) * step;
            v = Mathf.Clamp(v, min, max);


            Reference.GetReference("value", obj).GetComponent<TextLanguage>().UpdateText(
                value_key ?? "setting.range.value",
                new string[]
                {
                    Mathf.RoundToInt(v).ToString(),
                    v.ToString("0.00"),
                    Mathf.Round(v * 100).ToString(),
                    Mathf.Round(v * 100).ToString("0.00"),
                    Convert.ToString((int)v, 2),
                    Convert.ToString((int)v, 16).ToUpper()
                });

            if (value != v)
            {
                value = v;
                OnValueChanged?.Invoke(tile, parent, obj, v);
            }
        }
    }

    public class VolumeSettingEntry : SettingEntry
    {
        public float value;
        public UnityAction<TileObject, RectTransform, GameObject, float> OnValueChanged;

        public bool muted;
        public UnityAction<TileObject, RectTransform, GameObject, bool> OnMutedChanged;

        public override GameObject Make(TileObject tile, RectTransform parent)
        {
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/setting/volume.prefab");
            prefab.SetActive(false);
            var obj = Object.Instantiate(prefab, parent);
            obj.name = id;

            Reference.GetReference("title", obj).GetComponent<TextLanguage>().UpdateText(title_key);

            var slider = Reference.GetReference("slider", obj).GetComponent<Slider>();
            slider.value = value;
            slider.minValue = 0f;
            slider.maxValue = 2f;
            slider.onValueChanged.AddListener((v) => UpdateValue(tile, parent, obj, v));

            var toggle = Reference.GetReference("toggle", obj).GetComponent<Toggle>();
            toggle.isOn = muted;
            toggle.onValueChanged.AddListener((v) => UpdateMuted(tile, parent, obj, v));

            UpdateValue(tile, parent, obj, value);
            UpdateMuted(tile, parent, obj, muted);

            obj.SetActive(true);
            return obj;
        }

        private void UpdateValue(TileObject tile, RectTransform parent, GameObject obj, float v)
        {
            Reference.GetReference("value", obj).GetComponent<TextLanguage>()
                .UpdateText(new string[] { Mathf.Round(v * 100).ToString() });

            if (value != v)
            {
                value = v;
                OnValueChanged?.Invoke(tile, parent, obj, v);
            }
        }

        private void UpdateMuted(TileObject tile, RectTransform parent, GameObject obj, bool v)
        {
            if (muted == v)
                return;
            muted = v;
            OnMutedChanged?.Invoke(tile, parent, obj, v);
        }
    }

    public class LanguageSettingEntry : SettingEntry
    {
        public string value;
        public UnityAction<TileObject, RectTransform, GameObject, string> OnValueChanged;
        public string[] option_texts;
        public string[] option_keys;

        public override GameObject Make(TileObject tile, RectTransform parent)
        {
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/setting/dropdown.prefab");
            prefab.SetActive(false);
            var obj = Object.Instantiate(prefab, parent);
            obj.name = id;

            Reference.GetReference("title", obj).GetComponent<TextLanguage>().UpdateText(title_key);

            var o = Reference.GetReference("dropdown", obj);

            if (o.TryGetComponent<Dropdown>(out var dropdown))
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(new List<string>(option_texts));
                dropdown.value = Array.IndexOf(option_keys, value);
                dropdown.onValueChanged.AddListener((v) => UpdateValue(tile, parent, obj, v));
            }
            else if (o.TryGetComponent<TMPro.TMP_Dropdown>(out var tmpDropdown))
            {
                tmpDropdown.ClearOptions();
                tmpDropdown.AddOptions(new List<string>(option_texts));
                tmpDropdown.value = Array.IndexOf(option_keys, value);
                tmpDropdown.onValueChanged.AddListener((v) => UpdateValue(tile, parent, obj, v));
            }

            obj.SetActive(true);
            return obj;
        }

        private void UpdateValue(TileObject tile, RectTransform parent, GameObject obj, int v)
        {
            if (value == option_keys[v]) return;
            value = option_keys[v];
            OnValueChanged?.Invoke(tile, parent, obj, value);
        }
    }
}