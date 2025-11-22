#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using api.nox.avatar.network;
using Nox.Avatars.Editor;
using Nox.CCK.Avatars;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Editor.Panel;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using IPanel = Nox.Editor.Panel.IPanel;

namespace api.nox.avatar.editor {
	public class PublisherPanel : IEditorModInitializer, IPanel {
		private static readonly string[]         PanelPath = { "avatar", "publisher" };
		internal                EditorModCoreAPI API;

		public void OnInitializeEditor(EditorModCoreAPI api)
			=> API = api;

		public void OnDisposeEditor()
			=> API = null;

		public string[] GetPath()
			=> PanelPath;

		internal PublisherInstance Instance;

		public IInstance[] GetInstances()
			=> Instance != null
				? new IInstance[] { Instance }
				: Array.Empty<IInstance>();

		public string GetLabel()
			=> "Avatar/Publisher";

		public IInstance Instantiate(IWindow window, Dictionary<string, object> data) {
			if (Instance != null)
				throw new InvalidOperationException("PublisherInstance only supports a single instance.");
			return Instance = new PublisherInstance(this, window, data);
		}
	}
}
#endif