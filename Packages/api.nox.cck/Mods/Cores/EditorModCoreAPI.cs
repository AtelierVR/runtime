using Nox.CCK.Mods.Libs;
using Nox.CCK.Mods.Panels;

namespace Nox.CCK.Mods.Cores
{
    /// <summary>
    /// Core API for editor mods, providing access to editor-specific APIs.
    /// </summary>
    public interface EditorModCoreAPI : IModCoreAPI
    {
        /// <summary>
        /// Gets the panel management API for the editor.
        /// </summary>
        public IEditorModPanelAPI PanelAPI { get; }
        
        /// <summary>
        /// Gets the libraries API for the editor.
        /// </summary>
        public EditorLibsAPI LibsAPI { get; }
    }
}