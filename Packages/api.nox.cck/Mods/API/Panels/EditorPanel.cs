using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Nox.CCK.Mods.Panels
{
    public interface IEditorPanel
    {
        /// <summary>
        /// Get the mod ID that this panel belongs to.
        /// </summary>
        /// <returns></returns>
        public string GetModId();
        
        /// <summary>
        /// Get the unique ID of this panel.
        /// </summary>
        /// <returns></returns>
        public string GetId();
        
        /// <summary>
        /// Get the display name of this panel.
        /// </summary>
        /// <returns></returns>
        public string GetName();
        
        /// <summary>
        /// Check if this panel is hidden from the editor UI.
        /// </summary>
        /// <returns></returns>
        public bool IsHidden();
        
        /// <summary>
        /// Get the full ID of this panel in the format "modId.panelId".
        /// </summary>
        /// <returns></returns>
        public string GetFullId() => $"{GetModId()}.{GetId()}";

        /// <summary>
        /// Create the content of the panel.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public VisualElement MakeContent(Dictionary<string, object> data = null);
        
        /// <summary>
        /// Check if this panel is currently active.
        /// </summary>
        /// <returns></returns>
        public bool IsActive();
    }
}