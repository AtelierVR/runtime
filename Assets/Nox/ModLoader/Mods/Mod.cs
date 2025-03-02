using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Metadata;
using Nox.CCK.Utils;
using Nox.ModLoader.Cores.Assets;

namespace Nox.ModLoader.Mods
{
    public class Mod : CCK.Mods.Mod
    {
        internal Mod()
        {
            CoreAPI = new CoreAPI(this);
        }


        public T GetData<T>(string key, T defaultValue = default)
            => HasData<T>(key) ? (T)GetDatas()[key] : defaultValue;

        public bool SetData<T>(string key, T value)
        {
            GetDatas()[key] = value;
            return true;
        }

        public bool HasData<T>(string key)
            => GetDatas().ContainsKey(key);

        public Dictionary<string, object> GetDatas()
            => Metadata.InternalData;

        internal CoreAPI CoreAPI;
        public AssetAPI AssetAPI;

        // implementation of main class components
        internal List<MainModInitializer> MainInitializers = new();
        private InitializerState _mainState = InitializerState.None;

        private bool mainEnabled;
        public bool IsMainEnabled() => mainEnabled;
        public MainModInitializer[] GetMains() => MainInitializers.ToArray();

        public void EnableMain()
        {
            if (IsMainEnabled()) return;
            Logger.LogDebug($"Enabling main in {Metadata.GetId()}({Metadata.GetVersion()})");
            MainInitializers = CreateInstances<MainModInitializer>("main").ToList();
            mainEnabled = true;
        }

        public void DisableMain()
        {
            if (!IsMainEnabled()) return;
            Logger.LogDebug($"Disabling main in {Metadata.GetId()}({Metadata.GetVersion()})");
            mainEnabled = false;
        }

        public void ClearMain()
        {
            if (!IsMainEnabled()) return;
            Logger.LogDebug($"Clearing main in {Metadata.GetId()}({Metadata.GetVersion()})");
            foreach (var main in MainInitializers.ToArray())
                MainInitializers.Remove(main);
        }

        // implementation of editor class components
        internal List<EditorModInitializer> EditorInitializers = new();
        private InitializerState _editorState = InitializerState.None;


        private bool editorEnabled;
        public bool IsEditorEnabled() => editorEnabled;
        public EditorModInitializer[] GetEditors() => EditorInitializers.ToArray();

        public void EnableEditor()
        {
            if (IsEditorEnabled()) return;
            Logger.LogDebug($"Enabling editor in {Metadata.GetId()}({Metadata.GetVersion()})");
            var instances = CreateInstances<EditorModInitializer>("editor");
            EditorInitializers = instances.ToList();
            editorEnabled = true;
        }

        public void DisableEditor()
        {
            if (!IsEditorEnabled()) return;
            Logger.LogDebug($"Disabling editor in {Metadata.GetId()}({Metadata.GetVersion()})");
            editorEnabled = false;
        }

        public void ClearEditor()
        {
            if (!IsEditorEnabled()) return;
            Logger.LogDebug($"Clearing editor in {Metadata.GetId()}({Metadata.GetVersion()})");
            foreach (var editor in EditorInitializers.ToArray())
                EditorInitializers.Remove(editor);
        }

        // implementation of server class components
        internal List<ServerModInitializer> ServerInitializers = new();
        private InitializerState _serverState = InitializerState.None;

        private bool serverEnabled;
        public bool IsServerEnabled() => serverEnabled;
        public ServerModInitializer[] GetServers() => ServerInitializers.ToArray();

        public void EnableServer()
        {
            if (IsServerEnabled()) return;
            Logger.LogDebug($"Enabling server in {Metadata.GetId()}({Metadata.GetVersion()})");
            ServerInitializers = CreateInstances<ServerModInitializer>("server").ToList();
            serverEnabled = true;
        }

        public void DisableServer()
        {
            if (!IsServerEnabled()) return;
            Logger.LogDebug($"Disabling server in {Metadata.GetId()}({Metadata.GetVersion()})");
            serverEnabled = false;
        }

