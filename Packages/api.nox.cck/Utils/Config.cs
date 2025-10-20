using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Nox.CCK.Utils {
	public class Config {
		public static string GetPath()
			=> Path.Combine(Constants.AppPath, "config.json");

		public static Config Current;

		private JObject _jsonObject = new();
		private string  _path;


		public static Config Load(bool force = false) {
			if (Current != null && !force) return Current;
			if (!File.Exists(GetPath()))
				return new Config { _path = GetPath() }.Save();
			var jsonString = File.ReadAllText(GetPath());
			var config     = new Config { _jsonObject = JObject.Parse(jsonString), _path = GetPath() };
			Current = config;
			return config;
		}

		#if UNITY_EDITOR
		public static string GetEditorPath()
			=> Path.Combine(Application.dataPath, "..", "Library", "NoxEditorConfig.json");

		public static Config CurrentEditor;

		public static Config LoadEditor(bool force = false) {
			if (CurrentEditor != null && !force) return CurrentEditor;
			if (!File.Exists(GetEditorPath()))
				return new Config { _path = GetEditorPath() }.Save();
			var jsonString = File.ReadAllText(GetEditorPath());
			var config     = new Config { _jsonObject = JObject.Parse(jsonString), _path = GetEditorPath() };
			CurrentEditor = config;
			return config;
		}

		[UnityEditor.MenuItem("Nox/Config/Edit Config")]
		private static void EditConfig() {
			if (File.Exists(GetPath()))
				UnityEditor.EditorUtility.OpenWithDefaultApp(GetPath());
			else UnityEditor.EditorUtility.DisplayDialog("Nox Config", "No config file found.", "OK");
		}

		[UnityEditor.MenuItem("Nox/Config/Reveal Config")]
		private static void OpenConfigFolder() {
			if (File.Exists(GetPath()))
				UnityEditor.EditorUtility.RevealInFinder(GetPath());
			else UnityEditor.EditorUtility.DisplayDialog("Nox Config", "No config file found.", "OK");
		}

		[UnityEditor.MenuItem("Nox/Config/Edit Editor Config")]
		private static void EditEditorConfig() {
			if (File.Exists(GetEditorPath()))
				UnityEditor.EditorUtility.OpenWithDefaultApp(GetEditorPath());
			else UnityEditor.EditorUtility.DisplayDialog("Nox Config", "No config file found.", "OK");
		}

		[UnityEditor.MenuItem("Nox/Config/Reveal Editor Config")]
		private static void OpenEditorConfigFolder() {
			if (File.Exists(GetEditorPath()))
				UnityEditor.EditorUtility.RevealInFinder(GetEditorPath());
			else UnityEditor.EditorUtility.DisplayDialog("Nox Config", "No config file found.", "OK");
		}


		#endif

		public bool Has(string propertyName)
			=> Has(propertyName.Split('.'));

		public bool Has(string[] propertyPathName) {
			var current = _jsonObject;
			for (var i = 0; i < propertyPathName.Length - 1; i++) {
				if (current[propertyPathName[i]] == null)
					return false;
				current = (JObject)current[propertyPathName[i]];
			}

			return current[propertyPathName[^1]] != null;
		}

		public JToken Get(string propertyName)
			=> Get(propertyName.Split('.'));

		public JToken Get(string[] propertyPathName) {
			var current = _jsonObject;
			for (var i = 0; i < propertyPathName.Length - 1; i++) {
				if (current[propertyPathName[i]] == null)
					return null;
				current = (JObject)current[propertyPathName[i]];
			}

			return current[propertyPathName[^1]];
		}

		public JObject Get()
			=> _jsonObject;

		public T Get<T>(string propertyName, T defaultValue = default)
			=> Get(propertyName.Split('.'), defaultValue);

		public T Get<T>(string[] propertyPathName, T defaultValue = default) {
			var token = Get(propertyPathName);
			return token == null ? defaultValue : token.ToObject<T>();
		}

		public void Set<T>(string propertyName, T value)
			=> Set(propertyName.Split('.'), value);

		public void Set<T>(string[] propertyPathName, T value) {
			var current = _jsonObject;
			for (var i = 0; i < propertyPathName.Length - 1; i++) {
				if (current[propertyPathName[i]] == null)
					current[propertyPathName[i]] = new JObject();
				current = (JObject)current[propertyPathName[i]];
			}

			if (value == null)
				current.Remove(propertyPathName[^1]);
			else current[propertyPathName[^1]] = JToken.FromObject(value);
		}

		public void Remove(string propertyName)
			=> Remove(propertyName.Split('.'));

		public void Remove(string[] propertyPathName) {
			var current = _jsonObject;
			for (var i = 0; i < propertyPathName.Length - 1; i++) {
				if (current[propertyPathName[i]] == null)
					return;
				current = (JObject)current[propertyPathName[i]];
			}

			current.Remove(propertyPathName[^1]);
		}

		public Config Save() {
			File.WriteAllText(_path, _jsonObject.ToString());
			return this;
		}
	}
}