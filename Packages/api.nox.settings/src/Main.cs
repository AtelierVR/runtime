using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.Controllers;
using Nox.Settings;
using UnityEngine.Events;

namespace api.nox.settings {
	public class Main : IMainModInitializer, ISettingAPI {
		internal static readonly List<IHandler> Handlers = new();
		public static            Main           Instance;
		public                   MainModCoreAPI CoreAPI;
		private                  LanguagePack   _lang;

		public static IControllerAPI ControllerAPI
			=> Instance.CoreAPI.ModAPI
				.GetMod("controller")
				.GetInstance<IControllerAPI>();

		internal static readonly UnityEvent<IHandler> OnHandlerAdded   = new();
		internal static readonly UnityEvent<IHandler> OnHandlerRemoved = new();

		private void InvokeHandlerAdded(IHandler handler) {
			OnHandlerAdded.Invoke(handler);
			CoreAPI.EventAPI.Emit("setting_handler_added", handler);
		}

		private void InvokeHandlerRemoved(IHandler handler) {
			OnHandlerRemoved.Invoke(handler);
			CoreAPI.EventAPI.Emit("setting_handler_removed", handler);
		}

		public IHandler Add(IHandler handler) {
			if (handler == null) {
				CoreAPI.LoggerAPI.LogError("Cannot register a null setting handler");
				return null;
			}

			if (string.IsNullOrWhiteSpace(handler.GetPath().FirstOrDefault())) {
				CoreAPI.LoggerAPI.LogError("Cannot register a setting handler with an empty id");
				return null;
			}

			if (Has(handler.GetPath())) {
				CoreAPI.LoggerAPI.LogError($"Setting handler with id {handler.GetPath()} already exists");
				return null;
			}

			Handlers.Add(handler);
			InvokeHandlerAdded(handler);
			return handler;
		}

		public void Remove(string[] path) {
			var handler = Get(path);

			if (handler == null) {
				CoreAPI.LoggerAPI.LogError($"Setting handler with id {path} does not exist");
				return;
			}

			if (handler is IDisposable disposable)
				disposable.Dispose();

			Handlers.Remove(handler);
			InvokeHandlerRemoved(handler);
		}

		public IHandler Get(string[] path)
			=> Handlers.FirstOrDefault(h => h.GetPath().SequenceEqual(path));

		public bool Has(string[] path)
			=> Handlers.Exists(b => b.GetPath().SequenceEqual(path));

		private IHandler[] _handlers = Array.Empty<IHandler>();

		public void OnInitializeMain(MainModCoreAPI api) {
			Instance = this;
			CoreAPI  = api;
			_lang    = CoreAPI.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);
			_handlers = DefaultSettings.GetHandlers();
			foreach (var handler in _handlers)
				Add(handler);
		}

		public void OnDisposeMain() {
			foreach (var handler in Handlers)
				Remove(handler.GetPath());
			Handlers.Clear();
			_handlers = Array.Empty<IHandler>();
			LanguageManager.RemovePack(_lang);
			_lang    = null;
			Instance = null;
			CoreAPI  = null;
		}
	}
}