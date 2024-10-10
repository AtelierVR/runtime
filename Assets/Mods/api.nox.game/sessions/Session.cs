using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK;
using Nox.CCK.Mods;
using Nox.CCK.Worlds;
using Nox.SimplyLibs;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace api.nox.game.sessions
{
    public class Session : ShareObject, IDisposable
    {
        [ShareObjectExport, ShareObjectImport] public byte uid;
        [ShareObjectExport, ShareObjectImport] public uint id;
        [ShareObjectExport, ShareObjectImport] public string group;

        private SessionController _controller;
        public SessionController controller
        {
            get => _controller;
            set
            {
                _controller = value;
                if (_controller != null)
                    _controller.session = this;
            }
        }

        public List<Scene> scenes = new();

        public SimplyWorld world;
        public SimplyWorldAsset worldAsset;

        public BaseDescriptor GetDescriptor(byte scene_index) => GetDescriptor(scenes[scene_index]);
        public BaseDescriptor GetDescriptor(Scene scene) => Finder.FindComponent<BaseDescriptor>(scene);
        public List<BaseDescriptor> GetDescriptors()
        {
            List<BaseDescriptor> descriptors = new();
            foreach (var scene in scenes)
                descriptors.Add(GetDescriptor(scene));
            return descriptors;
        }
        public byte IndexOfMainDescriptor(out MainDescriptor descriptor)
        {
            Debug.Log($"Finding main descriptor in {scenes.Count} scenes");
            for (byte i = 0; i < scenes.Count; i++)
            {
                descriptor = Finder.FindComponent<MainDescriptor>(scenes[i]);
                if (descriptor != null)
                    return i;
            }
            descriptor = null;
            return byte.MaxValue;
        }

        public void Dispose()
        {
            controller.Dispose();
            controller = null;
            scenes.Clear();
        }

        public void SetCurrent() => GameSystem.Instance.SessionManager.CurrentSession = this;

        public void OnSelectedCurrent(Session old)
        {
            Debug.Log("Selected session " + id + (old == null ? "" : " but session " + old.id + " was deselected"));
            for (byte i = 0; i < scenes.Count; i++)
            {
                var scene = scenes[i];
                var wh = WorldHidden.GetWorldHidden(scene);
                if (wh != null) wh.SetHidden(false);
            }
        }

        public void OnDeselectedCurrent(Session current)
        {
            Debug.Log("Deselected session " + id + (current == null ? "" : " but session " + current.id + " was selected"));
            for (byte i = 0; i < scenes.Count; i++)
            {
                var scene = scenes[i];
                var wh = WorldHidden.GetWorldHidden(scene);
                if (wh != null) wh.SetHidden(true);
            }
        }
    }
}