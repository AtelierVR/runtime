#if UNITY_EDITOR
using Nox.CCK.Utils;
using UnityEditor;
using UnityEditor.Build;

namespace Nox.Editor {
	public class ModdingSettings {
		[InitializeOnLoadMethod]
		public static void Init() {
			ScriptingDefinitions.Add("NOX_SDK", NamedBuildTarget.Standalone);
			PlayerSettings.stripEngineCode = false;
			PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
			PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
			PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
			ModLinkerHelper.EnsureLinkerClassExists();
		}
	}
}
#endif