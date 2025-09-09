#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace Nox.CCK.Utils {
	public class ScriptingDefinitions {
		public static void Add(string define, NamedBuildTarget target) {
			var defines = PlayerSettings.GetScriptingDefineSymbols(target).Split(';');
			var list    = new List<string>(defines);
			if (list.Contains(define)) return;
			list.Add(define);
			PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", list.ToArray()));
		}

		public static void Remove(string define, NamedBuildTarget target) {
			var defines = PlayerSettings.GetScriptingDefineSymbols(target).Split(';');
			var list    = new List<string>(defines);
			if (!list.Contains(define)) return;
			list.Remove(define);
			PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", list.ToArray()));
		}
	}
}
#endif