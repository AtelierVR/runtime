using System;
using System.Collections.Generic;
using Nox.CCK;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace api.nox.game.Settings
{

    public abstract class SettingHandler
    {
        public string id;

        public UnityAction<TileObject, GameObject> OnSelected;
        public UnityAction<TileObject, GameObject> OnDeselected;

        public Func<SettingPage[]> GetPages;
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

    public class ToggleSettingEntry : SettingEntry
    {
        public bool value;
        public UnityAction<TileObject, RectTransform, GameObject, bool> OnValueChanged;
        public override GameObject Make(TileObject tile, RectTransform parent)
        {
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/setting/toggle");
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
        public string[] options;
        public override GameObject Make(TileObject tile, RectTransform parent)
        {
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/setting/dropdown");
            prefab.SetActive(false);
            var obj = Object.Instantiate(prefab, parent);
            obj.name = id;

            Reference.GetReference("title", obj).GetComponent<TextLanguage>().UpdateText(title_key);

            var dropdown_gameobject = Reference.GetReference("dropdown", obj);

            if (dropdown_gameobject.TryGetComponent<Dropdown>(out var dropdown))
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(new List<string>(options));
                dropdown.value = value;
                dropdown.onValueChanged.AddListener((v) => UpdateValue(tile, parent, obj, v));
            }
            else if (dropdown_gameobject.TryGetComponent<TMPro.TMP_Dropdown>(out var tmp_dropdown))
            {
                tmp_dropdown.ClearOptions();
                tmp_dropdown.AddOptions(new List<string>(options));
                tmp_dropdown.value = value;
                tmp_dropdown.onValueChanged.AddListener((v) => UpdateValue(tile, parent, obj, v));
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
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/setting/slider");
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
                new string[] {
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
            var prefab = GameClientSystem.CoreAPI.AssetAPI.GetLocalAsset<GameObject>("prefabs/setting/volume");
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


}