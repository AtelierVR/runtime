using Nox.CCK.Mods.Metadata;

namespace Nox.CCK.Mods.Libs
{
    /// <summary>
    /// API for loading and managing mod libraries in the editor.
    /// </summary>
    public interface EditorLibsAPI
    {
        /// <summary>
        /// Loads mod metadata from the specified path.
        /// </summary>
        /// <param name="path">The path to the mod metadata file.</param>
        /// <returns>The loaded mod metadata.</returns>
        public ModMetadata LoadMetadata(string path);
    }
}