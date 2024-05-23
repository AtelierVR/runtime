// #if UNITY_EDITOR
// using System.Linq;
// using Nox.CCK.Mods;
// using UnityEditor;
// using UnityEditor.UIElements;
// using UnityEngine;
// using UnityEngine.UIElements;

// namespace Nox.CCK.Editor
// {

//     public class CCKPanel : EditorWindow
//     {
//         public static CCKPanel Instance;
//         [MenuItem("Nox/CCK Panel")]
//         public static void ShowWindow()
//         {
//             if (Instance == null)
//                 Instance = GetWindow<CCKPanel>();
//         }

//         [MenuItem("Nox/Restart")]
//         public static void Restart() => CCKModManager.Reload();

//         [MenuItem("Nox/Compile")]
//         public static void Compile() => AssetDatabase.Refresh();

//         internal class TabData
//         {
//             public string Name;
//             public VisualElement Content;
//         }

//         private void OnGUI()
//         {
//             if (Instance == null || rootVisualElement.childCount > 0) return;
//             var root = Resources.Load<VisualTreeAsset>("api.nox.cck.panel").CloneTree();
//             rootVisualElement.Clear();
//             root.style.flexGrow = 1;
//             rootVisualElement.Add(root);
//             UpdateMenu();

//             if (!Goto("api.nox.default"))
//             {
//                 var home = new VisualElement();
//                 home.Add(new Label("Welcome to the Nox CCK."));
//                 rootVisualElement.Q<VisualElement>("content").Add(home);
//             }

//             rootVisualElement.Q<ToolbarButton>("restart").clicked += Restart;
//         }

//         public static void UpdateMenu()
//         {
//             if (Instance == null) return;
//             var dropdown = Instance.rootVisualElement.Q<ToolbarMenu>("pages");
//             dropdown.text = "Menu";
//             dropdown.Clear();
//             var mods = CCKModManager.Mods;
//             foreach (var mod in mods)
//             {
//                 var modAttr = mod.GetMethodWiths<CCKTabAttribute>();
//                 if (modAttr.Length == 0) continue;
//                 foreach (var tab in modAttr)
//                     if (!tab.Attribute.Flags.HasFlag(CCKTabFlags.Hidden))
//                         dropdown.menu.AppendAction(tab.Attribute.Name, a => Goto(tab.Attribute.Id), a => DropdownMenuAction.Status.Normal);
//             }
//         }

//         private void OnEnable()
//         {
//             Instance = this;
//         }

//         private TabSearch GetTab(string id)
//         {
//             var mods = CCKModManager.Mods;
//             foreach (var mod in mods)
//             {
//                 var modAttr = mod.GetMethodWiths<CCKTabAttribute>();
//                 if (modAttr.Length == 0) continue;
//                 foreach (var tab in modAttr)
//                     if (tab.Attribute.Id == id) return new TabSearch
//                     {
//                         tab = tab,
//                         mod = mod,
//                         search = id
//                     };
//             }
//             return null;
//         }

//         public static bool Goto(string id)
//         {
//             var tab = Instance.GetTab(id);
//             if (tab == null) return false;
//             if (tab.tab.Method.Invoke(tab.mod, new object[] { true }) is not VisualElement content) return false;
//             var root = Instance.rootVisualElement.Q<VisualElement>("content");
//             if (root == null) return false;
//             content.style.flexGrow = 1;
//             foreach (var child in content.Children())
//                 child.style.flexGrow = 1;
//             foreach (var child in root.Children().ToList())
//                 if (child.name != id)
//                 {
//                     root.Remove(child);
//                     var o = Instance.GetTab(child.name);
//                     o?.tab.Method.Invoke(o.mod, new object[] { false });
//                 }
//             root.Add(content);
//             content.name = id;
//             return true;
//         }

//         public static bool HasTab(string id)
//         {
//             if (Instance == null) return false;
//             var root = Instance.rootVisualElement.Q<VisualElement>("content");
//             if (root == null) return false;
//             foreach (var child in root.Children().ToList())
//                 if (child.name == id)
//                     return true;
//             return false;
//         }
//     }

//     public class TabSearch
//     {
//         public Descriptor.MethodWithAttribute<CCKTabAttribute> tab;
//         public string search;
//         public CCKDescriptor mod;
//     }
// }
// #endif