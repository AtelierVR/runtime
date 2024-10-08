using System;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Initializers;
using Nox.Mods.Type;

namespace Nox.Mods.Client
{
    public class RuntimeMod : Mod
    {
        private ModMetadata _metadata;
        private ModInitializer[] _mainClasses = new ModInitializer[0];
        private bool _mainIsEnable = false;
        private ClientModInitializer[] _clientInitializers = new ClientModInitializer[0];
        private bool _clientIsEnable = false;
        private InstanceModInitializer[] _instanceInitializers = new InstanceModInitializer[0];
        private bool _instanceIsEnable = false;
        private ModType _type;
        internal ClientModCoreAPI coreClientAPI;
        internal RuntimeModCoreAPI coreAPI;
        internal InstanceModCoreAPI instanceAPI;


        internal RuntimeMod(ModMetadata metadata, ModType type)
        {
            _metadata = metadata;
            _type = type;
            coreClientAPI = new ClientModCoreAPI(this);
            coreAPI = new RuntimeModCoreAPI(this);
        }

        public ModType GetModType() => _type;
        internal ModMetadata GetInternalMetadata() => _metadata;
        public CCK.Mods.ModMetadata GetMetadata() => _metadata;
        public ModInitializer[] GetMainClasses() => _mainClasses;
        public ClientModInitializer[] GetClientClasses() => _clientInitializers;
        public InstanceModInitializer[] GetInstanceClasses() => _instanceInitializers;

        internal void SetMainClasses(ModInitializer[] mainClasses)
        {
            if (IsMainEnabled()) return;
            _mainClasses = mainClasses;
        }
        internal void SetClientClasses(ClientModInitializer[] clientClasses)
        {
            if (IsClientEnabled()) return;
            _clientInitializers = clientClasses;
        }
        internal void SetInstanceClasses(InstanceModInitializer[] instanceClasses)
        {
            if (IsInstanceEnabled()) return;
            _instanceInitializers = instanceClasses;
        }

        public void EnableMain()
        {
            if (_mainIsEnable) return;
            foreach (var main in _mainClasses)
                main.OnInitialize(coreAPI);
            _mainIsEnable = true;
        }

        public void EnableClient()
        {
            if (_clientIsEnable) return;
            foreach (var client in _clientInitializers)
            {
                client.OnInitialize(coreAPI);
                client.OnInitializeClient(coreClientAPI);
            }
            _clientIsEnable = true;
        }

        public void EnableInstance()
        {
            if (_instanceIsEnable) return;
            foreach (var instance in _instanceInitializers)
            {
                instance.OnInitialize(coreAPI);
                instance.OnInitializeInstance(instanceAPI);
            }
            _instanceIsEnable = true;
        }

        public bool IsLoaded() => _mainClasses.Length + _clientInitializers.Length + _instanceInitializers.Length > 0;

        public bool IsMainEnabled() => _mainIsEnable;
        public bool IsClientEnabled() => _clientIsEnable;
        public bool IsInstanceEnabled() => _instanceIsEnable;

        public void DisableMain()
        {
            if (!_mainIsEnable) return;
            foreach (var main in _mainClasses)
                main.OnDispose();
            _mainIsEnable = false;
        }

        public void DisableClient()
        {
            if (!_clientIsEnable) return;
            foreach (var client in _clientInitializers)
                client.OnDispose();
            _clientIsEnable = false;
        }

        public void DisableInstance()
        {
            if (!_instanceIsEnable) return;
            foreach (var instance in _instanceInitializers)
                instance.OnDispose();
            _instanceIsEnable = false;
        }

        public void Unload()
        {
            if (IsMainEnabled()) DisableMain();
            if (IsClientEnabled()) DisableClient();
            if (IsInstanceEnabled()) DisableInstance();
        }

        public void Load()
        {
            if (!IsMainEnabled()) EnableMain();
            if (!IsClientEnabled()) EnableClient();
            if (!IsInstanceEnabled()) EnableInstance();
        }

        public void Reload()
        {
            Unload();
            Load();
        }

        public bool IsEnabled() => IsMainEnabled() || IsClientEnabled() || IsInstanceEnabled();

        public void Destroy()
        {
            Unload();
            _mainClasses = new ModInitializer[0];
            _clientInitializers = new ClientModInitializer[0];
            _instanceInitializers = new InstanceModInitializer[0];
            GetModType().Destroy();
        }

        internal void PostMain()
        {
            foreach (var main in _mainClasses)
                main.OnPostInitialize();
        }

        internal void PostClient()
        {
            foreach (var client in _clientInitializers)
                client.OnPostInitializeClient();
        }

        internal void PostInstance()
        {
            foreach (var instance in _instanceInitializers)
                instance.OnPostInitializeInstance();
        }
    }
}