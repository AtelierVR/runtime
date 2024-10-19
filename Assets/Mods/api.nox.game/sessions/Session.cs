using System;
using System.Collections.Generic;
using api.nox.network.Worlds;
using api.nox.network.Worlds.Assets;
using Nox.CCK;
using Nox.CCK.Mods;
using Nox.CCK.Worlds;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace api.nox.game.sessions
{
    public class Session : IDisposable
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

        public World world;
        public WorldAsset worldAsset;

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

        // public void SpawnPlayer<T>(T player) where T : Player {
        //     player.OnPreSpawn(this);
        //     players.Add(player);
        //     player.OnSpawn(this);
        // }

        // public void DestroyPlayer<T>(T player) where T : Player
        // {
        //     player.OnPreDestroy(this);
        //     players.Remove(player);
        //     player.OnDestroy(this);
        //     player.Dispose();
        // }

    }
}