        public void ClearServer()
        {
            if (!IsServerEnabled()) return;
            Logger.LogDebug($"Clearing server in {Metadata.GetId()}({Metadata.GetVersion()})");
            foreach (var server in ServerInitializers.ToArray())
                ServerInitializers.Remove(server);
        }

        // implementation of client class components
        internal List<ClientModInitializer> ClientInitializers = new();
        private InitializerState _clientState = InitializerState.None;

        public ClientModInitializer[] GetClients() => ClientInitializers.ToArray();

        private bool clientEnabled;
        public bool IsClientEnabled() => clientEnabled;

        public void EnableClient()
        {
            if (IsClientEnabled()) return;
            Logger.LogDebug($"Enabling client in {Metadata.GetId()}({Metadata.GetVersion()})");
            ClientInitializers = CreateInstances<ClientModInitializer>("client").ToList();
            clientEnabled = true;
        }

        public void DisableClient()
        {
            if (!IsClientEnabled()) return;
            Logger.LogDebug($"Disabling client in {Metadata.GetId()}({Metadata.GetVersion()})");
            clientEnabled = false;
        }

        public void ClearClient()
        {
            if (!IsClientEnabled()) return;
            Logger.LogDebug($"Clearing client in {Metadata.GetId()}({Metadata.GetVersion()})");
            foreach (var client in ClientInitializers.ToArray())
                ClientInitializers.Remove(client);
        }


        // implementation of instance class components
        internal Dictionary<uint, List<InstanceModInitializer>> InstanceInitializers = new();
        private Dictionary<uint, InitializerState> _instanceStates = new();

        private InitializerState GetInstanceState(uint id) =>
            _instanceStates.ContainsKey(id) ? _instanceStates[id] : InitializerState.None;

        private void SetInstanceStates(uint id, InitializerState state) => _instanceStates[id] = state;

        private Dictionary<uint, bool> instanceEnabled = new();
        public bool IsInstanceEnabled(uint id) => instanceEnabled.ContainsKey(id) && instanceEnabled[id];

        public InstanceModInitializer[] GetInstances(uint id)
            => InstanceInitializers.ContainsKey(id)
                ? InstanceInitializers[id].ToArray()
                : new InstanceModInitializer[0];

        public void EnableInstance(uint id)
        {
            if (IsInstanceEnabled(id)) return;
            Logger.LogDebug($"Enabling instance {id} in {Metadata.GetId()}({Metadata.GetVersion()})");
            InstanceInitializers[id] = CreateInstances<InstanceModInitializer>("instance").ToList();
            instanceEnabled[id] = true;
        }

        public void DisableInstance(uint id)
        {
            if (!IsInstanceEnabled(id)) return;
            Logger.LogDebug($"Disabling instance {id} in {Metadata.GetId()}({Metadata.GetVersion()})");
            instanceEnabled.Remove(id);
        }

        public void ClearInstance(uint id)
        {
            if (!IsInstanceEnabled(id)) return;
            Logger.LogDebug($"Clearing instance {id} in {Metadata.GetId()}({Metadata.GetVersion()})");
            foreach (var instance in InstanceInitializers[id].ToArray())
                InstanceInitializers[id].Remove(instance);
        }

        // implementation of custom class components
        internal Dictionary<string, IModInitializer[]> CustomInitializers = new();
        private Dictionary<string, InitializerState> _customStates = new();

        private InitializerState GetCustomState(string entry) =>
            _customStates.ContainsKey(entry) ? _customStates[entry] : InitializerState.None;

        private void SetCustomStates(string entry, InitializerState state) => _customStates[entry] = state;

        private Dictionary<string, bool> customEnabled = new();

        public bool IsCustomEnabled(string entry)
            => customEnabled.ContainsKey(entry) && customEnabled[entry];

