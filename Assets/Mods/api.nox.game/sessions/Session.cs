using System;
using System.Collections.Generic;
using api.nox.game.Worlds;
using api.nox.network.Worlds;
using api.nox.network.Worlds.Assets;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Worlds;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Nox.CCK.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace api.nox.game.sessions
{
    public class Session
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

        public WorldLock worldLock = null;

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

        public BaseDescriptor GetDescriptor(byte scene_index)
            => worldLock != null ? GetDescriptor(worldLock.scenes[scene_index].scene) : null;
        public BaseDescriptor GetDescriptor(Scene scene)
            => Finder.FindComponent<BaseDescriptor>(scene);

        public List<BaseDescriptor> GetDescriptors()
        {
            if (worldLock == null)
                return new();
            List<BaseDescriptor> descriptors = new();
            foreach (var scene in worldLock.scenes)
                descriptors.Add(GetDescriptor(scene.scene));
            return descriptors;
        }

        public byte IndexOfMainDescriptor(out MainDescriptor descriptor)
        {
            if (worldLock == null)
            {
                descriptor = null;
                return byte.MaxValue;
            }

            Logger.Log($"Finding main descriptor in {worldLock.scenes.Count} scenes");
            for (byte i = 0; i < worldLock.scenes.Count; i++)
            {
                descriptor = Finder.FindComponent<MainDescriptor>(worldLock.scenes[i].scene);
                if (descriptor != null)
                    return i;
            }
            descriptor = null;
            return byte.MaxValue;
        }

        public async UniTask Close()
        {
            // Unregister all players
            var abps = new List<IAbstractPlayer>(abstractPlayers);
            foreach (var player in abps)
                player.Unregister();
            abstractPlayers.Clear();

            // disconnect controller
            await Controller.Close();

            // check if is the current session, if so, deselect it
            await SessionManager.Instance.Remove(this);

            // Unload all scenes
            while (worldLock.scenes.Count > 0)
            {
                var scene = worldLock.scenes[0];
                worldLock.scenes.RemoveAt(0);
                if (scene.scene.isLoaded)
                    await WorldManager.UnloadScene(worldLock.hash, scene.index);
            }

            // Destroy other objects
            Controller = null;
            world = null;
            worldAsset = null;
            WorldManager.LockWorlds.Remove(worldLock);
            worldLock = null;

        }


        public async UniTask SetCurrent()
            => await SessionManager.Instance.SetSession(this);

        public void OnSelectedCurrent(Session old)
        {
            Logger.Log("Selected session " + id + (old == null ? "" : " but session " + old.id + " was deselected"));

            var descriptors = GetDescriptors();
            foreach (var descriptor in descriptors)
                if (descriptor.TryGetCustom("world_hidden", out var hidden) && hidden.Length > 0)
                    WorldHidden.Make(descriptor.gameObject.scene).Set(hidden[0] == 0);

            if (worldLock.scenes.Count > 0)
                USceneManager.SetActiveScene(worldLock.scenes[0].scene);
        }

        public void OnDeselectedCurrent(Session current)
        {
            Logger.Log("Deselected session " + id + (current == null ? "" : " but session " + current.id + " was selected"));

            var descriptors = GetDescriptors();
            foreach (var descriptor in descriptors)
            {
                var wh = WorldHidden.Make(descriptor.gameObject.scene);
                descriptor.SetCustom("world_hidden", new byte[] { wh.IsHidden() ? (byte)1 : (byte)0 });
                wh.Set(false);
            }
        }

        public void RegisterPlayer(IAbstractPlayer player)
        {
            player.SetSession(this);
            abstractPlayers.Add(player);
            Logger.Log("Player registered");
        }

        public void UnregisterPlayer(IAbstractPlayer player)
        {
            if (!abstractPlayers.Contains(player))
                return;
            abstractPlayers.Remove(player);
            Logger.Log("Player unregistered");
        }



    }
}