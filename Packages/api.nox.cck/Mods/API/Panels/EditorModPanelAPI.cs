namespace Nox.CCK.Mods.Panels
{
    /// <summary>
    /// API for managing editor panels in the Nox CCK.
    /// </summary>
    public interface IEditorModPanelAPI
    {
        /// <summary>
        /// Sets the active panel.
        /// </summary>
        /// <param name="panel">The panel to activate.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        public bool SetActivePanel(IEditorPanel panel);
        
        /// <summary>
        /// Sets the active panel by its ID.
        /// </summary>
        /// <param name="panelId">The ID of the panel to activate.</param>
        /// <returns>True if successful; otherwise, false.</returns>
        public bool SetActivePanel(string panelId);
        
        /// <summary>
        /// Gets the currently active panel.
        /// </summary>
        /// <returns>The active panel, or null if none is active.</returns>
        public IEditorPanel GetActivePanel();
        
        /// <summary>
        /// Checks if the specified panel is currently active.
        /// </summary>
        /// <param name="panel">The panel to check.</param>
        /// <returns>True if the panel is active; otherwise, false.</returns>
        public bool IsActivePanel(IEditorPanel panel);
        
        /// <summary>
        /// Checks if the panel with the specified ID is currently active.
        /// </summary>
        /// <param name="panelId">The ID of the panel to check.</param>
        /// <returns>True if the panel is active; otherwise, false.</returns>
        public bool IsActivePanel(string panelId);

        /// <summary>
        /// Gets a panel by its ID.
        /// </summary>
        /// <param name="panelId">The ID of the panel.</param>
        /// <returns>The panel, or null if not found.</returns>
        public IEditorPanel GetPanel(string panelId);
        
        /// <summary>
        /// Gets all available panels.
        /// </summary>
        /// <returns>An array of all panels.</returns>
        public IEditorPanel[] GetPanels();
        
        /// <summary>
        /// Checks if a panel exists.
        /// </summary>
        /// <param name="panel">The panel to check.</param>
        /// <returns>True if the panel exists; otherwise, false.</returns>
        public bool HasPanel(IEditorPanel panel);
        
        /// <summary>
        /// Checks if a panel with the specified ID exists.
        /// </summary>
        /// <param name="panelId">The ID of the panel to check.</param>
        /// <returns>True if the panel exists; otherwise, false.</returns>
        public bool HasPanel(string panelId);

        /// <summary>
        /// Adds a local panel (specific to the current mod).
        /// </summary>
        /// <param name="panel">The panel builder to add.</param>
        /// <returns>The created editor panel.</returns>
        public IEditorPanel AddLocalPanel(IEditorPanelBuilder panel);
        
        /// <summary>
        /// Removes a local panel.
        /// </summary>
        /// <param name="panel">The panel to remove.</param>
        /// <returns>True if successfully removed; otherwise, false.</returns>
        public bool RemoveLocalPanel(IEditorPanel panel);
        
        /// <summary>
        /// Removes a local panel by its ID.
        /// </summary>
        /// <param name="panelId">The ID of the panel to remove.</param>
        /// <returns>True if successfully removed; otherwise, false.</returns>
        public bool RemoveLocalPanel(string panelId);
        
        /// <summary>
        /// Checks if a local panel exists.
        /// </summary>
        /// <param name="panel">The panel to check.</param>
        /// <returns>True if the local panel exists; otherwise, false.</returns>
        public bool HasLocalPanel(IEditorPanel panel);
        
        /// <summary>
        /// Checks if a local panel with the specified ID exists.
        /// </summary>
        /// <param name="panelId">The ID of the panel to check.</param>
        /// <returns>True if the local panel exists; otherwise, false.</returns>
        public bool HasLocalPanel(string panelId);

        /// <summary>
        /// Gets a local panel by its ID.
        /// </summary>
        /// <param name="panelId">The ID of the panel.</param>
        /// <returns>The local panel, or null if not found.</returns>
        public IEditorPanel GetLocalPanel(string panelId);
        
        /// <summary>
        /// Gets all local panels.
        /// </summary>
        /// <returns>An array of all local panels.</returns>
        public IEditorPanel[] GetLocalPanels();

        /// <summary>
        /// Updates the panel list (refreshes the panel registry).
        /// </summary>
        public void UpdatePanelList();
        
        /// <summary>
        /// Shows the editor mod panel window.
        /// </summary>
        public void ShowWindow();
        
        /// <summary>
        /// Hides the editor mod panel window.
        /// </summary>
        public void CloseWindow();
    }
}