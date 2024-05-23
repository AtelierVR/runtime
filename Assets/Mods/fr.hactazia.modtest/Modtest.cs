// #if UNITY_EDITOR
// using Nox.CCK.Editor;
// using Nox.CCK.Mods;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.UIElements;

// namespace fr.hactazia.modtest
// {
//     [InitializeOnLoad, CCKMod("fr.hactazia.modtest", "0.0.1")]
//     public class Modtest : CCKDescriptor
//     {
//         public override void OnLoad() { }
//         public override void OnUnload() { }

//         [CCKTab("fr.hactazia.modtest.about", "ModTest/About")]
//         public VisualElement AboutTab(bool selected = false)
//         {
//             if (!selected) return null;
//             var root = new VisualElement();
//             root.Add(new Label("This is a test mod."));
//             var button = new Button(() => CCKPanel.Goto("fr.hactazia.modtest.hidded"));
//             button.text = "Go to hidded tab";
//             root.Add(button);
//             return root;
//         }

//         [CCKTab("fr.hactazia.modtest.hidded", "ModTest/Hidded", CCKTabFlags.Hidden)]
//         public VisualElement HiddedTab(bool selected = false)
//         {
//             if (!selected) return null;
//             var root = new VisualElement();
//             root.Add(new Label("This is a hidded tab."));
//             return root;
//         }
//     }
// }
// #endif