        public IModInitializer[] GetCustom(string entry)
            => CustomInitializers.ContainsKey(entry)
                ? CustomInitializers[entry]
                : Array.Empty<IModInitializer>();

        public string[] GetCustomEntries() => CustomInitializers.Keys.ToArray();

        public T[] GetCustom<T>(string entry) where T : IModInitializer
            => CustomInitializers.ContainsKey(entry)
                ? CustomInitializers[entry].Where(i => i.GetType() == typeof(T)).Cast<T>().ToArray()
                : Array.Empty<T>();

        public void DisableCustom(string entry)
        {
            if (!IsCustomEnabled(entry)) return;
            Logger.LogDebug($"Disabling {entry} in {Metadata.GetId()}({Metadata.GetVersion()})");
            customEnabled[entry] = false;
        }

        public void EnableCustom<T>(string entry) where T : IModInitializer
        {
            if (IsCustomEnabled(entry)) return;
            Logger.LogDebug($"Enabling {entry} in {Metadata.GetId()}({Metadata.GetVersion()})");
            CustomInitializers[entry] = CreateInstances<T>(entry).Cast<IModInitializer>().ToArray();
            customEnabled[entry] = true;
        }

        public void ClearCustom(string entry)
        {
            if (!IsCustomEnabled(entry)) return;
            Logger.LogDebug($"Clearing {entry} in {Metadata.GetId()}({Metadata.GetVersion()})");
            foreach (var instance in CustomInitializers[entry].ToArray())
            {
                var list = CustomInitializers[entry].ToList();
                list.Remove(instance);
                CustomInitializers[entry] = list.ToArray();
            }
        }


        internal Typing.ModMetadata Metadata;

        public ModMetadata GetMetadata() => Metadata;

        public virtual bool IsLoaded() => true;

        public virtual UniTask<bool> Load()
        {
            Logger.LogDebug($"Loading {Metadata.GetId()}({Metadata.GetVersion()})");
            return UniTask.FromResult(true);
        }

        public virtual async UniTask<bool> Unload()
        {
            Logger.LogDebug($"Unloading {Metadata.GetId()}({Metadata.GetVersion()})");

            // if not disabled, disable
            DisableMain();
            DisableEditor();
            DisableServer();
            DisableClient();
            foreach (var entry in InstanceInitializers.Keys)
                DisableInstance(entry);
            foreach (var entry in CustomInitializers.Keys)
                DisableCustom(entry);

            // send pre-dispose and dispose
            await SendPreDispose();
            await SendDispose();

            ClearMain();
            ClearEditor();
            ClearServer();
            ClearClient();
            foreach (var entry in InstanceInitializers.Keys)
                ClearInstance(entry);
            foreach (var entry in CustomInitializers.Keys)
                ClearCustom(entry);

            return true;
        }

        public virtual AppDomain GetAppDomain() => AppDomain.CurrentDomain;
        public virtual Assembly[] GetAssemblies() => GetAppDomain().GetAssemblies();

        public virtual Type[] GetEntryClasses(string entry)
        {
            var entries = GetMetadata().GetEntryPoints();
            if (!entries.Has(entry)) return Type.EmptyTypes;
            var namespaces = entries.Get(entry);

            List<Type> ModClasses = new();
            var t = typeof(IModInitializer);

            foreach (var assembly in GetAssemblies())
            foreach (var type in assembly.GetTypes())
            foreach (var ns in namespaces)
                if (type.FullName == ns && type.GetInterface(t.FullName) != null)
                    ModClasses.Add(type);

            return ModClasses.ToArray();
        }

        public virtual T[] CreateInstances<T>(string entry) where T : IModInitializer
        {
            var types = GetEntryClasses(entry);
            var instances = new Dictionary<string, T>();

            foreach (var type in types)
            {
                Logger.LogDebug(
                    $"Creating instance of [{entry.ToUpper()}]:{type.FullName} in {Metadata.GetId()}({Metadata.GetVersion()})");
                var instance = (T)Activator.CreateInstance(type);
                if (type.FullName != null) instances.Add(type.FullName, instance);
            }

            Logger.LogDebug($"Instance of [{entry.ToUpper()}]{Metadata.GetId()}({Metadata.GetVersion()})");
            return instances.Values.ToArray();
        }

