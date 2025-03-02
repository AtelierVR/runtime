using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;

namespace Nox.CCK.Mods.Initializers
{
    public interface EditorModInitializer : IModInitializer
    {
        public void OnInitializeEditor(EditorModCoreAPI api) { }
        public UniTask OnInitializeEditorAsync(EditorModCoreAPI api) => UniTask.CompletedTask;
        public void OnPostInitializeEditor() { }
        public UniTask OnPostInitializeEditorAsync() => UniTask.CompletedTask;

        public void OnUpdateEditor() { }
        public void OnLateUpdateEditor() { }
        public void OnFixedUpdateEditor() { }   

        public void OnPreDisposeEditor() { }
        public UniTask OnPreDisposeEditorAsync() => UniTask.CompletedTask;
        public void OnDisposeEditor() { }
        public UniTask OnDisposeEditorAsync() => UniTask.CompletedTask;
    }
}