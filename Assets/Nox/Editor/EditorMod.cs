using System;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Initializers;

namespace Nox.Editor.Mods
{
    public class EditorMod : Mod
    {
        public EditorMod(ModMetadata metadata, ModInitializer[] initializers, EditorModInitializer[] editorinitializers)
        {
            _metadata = metadata;
            _initializers = initializers;
            _editorinitializers = editorinitializers;
            coreAPI = new EditorModCoreAPI(this);
        }

        private ModMetadata _metadata;
        private ModInitializer[] _initializers;
        private EditorModInitializer[] _editorinitializers;

        public ModMetadata GetMetadata() => _metadata;
        public ModInitializer[] GetMainClasses() => _initializers;
        internal EditorModInitializer[] GetEditorClasses() => _editorinitializers;
        internal EditorModCoreAPI coreAPI;

        private bool _enabled = false;
        public bool IsEnabled() => _enabled;
        internal bool SetEnabled(bool enabled)
        {
            if (_enabled != enabled)
                try
                {
                    _enabled = enabled;
                    if (enabled)
                    {
                        foreach (var initializer in _initializers)
                            initializer.OnInitialize(coreAPI);
                        foreach (var initializer in _editorinitializers)
                            initializer.OnInitializeEditor(coreAPI);
                    }
                    else
                    {
                        foreach (var initializer in _editorinitializers)
                            initializer.OnDispose();
                        foreach (var initializer in _initializers)
                            initializer.OnDispose();
                    }

                }
                catch (Exception e)
                {
                    _enabled = !enabled;
                    UnityEngine.Debug.LogError($"Error while {(enabled ? "enabling" : "disabling")} mod {_metadata.GetId()}: {e}");
                }
            return _enabled;
        }
    }
}