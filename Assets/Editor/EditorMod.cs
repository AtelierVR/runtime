using System;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Initializers;

namespace Nox.Mods
{
    public class EditorMod : Mod
    {
        public EditorMod(CCK.Mods.ModMetadata metadata, EditorModInitializer initializer)
        {
            _metadata = metadata;
            _initializer = initializer;
        }

        private CCK.Mods.ModMetadata _metadata;
        private EditorModInitializer _initializer;

        public CCK.Mods.ModMetadata GetMetadata() => _metadata;
        public T GetAssembly<T>() where T : ModInitializer => (T)_initializer;

        internal EditorModInitializer GetEditorAssembly() => _initializer;

        private bool _enabled = false;
        public bool IsEnabled() => _enabled;
        public void SetEnabled(bool enabled)
        {
            if (_enabled == enabled) return;
            if (enabled) _initializer.OnInitializeEditor(null);
            else _initializer.OnDispose();
            _enabled = enabled;
        }
    }
}