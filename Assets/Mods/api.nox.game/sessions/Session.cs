using System;
using System.Collections.Generic;
using api.nox.network.Worlds;
using api.nox.network.Worlds.Assets;
using Nox.CCK;
using Nox.CCK.Worlds;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace api.nox.game.sessions
{
    public class Session : IDisposable
    {
        public byte uid;
        public uint id;
        public string group;

        private ISessionController _controller;
        public ISessionController Controller
        {
            get => _controller;
            set
            {
                _controller = value;
                if (_controller != null)
                    _controller.SetSession(this);
            }
        }

        public List<Scene> scenes = new();

        public List<IAbstractPlayer> abstractPlayers = new();

        public IAbstractPlayer GetAbstractPlayer(ushort id)
        {
            foreach (var player in abstractPlayers)
                if (player.GetId() == id)
                    return player;
            return null;
        }

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
            Controller.Dispose();
            Controller = null;
            scenes.Clear();
            abstractPlayers.Clear();
            world = null;
            worldAsset = null;
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

        public void RegisterPlayer(IAbstractPlayer player)
        {
            player.SetSession(this);
            abstractPlayers.Add(player);
            Debug.Log("Player registered");
        }

        public void UnregisterPlayer(IAbstractPlayer player)
        {
            if (!abstractPlayers.Contains(player))
                return;
            abstractPlayers.Remove(player);
            Debug.Log("Player unregistered");
        }



    }
}