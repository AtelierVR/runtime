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
    }
}