        public async UniTask SendInitialize()
        {
            Logger.LogDebug($"Initializing {Metadata.GetId()}({Metadata.GetVersion()})");

            if (IsMainEnabled() && _mainState == InitializerState.None)
            {
                _mainState = InitializerState.Initialized;
                foreach (var instance in MainInitializers)
                {
                    Logger.LogDebug($"Initializing main in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                    try
                    {
                        instance.OnInitialize(CoreAPI);
                        await instance.OnInitializeAsync(CoreAPI);
                        instance.OnInitializeMain(CoreAPI);
                        await instance.OnInitializeMainAsync(CoreAPI);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error initializing main in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }
                }
            }

            if (IsEditorEnabled() && _editorState == InitializerState.None)
            {
                _editorState = InitializerState.Initialized;
                foreach (var instance in EditorInitializers)
                {
                    Logger.LogDebug(
                        $"Initializing editor in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                    try
                    {
                        instance.OnInitialize(CoreAPI);
                        await instance.OnInitializeAsync(CoreAPI);
                        instance.OnInitializeEditor(CoreAPI);
                        await instance.OnInitializeEditorAsync(CoreAPI);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error initializing editor in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }
                }
            }

            if (IsServerEnabled() && _serverState == InitializerState.None)
            {
                _serverState = InitializerState.Initialized;
                foreach (var instance in ServerInitializers)
                {
                    Logger.LogDebug(
                        $"Initializing server in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                    try
                    {
                        instance.OnInitialize(CoreAPI);
                        await instance.OnInitializeAsync(CoreAPI);
                        instance.OnInitializeServer(CoreAPI);
                        await instance.OnInitializeServerAsync(CoreAPI);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error initializing server in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }
                }
            }

            if (IsClientEnabled() && _clientState == InitializerState.None)
            {
                _clientState = InitializerState.Initialized;
                foreach (var instance in ClientInitializers)
                {
                    Logger.LogDebug(
                        $"Initializing client in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                    try
                    {
                        instance.OnInitialize(CoreAPI);
                        await instance.OnInitializeAsync(CoreAPI);
                        instance.OnInitializeClient(CoreAPI);
                        await instance.OnInitializeClientAsync(CoreAPI);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error initializing client in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }
                }
            }

            foreach (var entry in InstanceInitializers.Keys)
                if (IsInstanceEnabled(entry) && GetInstanceState(entry) == InitializerState.None)
                {
                    SetInstanceStates(entry, InitializerState.Initialized);
                    foreach (var instance in InstanceInitializers[entry])
                    {
                        Logger.LogDebug(
                            $"Initializing instance {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                        try
                        {
                            instance.OnInitialize(CoreAPI);
                            await instance.OnInitializeAsync(CoreAPI);
                            instance.OnInitializeInstance(CoreAPI);
                            await instance.OnInitializeInstanceAsync(CoreAPI);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(
                                $"Error initializing instance {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                        }
                    }
                }

            foreach (var entry in CustomInitializers.Keys)
                if (IsCustomEnabled(entry) && GetCustomState(entry) == InitializerState.None)
                {
                    SetCustomStates(entry, InitializerState.Initialized);
                    foreach (var instance in CustomInitializers[entry])
                    {
                        Logger.LogDebug(
                            $"Initializing {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                        try
                        {
                            instance.OnInitialize(CoreAPI);
                            await instance.OnInitializeAsync(CoreAPI);
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(
                                $"Error initializing {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                        }
                    }
                }
        }

        public async UniTask SendPostInitialize()
        {
            Logger.LogDebug($"Post initializing {Metadata.GetId()}({Metadata.GetVersion()})");

            if (IsMainEnabled() && _mainState == InitializerState.Initialized)
            {
                _mainState = InitializerState.PostInitialized;
                foreach (var instance in MainInitializers)
                {
                    Logger.LogDebug(
                        $"Post initializing main in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                    try
                    {
                        instance.OnPostInitialize();
                        await instance.OnPostInitializeAsync();
                        instance.OnPostInitializeMain();
                        await instance.OnPostInitializeMainAsync();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error post initializing main in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }
                }
            }

            if (IsEditorEnabled() && _editorState == InitializerState.Initialized)
            {
                _editorState = InitializerState.PostInitialized;
                foreach (var instance in EditorInitializers)
                {
                    Logger.LogDebug(
                        $"Post initializing editor in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                    try
                    {
                        instance.OnPostInitialize();
                        await instance.OnPostInitializeAsync();
                        instance.OnPostInitializeEditor();
                        await instance.OnPostInitializeEditorAsync();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error post initializing editor in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }
                }
            }

            if (IsServerEnabled() && _serverState == InitializerState.Initialized)
            {
                _serverState = InitializerState.PostInitialized;
                foreach (var instance in ServerInitializers)
                {
                    Logger.LogDebug(
                        $"Post initializing server in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                    try
                    {
                        instance.OnPostInitialize();
                        await instance.OnPostInitializeAsync();
                        instance.OnPostInitializeServer();
                        await instance.OnPostInitializeServerAsync();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error post initializing server in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }
                }
            }

            if (IsClientEnabled() && _clientState == InitializerState.Initialized)
            {
                _clientState = InitializerState.PostInitialized;
                foreach (var instance in ClientInitializers)
                {
                    Logger.LogDebug(
                        $"Post initializing client in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                    try
                    {
                        instance.OnPostInitialize();
                        await instance.OnPostInitializeAsync();
                        instance.OnPostInitializeClient();
                        await instance.OnPostInitializeClientAsync();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error post initializing client in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }
                }
            }

            foreach (var entry in InstanceInitializers.Keys)
                if (IsInstanceEnabled(entry) && GetInstanceState(entry) == InitializerState.Initialized)
                {
                    SetInstanceStates(entry, InitializerState.PostInitialized);
                    foreach (var instance in InstanceInitializers[entry])
                    {
                        Logger.LogDebug(
                            $"Post initializing instance {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                        try
                        {
                            instance.OnPostInitialize();
                            await instance.OnPostInitializeAsync();
                            instance.OnPostInitializeInstance();
                            await instance.OnPostInitializeInstanceAsync();
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(
                                $"Error post initializing instance {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                        }
                    }
                }

            foreach (var entry in CustomInitializers.Keys)
                if (IsCustomEnabled(entry) && GetCustomState(entry) == InitializerState.Initialized)
                {
                    SetCustomStates(entry, InitializerState.PostInitialized);
                    foreach (var instance in CustomInitializers[entry])
                    {
                        Logger.LogDebug(
                            $"Post initializing {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                        try
                        {
                            instance.OnPostInitialize();
                            await instance.OnPostInitializeAsync();
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(
                                $"Error post initializing {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                        }
                    }
                }
        }

        public async UniTask SendPreDispose()
        {
            Logger.LogDebug($"Pre disposing {Metadata.GetId()}({Metadata.GetVersion()})");

            if (!IsMainEnabled() && _mainState == InitializerState.PostInitialized)
            {
                _mainState = InitializerState.PreDisposed;
                foreach (var instance in MainInitializers)
                {
                    Logger.LogDebug($"Pre disposing main in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                    try
                    {
                        instance.OnPreDispose();
                        await instance.OnPreDisposeAsync();
                        instance.OnDisposeMain();
                        await instance.OnDisposeMainAsync();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error pre disposing main in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }
                }
            }

            if (!IsEditorEnabled() && _editorState == InitializerState.PostInitialized)
            {
                _editorState = InitializerState.PreDisposed;
                foreach (var instance in EditorInitializers)
                {
                    Logger.LogDebug(
                        $"Pre disposing editor in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                    try
                    {
                        instance.OnPreDispose();
                        await instance.OnPreDisposeAsync();
                        instance.OnDisposeEditor();
                        await instance.OnDisposeEditorAsync();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error pre disposing editor in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }
                }
            }

            if (!IsServerEnabled() && _serverState == InitializerState.PostInitialized)
            {
                _serverState = InitializerState.PreDisposed;
                foreach (var instance in ServerInitializers)
                {
                    Logger.LogDebug(
                        $"Pre disposing server in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                    try
                    {
                        instance.OnPreDispose();
                        await instance.OnPreDisposeAsync();
                        instance.OnDisposeServer();
                        await instance.OnDisposeServerAsync();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error pre disposing server in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }
                }
            }

            Logger.Log($"{IsClientEnabled()} {_clientState}");
            if (!IsClientEnabled() && _clientState == InitializerState.PostInitialized)
            {
                _clientState = InitializerState.PreDisposed;
                foreach (var instance in ClientInitializers)
                {
                    Logger.LogDebug(
                        $"Pre disposing client in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                    try
                    {
                        instance.OnPreDispose();
                        await instance.OnPreDisposeAsync();
                        instance.OnDisposeClient();
                        await instance.OnDisposeClientAsync();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error pre disposing client in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }
                }
            }

            foreach (var entry in InstanceInitializers.Keys)
                if (!IsInstanceEnabled(entry) && GetInstanceState(entry) == InitializerState.PostInitialized)
                {
                    SetInstanceStates(entry, InitializerState.PreDisposed);
                    foreach (var instance in InstanceInitializers[entry])
                    {
                        Logger.LogDebug(
                            $"Pre disposing instance {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                        try
                        {
                            instance.OnPreDispose();
                            await instance.OnPreDisposeAsync();
                            instance.OnDisposeInstance();
                            await instance.OnDisposeInstanceAsync();
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(
                                $"Error pre disposing instance {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                        }
                    }
                }

            foreach (var entry in CustomInitializers.Keys)
                if (!IsCustomEnabled(entry) && GetCustomState(entry) == InitializerState.PostInitialized)
                {
                    SetCustomStates(entry, InitializerState.PreDisposed);
                    foreach (var instance in CustomInitializers[entry])
                    {
                        Logger.LogDebug(
                            $"Pre disposing {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                        try
                        {
                            instance.OnPreDispose();
                            await instance.OnPreDisposeAsync();
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(
                                $"Error pre disposing {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                        }
                    }
                }
        }

        public async UniTask SendDispose()
        {
            Logger.LogDebug($"Disposing {Metadata.GetId()}({Metadata.GetVersion()})");

            Logger.LogDebug($"{IsMainEnabled()} {_mainState}");
            if (!IsMainEnabled() && _mainState == InitializerState.PreDisposed)
            {
                Logger.LogDebug($"Disposing main in {Metadata.GetId()}({Metadata.GetVersion()})");
                _mainState = InitializerState.Disposed;
                foreach (var instance in MainInitializers)
                {
                    Logger.LogDebug($"Disposing main in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                    try
                    {
                        instance.OnDispose();
                        await instance.OnDisposeAsync();
                        instance.OnDisposeMain();
                        await instance.OnDisposeMainAsync();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error disposing main in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }
                }
            }

            if (!IsEditorEnabled() && _editorState == InitializerState.PreDisposed)
            {
                _editorState = InitializerState.Disposed;
                foreach (var instance in EditorInitializers)
                {
                    Logger.LogDebug($"Disposing editor in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                    try
                    {
                        instance.OnDispose();
                        await instance.OnDisposeAsync();
                        instance.OnDisposeEditor();
                        await instance.OnDisposeEditorAsync();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error disposing editor in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }
                }
            }

            if (!IsServerEnabled() && _serverState == InitializerState.PreDisposed)
            {
                _serverState = InitializerState.Disposed;
                foreach (var instance in ServerInitializers)
                {
                    Logger.LogDebug($"Disposing server in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                    try
                    {
                        instance.OnDispose();
                        await instance.OnDisposeAsync();
                        instance.OnDisposeServer();
                        await instance.OnDisposeServerAsync();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error disposing server in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }
                }
            }

            if (!IsClientEnabled() && _clientState == InitializerState.PreDisposed)
            {
                _clientState = InitializerState.Disposed;
                foreach (var instance in ClientInitializers)
                {
                    Logger.LogDebug($"Disposing client in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                    try
                    {
                        instance.OnDispose();
                        await instance.OnDisposeAsync();
                        instance.OnDisposeClient();
                        await instance.OnDisposeClientAsync();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error disposing client in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }
                }
            }

            foreach (var entry in InstanceInitializers.Keys)
                if (!IsInstanceEnabled(entry) && GetInstanceState(entry) == InitializerState.PreDisposed)
                {
                    SetInstanceStates(entry, InitializerState.Disposed);
                    foreach (var instance in InstanceInitializers[entry])
                    {
                        Logger.LogDebug(
                            $"Disposing instance {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                        try
                        {
                            instance.OnDispose();
                            await instance.OnDisposeAsync();
                            instance.OnDisposeInstance();
                            await instance.OnDisposeInstanceAsync();
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(
                                $"Error disposing instance {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                        }
                    }
                }

            foreach (var entry in CustomInitializers.Keys)
                if (!IsCustomEnabled(entry) && GetCustomState(entry) == InitializerState.PreDisposed)
                {
                    SetCustomStates(entry, InitializerState.Disposed);
                    foreach (var instance in CustomInitializers[entry])
                    {
                        Logger.LogDebug(
                            $"Disposing {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                        try
                        {
                            instance.OnDispose();
                            await instance.OnDisposeAsync();
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(
                                $"Error disposing {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                        }
                    }
                }
        }

        public void SendUpdate()
        {
            if (IsMainEnabled() && _mainState == InitializerState.PostInitialized)
                foreach (var instance in MainInitializers)
                    try
                    {
                        instance.OnUpdate();
                        instance.OnUpdateMain();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error updating main in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }

            if (IsEditorEnabled() && _editorState == InitializerState.PostInitialized)
                foreach (var instance in EditorInitializers)
                    try
                    {
                        instance.OnUpdate();
                        instance.OnUpdateEditor();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error updating editor in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }

            if (IsServerEnabled() && _serverState == InitializerState.PostInitialized)
                foreach (var instance in ServerInitializers)
                    try
                    {
                        instance.OnUpdate();
                        instance.OnUpdateServer();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error updating server in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }

            if (IsClientEnabled() && _clientState == InitializerState.PostInitialized)
                foreach (var instance in ClientInitializers)
                    try
                    {
                        instance.OnUpdate();
                        instance.OnUpdateClient();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error updating client in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }

            foreach (var entry in InstanceInitializers.Keys)
                if (IsInstanceEnabled(entry) && GetInstanceState(entry) == InitializerState.PostInitialized)
                    foreach (var instance in InstanceInitializers[entry])
                        try
                        {
                            instance.OnUpdate();
                            instance.OnUpdateInstance();
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(
                                $"Error updating instance {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                        }

            foreach (var entry in CustomInitializers.Keys)
                if (IsCustomEnabled(entry) && GetCustomState(entry) == InitializerState.PostInitialized)
                    foreach (var instance in CustomInitializers[entry])
                        try
                        {
                            instance.OnUpdate();
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(
                                $"Error updating {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                        }
        }

        public void SendLateUpdate()
        {
            if (IsMainEnabled() && _mainState == InitializerState.PostInitialized)
                foreach (var instance in MainInitializers)
                    try
                    {
                        instance.OnLateUpdate();
                        instance.OnLateUpdateMain();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error late updating main in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }

            if (IsEditorEnabled() && _editorState == InitializerState.PostInitialized)
                foreach (var instance in EditorInitializers)
                    try
                    {
                        instance.OnLateUpdate();
                        instance.OnLateUpdateEditor();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error late updating editor in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }

            if (IsServerEnabled() && _serverState == InitializerState.PostInitialized)
                foreach (var instance in ServerInitializers)
                    try
                    {
                        instance.OnLateUpdate();
                        instance.OnLateUpdateServer();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error late updating server in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }

            if (IsClientEnabled() && _clientState == InitializerState.PostInitialized)
                foreach (var instance in ClientInitializers)
                    try
                    {
                        instance.OnLateUpdate();
                        instance.OnLateUpdateClient();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error late updating client in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }

            foreach (var entry in InstanceInitializers.Keys)
                if (IsInstanceEnabled(entry) && GetInstanceState(entry) == InitializerState.PostInitialized)
                    foreach (var instance in InstanceInitializers[entry])
                        try
                        {
                            instance.OnLateUpdate();
                            instance.OnLateUpdateInstance();
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(
                                $"Error late updating instance {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                        }

            foreach (var entry in CustomInitializers.Keys)
                if (IsCustomEnabled(entry) && GetCustomState(entry) == InitializerState.PostInitialized)
                    foreach (var instance in CustomInitializers[entry])
                        try
                        {
                            instance.OnLateUpdate();
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(
                                $"Error late updating {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                        }
        }

        public void SendFixedUpdate()
        {
            if (IsMainEnabled() && _mainState == InitializerState.PostInitialized)
                foreach (var instance in MainInitializers)
                    try
                    {
                        instance.OnFixedUpdate();
                        instance.OnFixedUpdateMain();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error fixed updating main in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }

            if (IsEditorEnabled() && _editorState == InitializerState.PostInitialized)
                foreach (var instance in EditorInitializers)
                    try
                    {
                        instance.OnFixedUpdate();
                        instance.OnFixedUpdateEditor();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error fixed updating editor in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }

            if (IsServerEnabled() && _serverState == InitializerState.PostInitialized)
                foreach (var instance in ServerInitializers)
                    try
                    {
                        instance.OnFixedUpdate();
                        instance.OnFixedUpdateServer();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error fixed updating server in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }

            if (IsClientEnabled() && _clientState == InitializerState.PostInitialized)
                foreach (var instance in ClientInitializers)
                    try
                    {
                        instance.OnFixedUpdate();
                        instance.OnFixedUpdateClient();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(
                            $"Error fixed updating client in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                    }

            foreach (var entry in InstanceInitializers.Keys)
                if (IsInstanceEnabled(entry) && GetInstanceState(entry) == InitializerState.PostInitialized)
                    foreach (var instance in InstanceInitializers[entry])
                        try
                        {
                            instance.OnFixedUpdate();
                            instance.OnFixedUpdateInstance();
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(
                                $"Error fixed updating instance {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                        }

            foreach (var entry in CustomInitializers.Keys)
                if (IsCustomEnabled(entry) && GetCustomState(entry) == InitializerState.PostInitialized)
                    foreach (var instance in CustomInitializers[entry])
                    {
                        Logger.LogDebug(
                            $"Fixed updating {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}");
                        try
                        {
                            instance.OnFixedUpdate();
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(
                                $"Error fixed updating {entry} in {Metadata.GetId()}({Metadata.GetVersion()}) on {instance}: {e}");
                        }
                    }
        }

        public override string ToString() =>
            $"{GetType().Name}[id={Metadata.GetId()}, version={Metadata.GetVersion()}]";
    }

    public enum InitializerState : byte
    {
        None = 0,
        Initialized = 1,
        PostInitialized = 2,

        PreDisposed = 4,
        Disposed = 8,

        Ready = PostInitialized & Disposed,
        Done = Initialized & Disposed
    }
}