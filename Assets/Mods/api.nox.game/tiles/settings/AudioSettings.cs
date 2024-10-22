using Newtonsoft.Json.Linq;
using Nox.CCK;
using UnityEngine;


namespace api.nox.game.Settings
{
    public class AudioSettings : SettingHandler
    {
        public AudioGroup Volume;

        public bool HasAudioGroup(string id) => GetAudioGroup(id) != null;

        public AudioGroup GetAudioGroup(string id)
        {
            if (Volume.Id == id)
                return Volume;
            return GetAudioGroup(Volume, id);
        }

        private AudioGroup GetAudioGroup(AudioGroup group, string id)
        {
            if (group.Id == id)
                return group;
            if (group.SubVolumes != null)
                foreach (var sub in group.SubVolumes)
                {
                    var result = GetAudioGroup(sub, id);
                    if (result != null)
                        return result;
                }
            return null;
        }

        public void SetVolume(string id, float value)
        {
            if (value < 0)
                value = 0;
            if (value > 1)
                value = 1;
            var group = GetAudioGroup(id);
            if (group != null)
                group.Value = value;
            GameClientSystem.CoreAPI.EventAPI.Emit("audio.volume.changed", id, value);
        }

        public void SetMuted(string id, bool value)
        {
            var group = GetAudioGroup(id);
            if (group != null)
                group.IsMuted = value;
            GameClientSystem.CoreAPI.EventAPI.Emit("audio.muted.changed", id, value);
        }

        internal AudioSettings()
        {
            id = "game.audio";
            text_key = "settings.audio";
            title_key = "settings.audio";
            GetPages = GetInternalPages;

            Volume = new AudioGroup
            {
                Id = "master",
                Name_Key = "audio.master",
                Value = 1,
                IsMuted = false,
                SubVolumes = new[]
                {
                    new AudioGroup
                    {
                        Id = "music",
                        Name_Key = "audio.music",
                        Value = 1,
                        IsMuted = false
                    },
                    new AudioGroup
                    {
                        Id = "sfx",
                        Name_Key = "audio.sfx",
                        Value = 1,
                        IsMuted = false
                    },
                    new AudioGroup
                    {
                        Id = "voice",
                        Name_Key = "audio.voice",
                        Value = 1,
                        IsMuted = false
                    }
                }
            };
        }


        public void LoadFromConfig()
        {
            var config = Config.Load();

            var audio = config.Get("settings.audio")?.ToObject<JObject>();
            if (audio == null)
                return;

            LoadFromConfig(Volume, audio);
        }

        private void LoadFromConfig(AudioGroup group, JObject json)
        {
            if (json == null)
                return;
            if (json.ContainsKey("value"))
                group.Value = json["value"]?.Value<float>() ?? 1;
            if (json.ContainsKey("muted"))
                group.IsMuted = json["muted"]?.Value<bool>() ?? false;
            if (group.SubVolumes != null && json.ContainsKey("channels"))
            {
                var channels = json["channels"].ToObject<JObject>();
                foreach (var sub in group.SubVolumes)
                    if (channels.ContainsKey(sub.Id))
                        LoadFromConfig(sub, channels[sub.Id]?.ToObject<JObject>());
            }
        }

        public void SaveToConfig()
        {
            var config = Config.Load();

            var audio = new JObject();
            SaveToConfig(Volume, audio);
            config.Set("settings.audio", audio);
            config.Save();
        }

        private void SaveToConfig(AudioGroup group, JObject json)
        {
            json["value"] = group.Value;
            json["muted"] = group.IsMuted;
            if (group.SubVolumes != null)
            {
                var channels = new JObject();
                foreach (var sub in group.SubVolumes)
                {
                    var subJson = new JObject();
                    SaveToConfig(sub, subJson);
                    channels[sub.Id] = subJson;
                }
                json["channels"] = channels;
            }
        }

        private SettingPage[] GetInternalPages()
        {
            return new SettingPage[0];
        }

        internal void UpdateHandler()
        {
            Debug.Log("AudioSettings.UpdateHandler");
            GameClientSystem.CoreAPI.EventAPI.Emit("game.setting", this);
        }

        public void OnDispose()
        {
            GetPages = null;
            UpdateHandler();
        }

    }

    public class AudioGroup
    {
        public string Id;
        public string Name_Key;
        public float Value;
        public bool IsMuted;
        public AudioGroup[] SubVolumes;
    }
}