// #if UNITY_EDITOR
// using System;
// using System.IO;
// using System.Linq;
// using Jint;
// using Jint.Native;
// using UnityEditor;
// using UnityEditor.SceneManagement;
// using UnityEditor.UIElements;
// using UnityEngine;
// using UnityEngine.SceneManagement;
// using UnityEngine.UIElements;
// using Logger = Nox.CCK.Utils.Logger;
//
//
// namespace Nox.CCK.Worlds
// {
//     [CustomEditor(typeof(JintScript)), CanEditMultipleObjects]
//     public class JintScriptEditor : Editor
//     {
//         public override bool UseDefaultMargins() => false;
//
//         private VisualElement _root;
//
//         public override VisualElement CreateInspectorGUI()
//         {
//             if (_root != null) return _root;
//             _root = Resources.Load<VisualTreeAsset>("api.nox.cck.world.jintscript").CloneTree();
//             var script = target as JintScript;
//             if (script == null) return _root;
//
//             var comp = _root.Q<VisualElement>("compiled-message");
//             if (script.isCompiled)
//             {
//                 comp.style.display = DisplayStyle.Flex;
//                 comp.Q<Image>("icon").image = Resources.Load<Texture2D>("warning.png");
//             }
//             else comp.style.display = DisplayStyle.None;
//
//             var exports = _root.Q<VisualElement>("exports");
//             exports.Clear();
//             foreach (var key in script.GetDataKeys())
//             {
//                 var field = new TextField(key);
//                 field.RegisterValueChangedCallback(evt => script.SetData(key, evt.newValue));
//                 exports.Add(field);
//             }
//
//             var newButton = _root.Q<Button>("new");
//             var openButton = _root.Q<Button>("open");
//             var pathField = _root.Q<TextField>("path");
//
//             newButton.clicked += () =>
//             {
//                 if (script.isCompiled)
//                 {
//                     EditorUtility.DisplayDialog("Script already compiled",
//                         "You cannot change the script path after it has been compiled", "OK");
//                     return;
//                 }
//
//                 var activeScene = SceneManager.GetActiveScene();
//                 var scenePath = activeScene.path;
//
//                 var path = EditorUtility.SaveFilePanel("Save script",
//                     Path.GetDirectoryName(scenePath),
//                     "script.js",
//                     "js"
//                 );
//
//                 if (string.IsNullOrEmpty(path)) return;
//
//                 if (!path.StartsWith(Application.dataPath))
//                 {
//                     EditorUtility.DisplayDialog("Invalid path",
//                         "The script must be in the Assets folder", "OK");
//                     return;
//                 }
//
//                 if (!File.Exists(path))
//                 {
//                     var example = Resources.Load<TextAsset>("jint_example.js");
//                     File.WriteAllText(path, example?.text ?? "");
//                 }
//
//                 SetRelativePath(path);
//                 // pathField.SetValueWithoutNotify(script.scriptPath);
//                 EditorUtility.SetDirty(target);
//                 UpdateGui();
//             };
//
//             openButton.clicked += () =>
//             {
//                 if (script.isCompiled)
//                 {
//                     EditorUtility.DisplayDialog("Script already compiled",
//                         "You cannot change the script path after it has been compiled", "OK");
//                     return;
//                 }
//             };
//
//             pathField.RegisterValueChangedCallback(evt =>
//             {
//                 if (script.isCompiled)
//                 {
//                     EditorUtility.DisplayDialog("Script already compiled",
//                         "You cannot change the script path after it has been compiled", "OK");
//                     return;
//                 }
//
//                 if (File.Exists(evt.newValue))
//                 {
//                     EditorUtility.OpenWithDefaultApp(evt.newValue);
//                     return;
//                 }
//             });
//
//             UpdateGui();
//
//             script.onLog.AddListener(OnLog);
//             OnLog();
//
//             return _root;
//         }
//
//         private void OnLog()
//         {
//             var script = target as JintScript;
//             if (!script || _root == null) return;
//             var logContainer = _root.Q<VisualElement>("logs");
//             logContainer.Clear();
//             foreach (var log in script.LogList ?? Enumerable.Empty<JintScript.LogData>())
//                 logContainer.Insert(0, new Label($"[{log.Type}/{log.Time:HH:mm:ss}] {log.Message}")
//                 {
//                     style =
//                     {
//                         color = log.Type switch
//                         {
//                             Utils.LogType.Error => Color.red,
//                             Utils.LogType.Warning => Color.yellow,
//                             _ => Color.white
//                         },
//                         whiteSpace = WhiteSpace.PreWrap,
//                         overflow = Overflow.Hidden
//                     }
//                 });
//         }
//
//         private void SetRelativePath(string path)
//         {
//             var script = target as JintScript;
//             if (script == null) return;
//             // script.scriptPath = path.StartsWith(Application.dataPath)
//             //     ? path.Replace(Application.dataPath, "Assets")
//             //     : path;
//             EditorUtility.SetDirty(target);
//             UpdateGui();
//         }
//
//         private void UpdateGui()
//         {
//             var newButton = _root.Q<Button>("new");
//             var openButton = _root.Q<Button>("open");
//             var pathField = _root.Q<TextField>("path");
//             var script = target as JintScript;
//             if (script == null) return;
//             // pathField.value = script.scriptPath;
//             // newButton.style.display = string.IsNullOrEmpty(script.scriptPath) || !File.Exists(script.scriptPath)
//             //     ? DisplayStyle.Flex
//             //     : DisplayStyle.None;
//             // openButton.style.display = string.IsNullOrEmpty(script.scriptPath) || !File.Exists(script.scriptPath)
//             //     ? DisplayStyle.None
//             //     : DisplayStyle.Flex;
//             pathField.SetEnabled(!script.isCompiled);
//         }
//
//         void OnDestroy()
//         {
//             var script = target as JintScript;
//             if (script == null) return;
//             script.onLog.RemoveListener(OnLog);
//         }
//     }
// }
// #endif // UNITY_EDITOR