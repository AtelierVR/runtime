// #if UNITY_EDITOR
// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Text.RegularExpressions;
// using UnityEditor;
// using UnityEditor.SceneManagement;
// using UnityEditor.UIElements;
// using UnityEngine;
// using UnityEngine.SceneManagement;
// using UnityEngine.UIElements;
// using Logger = Nox.CCK.Utils.Logger;
//
// namespace Nox.CCK.Worlds
// {
//     [CustomEditor(typeof(MainDescriptor)), CanEditMultipleObjects]
//     public class MainDescriptorEditor : Editor
//     {
//         public override bool UseDefaultMargins() => false;
//
//         public override VisualElement CreateInspectorGUI()
//         {
//             var root = Resources.Load<VisualTreeAsset>("api.nox.cck.world.maindescriptor").CloneTree();
//             var descriptor = target as MainDescriptor;
//             if (descriptor == null) return root;
//             var comp = root.Q<VisualElement>("compiled-message");
//             if (descriptor.IsCompiled)
//             {
//                 comp.style.display = DisplayStyle.Flex;
//                 comp.Q<Image>("icon").image = Resources.Load<Texture2D>("warning.png");
//             }
//             else
//             {
//                 comp.style.display = DisplayStyle.None;
//             }
//
//             var spawns = root.Q<ListView>("spawns");
//             spawns.showAddRemoveFooter = !descriptor.IsCompiled;
//             spawns.allowAdd = !descriptor.IsCompiled;
//             spawns.allowRemove = !descriptor.IsCompiled;
//             spawns.showBoundCollectionSize = !descriptor.IsCompiled;
//             spawns.makeItem = () =>
//             {
//                 var item = new VisualElement
//                 {
//                     style =
//                     {
//                         paddingLeft = 2,
//                         paddingRight = 8
//                     }
//                 };
//                 var obj = new ObjectField()
//                     { objectType = typeof(GameObject), label = "#-", focusable = !descriptor.IsCompiled };
//                 if (!descriptor.IsCompiled)
//                     obj.RegisterValueChangedCallback(evt =>
//                     {
//                         if (item.userData is int i and >= 0 && i < descriptor.GetSpawns().Count)
//                             descriptor.Spawns[i] = evt.newValue as GameObject;
//                         EditorUtility.SetDirty(descriptor);
//                     });
//                 else obj.SetEnabled(false);
//                 item.Add(obj);
//                 return item;
//             };
//             spawns.bindItem = (e, i) =>
//             {
//                 e.Q<ObjectField>().label = "#" + i;
//                 e.Q<ObjectField>().value = descriptor.GetSpawns()[i];
//                 e.userData = i;
//             };
//             spawns.itemsSource = descriptor.GetSpawns();
//
//             if (!descriptor.IsCompiled)
//                 root.Q<Button>("spawns-normalize").clicked += () =>
//                 {
//                     var o = descriptor.EstimateSpawns();
//                     descriptor.Spawns = o.Values.ToList();
//                     spawns.itemsSource = descriptor.GetSpawns();
//                     EditorUtility.SetDirty(descriptor);
//                 };
//             else root.Q<Button>("spawns-normalize").SetEnabled(false);
//
//
//             // var jintscripts = root.Q<ListView>("jintscripts-objects");
//             // jintscripts.showAddRemoveFooter = !descriptor.IsCompiled;
//             // jintscripts.allowAdd = !descriptor.IsCompiled;
//             // jintscripts.allowRemove = !descriptor.IsCompiled;
//             // jintscripts.showBoundCollectionSize = !descriptor.IsCompiled;
//             // jintscripts.makeItem = () =>
//             // {
//             //     var item = new VisualElement
//             //     {
//             //         style =
//             //         {
//             //             paddingLeft = 2,
//             //             paddingRight = 8
//             //         }
//             //     };
//             //     var obj = new ObjectField()
//             //         { objectType = typeof(JintScript), label = "#-", focusable = !descriptor.IsCompiled };
//             //     if (!descriptor.IsCompiled)
//             //         obj.RegisterValueChangedCallback(evt =>
//             //         {
//             //             if (item.userData is int i and >= 0 && i < descriptor.GetJintScripts().Count)
//             //                 descriptor.JintScripts[i] = evt.newValue as JintScript;
//             //             EditorUtility.SetDirty(descriptor);
//             //         });
//             //     else obj.SetEnabled(false);
//             //     item.Add(obj);
//             //     return item;
//             // };
//             // jintscripts.bindItem = (e, i) =>
//             // {
//             //     e.Q<ObjectField>().label = "#" + i;
//             //     e.Q<ObjectField>().value = descriptor.GetJintScripts()[i];
//             //     e.userData = i;
//             // };
//             // jintscripts.itemsSource = descriptor.GetJintScripts();
//             //
//             // if (!descriptor.IsCompiled)
//             //     root.Q<Button>("jintscripts-objects-normalize").clicked += () =>
//             //     {
//             //         var o = descriptor.EstimateJintScripts();
//             //         descriptor.JintScripts = o.Values.ToList();
//             //         jintscripts.itemsSource = descriptor.GetJintScripts();
//             //         EditorUtility.SetDirty(descriptor);
//             //     };
//             // else root.Q<Button>("jintscripts-objects-normalize").SetEnabled(false);
//             //
//             // if (!descriptor.IsCompiled)
//             //     root.Q<Button>("jintscripts-objects-detect").clicked += () =>
//             //     {
//             //         var o = new List<JintScript>();
//             //         foreach (var ro in descriptor.gameObject.scene.GetRootGameObjects())
//             //             o.AddRange(ro.GetComponentsInChildren<JintScript>());
//             //         descriptor.JintScripts = o;
//             //         jintscripts.itemsSource = descriptor.GetJintScripts();
//             //         EditorUtility.SetDirty(descriptor);
//             //     };
//             // else root.Q<Button>("jintscripts-objects-detect").SetEnabled(false);
//
//             var networkObjects = root.Q<ListView>("network-objects");
//             networkObjects.showAddRemoveFooter = !descriptor.IsCompiled;
//             networkObjects.allowAdd = !descriptor.IsCompiled;
//             networkObjects.allowRemove = !descriptor.IsCompiled;
//             networkObjects.showBoundCollectionSize = !descriptor.IsCompiled;
//             networkObjects.makeItem = () =>
//             {
//                 var item = new VisualElement
//                 {
//                     style =
//                     {
//                         paddingLeft = 2,
//                         paddingRight = 8
//                     }
//                 };
//                 var obj = new ObjectField()
//                     { objectType = typeof(NetworkObject), label = "#-", focusable = !descriptor.IsCompiled };
//                 if (!descriptor.IsCompiled)
//                     obj.RegisterValueChangedCallback(evt =>
//                     {
//                         if (item.userData is int i && i >= 0 && i < descriptor.GetNetworkObjects().Count)
//                             descriptor.GetNetworkObjects()[i] = evt.newValue as NetworkObject;
//                         EditorUtility.SetDirty(descriptor);
//                     });
//                 else obj.SetEnabled(false);
//                 item.Add(obj);
//                 return item;
//             };
//             networkObjects.bindItem = (e, i) =>
//             {
//                 e.Q<ObjectField>().label = "#" + descriptor.GetNetworkObjects()[i]?.networkId;
//                 e.Q<ObjectField>().value = descriptor.GetNetworkObjects()[i];
//                 e.userData = i;
//             };
//             networkObjects.itemsSource = descriptor.GetNetworkObjects();
//
//             if (!descriptor.IsCompiled)
//             {
//                 root.Q<Button>("network-objects-normalize").clicked += () =>
//                 {
//                     var o = descriptor.EstimateNetworkObjects();
//                     foreach (var obj in o)
//                         obj.Value.networkId = obj.Key;
//                     descriptor.NetworkObjects = o.Values.ToList();
//                     networkObjects.itemsSource = descriptor.GetNetworkObjects();
//                     EditorUtility.SetDirty(descriptor);
//                 };
//
//                 root.Q<Button>("network-objects-detect").clicked += () =>
//                 {
//                     var o = new List<NetworkObject>();
//                     foreach (var ro in descriptor.gameObject.scene.GetRootGameObjects())
//                         o.AddRange(ro.GetComponentsInChildren<NetworkObject>());
//                     descriptor.NetworkObjects = o;
//                     networkObjects.itemsSource = descriptor.GetNetworkObjects();
//                     EditorUtility.SetDirty(descriptor);
//                 };
//             }
//             else
//             {
//                 root.Q<Button>("network-objects-normalize").SetEnabled(false);
//                 root.Q<Button>("network-objects-detect").SetEnabled(false);
//             }
//
//             var spawnType = root.Q<EnumField>("spawn-type");
//             spawnType.Init(descriptor.SpawnType);
//             if (!descriptor.IsCompiled)
//                 spawnType.RegisterValueChangedCallback(evt =>
//                 {
//                     descriptor.SpawnType = (SpawnType)evt.newValue;
//                     EditorUtility.SetDirty(descriptor);
//                 });
//             else spawnType.SetEnabled(false);
//
//             var sc = AssetDatabase.LoadAssetAtPath<SceneAsset>(descriptor.gameObject.scene.path);
//             root.Q<ObjectField>("current-scene").value = sc;
//             if (!descriptor.IsCompiled)
//                 root.Q<ObjectField>("current-scene").RegisterValueChangedCallback(evt =>
//                 {
//                     if (evt.newValue == sc) return;
//                     root.Q<ObjectField>("current-scene").value = sc;
//                     EditorUtility.SetDirty(descriptor);
//                 });
//
//             else root.Q<ObjectField>("current-scene").SetEnabled(false);
//
//             var respawnHeight = root.Q<DoubleField>("respawn-height");
//             respawnHeight.value = descriptor.RespawnHeight;
//             if (!descriptor.IsCompiled)
//                 respawnHeight.RegisterValueChangedCallback(evt =>
//                 {
//                     descriptor.RespawnHeight = evt.newValue;
//                     EditorUtility.SetDirty(descriptor);
//                 });
//             else respawnHeight.SetEnabled(false);
//
//             var scenes = root.Q<ListView>("scenes");
//             scenes.showAddRemoveFooter = !descriptor.IsCompiled;
//             scenes.allowAdd = !descriptor.IsCompiled;
//             scenes.allowRemove = !descriptor.IsCompiled;
//             scenes.showBoundCollectionSize = !descriptor.IsCompiled;
//
//             scenes.makeItem = () =>
//             {
//                 var item = new VisualElement
//                 {
//                     style =
//                     {
//                         paddingLeft = 2,
//                         paddingRight = 8
//                     }
//                 };
//                 var obj = new ObjectField
//                     { objectType = typeof(SceneAsset), label = "#-", focusable = !descriptor.IsCompiled };
//                 if (!descriptor.IsCompiled)
//                     obj.RegisterValueChangedCallback(evt =>
//                     {
//                         if (item.userData is int i and >= 0 && i < descriptor.Scenes.Count)
//                             descriptor.Scenes[i] = evt.newValue as SceneAsset;
//                         EditorUtility.SetDirty(descriptor);
//                     });
//                 else obj.SetEnabled(false);
//                 item.Add(obj);
//                 return item;
//             };
//
//             scenes.bindItem = (e, i) =>
//             {
//                 e.Q<ObjectField>().label = "#" + i;
//                 var list = descriptor.GetScenes();
//                 var scene = list.Count > i ? list[i] : null;
//                 e.Q<ObjectField>().value = scene != null ? AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) : null;
//                 e.userData = i;
//             };
//
//             scenes.itemsAdded += e =>
//             {
//                 if (descriptor.IsCompiled) return;
//                 descriptor.Scenes.Add(null);
//                 scenes.itemsSource = descriptor.GetScenes();
//                 EditorUtility.SetDirty(descriptor);
//             };
//
//             scenes.itemsRemoved += e =>
//             {
//                 if (descriptor.IsCompiled) return;
//                 descriptor.Scenes = descriptor.Scenes.Where((_, i) => !e.Contains(i)).ToList();
//                 scenes.itemsSource = descriptor.GetScenes();
//                 EditorUtility.SetDirty(descriptor);
//             };
//
//             scenes.itemIndexChanged += (i, j) =>
//             {
//                 if (descriptor.IsCompiled) return;
//                 var list = descriptor.Scenes;
//                 if (i < 0 || i >= list.Count) return;
//                 if (j < 0 || j >= list.Count) return;
//                 (list[j], list[i]) = (list[i], list[j]);
//                 descriptor.Scenes = list;
//                 scenes.itemsSource = descriptor.GetScenes();
//                 EditorUtility.SetDirty(descriptor);
//             };
//
//             scenes.itemsSource = descriptor.GetScenes();
//             if (!descriptor.IsCompiled)
//                 root.Q<Button>("scenes-normalize").clicked += () =>
//                 {
//                     var o = descriptor.EstimateScenes();
//                     descriptor.Scenes = o.Values.Skip(1).ToList();
//                     scenes.itemsSource = descriptor.GetScenes();
//                     EditorUtility.SetDirty(descriptor);
//                 };
//             else root.Q<Button>("scenes-normalize").SetEnabled(false);
//
//             root.Q<Button>("open-scenes").clicked += () =>
//             {
//                 foreach (var scene in descriptor.GetScenes())
//                     EditorSceneManager.OpenScene(scene, OpenSceneMode.Additive);
//                 EditorUtility.SetDirty(descriptor);
//             };
//
//             if (!descriptor.IsCompiled)
//                 root.Q<Button>("detect-scenes").clicked += () =>
//                 {
//                     var descriptors = BaseDescriptor.GetDescriptors<BaseDescriptor>();
//                     Logger.Log("Detected " + descriptors.Length + " descriptors");
//                     var list = new List<SceneAsset>();
//                     foreach (var desc in descriptors)
//                         if (desc is SubDescriptor mD)
//                         {
//                             var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(mD.gameObject.scene.path);
//                             if (scene && !list.Contains(scene))
//                                 list.Add(scene);
//                         }
//
//                     descriptor.Scenes = list;
//                     scenes.itemsSource = descriptor.GetScenes();
//                     EditorUtility.SetDirty(descriptor);
//                 };
//             else root.Q<Button>("detect-scenes").SetEnabled(false);
//
//             var features = root.Q<ListView>("features");
//             features.showAddRemoveFooter = !descriptor.IsCompiled;
//             features.allowAdd = !descriptor.IsCompiled;
//             features.allowRemove = !descriptor.IsCompiled;
//             features.showBoundCollectionSize = !descriptor.IsCompiled;
//             features.makeItem = () =>
//             {
//                 var item = new VisualElement
//                 {
//                     style =
//                     {
//                         paddingLeft = 2,
//                         paddingRight = 8
//                     }
//                 };
//                 var obj = new TextField() { label = "#-", focusable = !descriptor.IsCompiled };
//                 if (!descriptor.IsCompiled)
//                     obj.RegisterValueChangedCallback(evt =>
//                     {
//                         if (item.userData is int i and >= 0 && i < descriptor.Features.Count)
//                             descriptor.Features[i] = evt.newValue;
//                         EditorUtility.SetDirty(descriptor);
//                     });
//                 else obj.SetEnabled(false);
//                 item.Add(obj);
//                 return item;
//             };
//
//             features.bindItem = (e, i) =>
//             {
//                 e.Q<TextField>().label = "#" + i;
//                 e.Q<TextField>().value = descriptor.Features[i];
//                 e.userData = i;
//             };
//
//             features.itemsSource = descriptor.GetFeatures();
//
//             if (!descriptor.IsCompiled)
//                 root.Q<Button>("features-normalize").clicked += () =>
//                 {
//                     var o = descriptor.EstimateFeatures();
//                     descriptor.Features = o.Values.ToList();
//                     features.itemsSource = descriptor.GetFeatures();
//                     EditorUtility.SetDirty(descriptor);
//                 };
//             else root.Q<Button>("features-normalize").SetEnabled(false);
//
//             var mods = root.Q<ListView>("mods");
//             mods.showAddRemoveFooter = !descriptor.IsCompiled;
//             mods.allowAdd = !descriptor.IsCompiled;
//             mods.allowRemove = !descriptor.IsCompiled;
//             mods.showBoundCollectionSize = !descriptor.IsCompiled;
//
//             mods.makeItem = () =>
//             {
//                 var item = new VisualElement
//                 {
//                     style =
//                     {
//                         paddingLeft = 2,
//                         paddingRight = 8
//                     }
//                 };
//                 var obj = new TextField() { label = "#-", focusable = !descriptor.IsCompiled };
//                 if (!descriptor.IsCompiled)
//                     obj.RegisterValueChangedCallback(evt =>
//                     {
//                         if (item.userData is not (int i and >= 0) || i >= descriptor.ModRequirements.Count) return;
//                         Logger.Log("Changed mod" + item.userData + " (value) to " + evt.newValue);
//                         descriptor.ModRequirements[i].Id = evt.newValue;
//                         EditorUtility.SetDirty(descriptor);
//                     });
//                 else obj.SetEnabled(false);
//                 item.Add(obj);
//                 var e = new EnumField() { label = "Flags", focusable = !descriptor.IsCompiled };
//                 if (!descriptor.IsCompiled)
//                     e.RegisterValueChangedCallback(evt =>
//                     {
//                         if (item.userData is not int i || i < 0 || i >= descriptor.ModRequirements.Count) return;
//                         Logger.Log("Changed mod " + item.userData + " (flag) to " + evt.newValue);
//                         descriptor.ModRequirements[i].Flags = (ModRequirmentFlags)evt.newValue;
//                         EditorUtility.SetDirty(descriptor);
//                     });
//                 else e.SetEnabled(false);
//                 item.Add(e);
//                 return item;
//             };
//
//             mods.bindItem = (e, i) =>
//             {
//                 var mod = descriptor.GetRequirementMods()[i];
//                 e.Q<TextField>().label = "#" + i;
//                 e.Q<TextField>().value = mod?.Id ?? "";
//                 e.Q<EnumField>().Init(mod?.Flags ?? ModRequirmentFlags.None);
//                 e.userData = i;
//             };
//
//             mods.itemsAdded += e =>
//             {
//                 if (descriptor.IsCompiled) return;
//                 descriptor.ModRequirements.Add(new ModRequirement()
//                 {
//                     Id = "",
//                     Flags = ModRequirmentFlags.None
//                 });
//                 mods.itemsSource = descriptor.GetRequirementMods();
//                 EditorUtility.SetDirty(descriptor);
//             };
//
//             mods.itemsRemoved += e =>
//             {
//                 if (descriptor.IsCompiled) return;
//                 descriptor.ModRequirements = descriptor.ModRequirements.Where((_, i) => !e.Contains(i)).ToList();
//                 mods.itemsSource = descriptor.GetRequirementMods();
//                 EditorUtility.SetDirty(descriptor);
//             };
//
//             mods.itemIndexChanged += (i, j) =>
//             {
//                 if (descriptor.IsCompiled) return;
//                 var m = descriptor.ModRequirements;
//                 if (i < 0 || i >= m.Count) return;
//                 if (j < 0 || j >= m.Count) return;
//                 (m[j], m[i]) = (m[i], m[j]);
//                 mods.itemsSource = descriptor.GetRequirementMods();
//                 EditorUtility.SetDirty(descriptor);
//             };
//
//             if (!descriptor.IsCompiled)
//                 root.Q<Button>("mods-normalize").clicked += () =>
//                 {
//                     var o = descriptor.EstimateMods();
//                     descriptor.ModRequirements = o.Values.ToList();
//                     mods.itemsSource = descriptor.GetRequirementMods();
//                     EditorUtility.SetDirty(descriptor);
//                 };
//             else root.Q<Button>("mods-normalize").SetEnabled(false);
//
//             mods.itemsSource = descriptor.GetRequirementMods();
//
//
//             var walkSpeed = root.Q<DoubleField>("walk-speed");
//             walkSpeed.value = descriptor.WalkSpeed;
//             if (!descriptor.IsCompiled)
//                 walkSpeed.RegisterValueChangedCallback(evt =>
//                 {
//                     if (evt.newValue < 0) walkSpeed.value = descriptor.WalkSpeed;
//                     else descriptor.WalkSpeed = evt.newValue;
//                     EditorUtility.SetDirty(descriptor);
//                 });
//             else walkSpeed.SetEnabled(false);
//
//             var sprintSpeed = root.Q<DoubleField>("sprint-speed");
//             sprintSpeed.value = descriptor.SprintSpeed;
//             if (!descriptor.IsCompiled)
//                 sprintSpeed.RegisterValueChangedCallback(evt =>
//                 {
//                     if (evt.newValue < 0) sprintSpeed.value = descriptor.SprintSpeed;
//                     else descriptor.SprintSpeed = evt.newValue;
//                     EditorUtility.SetDirty(descriptor);
//                 });
//             else sprintSpeed.SetEnabled(false);
//
//             var crouchSpeed = root.Q<DoubleField>("crounch-speed");
//             crouchSpeed.value = descriptor.CrouchSpeed;
//             if (!descriptor.IsCompiled)
//                 crouchSpeed.RegisterValueChangedCallback(evt =>
//                 {
//                     if (evt.newValue < 0) crouchSpeed.value = descriptor.CrouchSpeed;
//                     else descriptor.CrouchSpeed = evt.newValue;
//                     EditorUtility.SetDirty(descriptor);
//                 });
//             else crouchSpeed.SetEnabled(false);
//
//             var flySpeed = root.Q<DoubleField>("fly-speed");
//             flySpeed.value = descriptor.FlySpeed;
//             if (!descriptor.IsCompiled)
//                 flySpeed.RegisterValueChangedCallback(evt =>
//                 {
//                     if (evt.newValue < 0) flySpeed.value = descriptor.FlySpeed;
//                     else descriptor.FlySpeed = evt.newValue;
//                     EditorUtility.SetDirty(descriptor);
//                 });
//             else flySpeed.SetEnabled(false);
//
//             var flyOnSpawn = root.Q<Toggle>("fly-on-spawn");
//             flyOnSpawn.value = descriptor.FlyOnSpawn;
//             if (!descriptor.IsCompiled)
//                 flyOnSpawn.RegisterValueChangedCallback(evt =>
//                 {
//                     descriptor.FlyOnSpawn = evt.newValue;
//                     EditorUtility.SetDirty(descriptor);
//                 });
//             else flyOnSpawn.SetEnabled(false);
//
//             var showingDistance = root.Q<DoubleField>("showing-distance");
//             showingDistance.value = descriptor.ShowingDistance;
//             if (!descriptor.IsCompiled)
//                 showingDistance.RegisterValueChangedCallback(evt =>
//                 {
//                     if (evt.newValue < 0) showingDistance.value = descriptor.ShowingDistance;
//                     else descriptor.ShowingDistance = evt.newValue;
//                     EditorUtility.SetDirty(descriptor);
//                 });
//             else showingDistance.SetEnabled(false);
//
//             var showingType = root.Q<EnumField>("showing-type");
//             showingType.Init(descriptor.ShowingOnJoin);
//             if (!descriptor.IsCompiled)
//                 showingType.RegisterValueChangedCallback(evt =>
//                 {
//                     descriptor.ShowingOnJoin = (ShowingType)evt.newValue;
//                     EditorUtility.SetDirty(descriptor);
//                 });
//             else showingType.SetEnabled(false);
//
//             return root;
//         }
//
//
//         private const BuildTarget BuildTarget = UnityEditor.BuildTarget.NoTarget;
//         public static bool IsBuilding { get; private set; }
//
//         private static BuildResult EndBuild(string path, string message, bool dialog, SceneSetup[] currentScenes)
//         {
//             IsBuilding = false;
//             if (dialog && !string.IsNullOrEmpty(message))
//                 EditorUtility.DisplayDialog("Nox Build Error", message, "OK");
//             if (currentScenes.Length > 0)
//                 EditorSceneManager.RestoreSceneManagerSetup(currentScenes);
//             return new BuildResult
//             {
//                 Success = string.IsNullOrEmpty(message),
//                 ErrorMessage = message,
//                 path = path
//             };
//         }
//
//         [MenuItem("Nox/CCK/World/Build")]
//         public static void BuildWorld()
//         {
//             BuildWorld(null);
//         }
//
//         public static BuildResult BuildWorld(MainDescriptor descriptor = null,
//             BuildTarget buildTarget = BuildTarget.NoTarget, bool dialog = true)
//         {
//             IsBuilding = true;
//             if (EditorApplication.isCompiling)
//                 return EndBuild(null, "Cannot build while compiling", dialog, Array.Empty<SceneSetup>());
//             if (EditorApplication.isPlaying)
//                 return EndBuild(null, "Cannot build while playing", dialog, Array.Empty<SceneSetup>());
//
//             if (buildTarget == BuildTarget.NoTarget)
//                 buildTarget = BuildTarget;
//             if (buildTarget == BuildTarget.NoTarget)
//                 return EndBuild(null, "No build target selected", dialog, Array.Empty<SceneSetup>());
//
//             var currentScene = SceneManager.GetActiveScene();
//             if (!descriptor) descriptor = GetWorldDescriptors(dialog).FirstOrDefault();
//             if (!descriptor) return EndBuild(null, "No World Descriptor Found", dialog, Array.Empty<SceneSetup>());
//
//             if (!EditorSceneManager.SaveScene(currentScene))
//                 return EndBuild(null, "Failed to save current scene", dialog, Array.Empty<SceneSetup>());
//
//             string assetBundleDirectory = "Assets/Output/";
//             if (!Directory.Exists(assetBundleDirectory))
//                 Directory.CreateDirectory(assetBundleDirectory);
//             string buildBundleDirectory = "Assets/BuildOutput/";
//             if (!Directory.Exists(buildBundleDirectory))
//                 Directory.CreateDirectory(buildBundleDirectory);
//
//             string buildId = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + '-' + currentScene.name.ToLower();
//             var pathBuild = buildBundleDirectory + buildId + "/";
//             if (!Directory.Exists(pathBuild))
//                 Directory.CreateDirectory(pathBuild);
//             var dependencyPath = pathBuild + "Dependencies/";
//             if (!Directory.Exists(dependencyPath))
//                 Directory.CreateDirectory(dependencyPath);
//
//             if (!EditorSceneManager.SaveOpenScenes())
//                 return EndBuild(null, "Failed to save all scenes", dialog, Array.Empty<SceneSetup>());
//
//             var currentScenes = EditorSceneManager.GetSceneManagerSetup();
//
//             var scenesSet = descriptor.EstimateScenes();
//             var assets = new List<string>();
//             var scenes = new List<string>();
//             var initialGUIDs = new Dictionary<string, string>();
//             var endGUIDs = new Dictionary<string, string>();
//
//             foreach (var scene in scenesSet)
//             {
//                 var path = AssetDatabase.GetAssetPath(scene.Value);
//                 var destination = pathBuild + scene.Key + ".unity";
//                 if (string.IsNullOrEmpty(path) || !File.Exists(path))
//                     return EndBuild(null, "Scene not found: " + scene.Value.name, dialog, currentScenes);
//                 File.Copy(path, destination);
//                 File.Copy(path + ".meta", destination + ".meta");
//                 Logger.Log("Copied " + path + " to " + destination);
//                 assets.Add(destination);
//                 scenes.Add(destination);
//                 initialGUIDs.Add(destination, AssetDatabase.AssetPathToGUID(path));
//                 endGUIDs.Add(destination, Guid.NewGuid().ToString("N"));
//                 foreach (var dependency in AssetDatabase.GetDependencies(path))
//                 {
//                     var gui = Guid.NewGuid().ToString("N");
//                     var destinationPath = pathBuild + "Dependencies/" + gui + Path.GetExtension(dependency);
//                     if (assets.Contains(destinationPath)) continue;
//                     switch (Path.GetExtension(dependency))
//                     {
//                         case ".cs":
//                         case ".dll":
//                         case ".meta":
//                         case ".unity":
//                             continue;
//                     }
//
//                     File.Copy(dependency, destinationPath);
//                     File.Copy(dependency + ".meta", destinationPath + ".meta");
//                     Logger.Log("Copied " + dependency + " to " + destinationPath);
//                     assets.Add(destinationPath);
//                     initialGUIDs.Add(destinationPath, AssetDatabase.AssetPathToGUID(dependency));
//                     endGUIDs.Add(destinationPath, gui);
//                 }
//             }
//
//             foreach (var asset in assets)
//             {
//                 if (new[]
//                     {
//                         ".unity", ".prefab", ".asset",
//                         ".mat", ".anim", ".controller"
//                     }.Contains(Path.GetExtension(asset)))
//                 {
//                     var text = File.ReadAllText(asset);
//                     text = initialGUIDs.Aggregate(text,
//                         (current, guid) => new Regex(guid.Value).Replace(current, endGUIDs[guid.Key]));
//                     File.WriteAllText(asset, text);
//                 }
//
//                 var meta = File.ReadAllText(asset + ".meta");
//                 meta = initialGUIDs.Aggregate(meta,
//                     (current, guid) => new Regex(guid.Value).Replace(current, endGUIDs[guid.Key]));
//                 File.WriteAllText(asset + ".meta", meta);
//             }
//
//             AssetDatabase.Refresh();
//
//             // get all new uids
//             var newGUIDs = new Dictionary<string, string>();
//             foreach (var asset in assets)
//                 if (asset.StartsWith(dependencyPath))
//                 {
//                     var fileName = Path.GetFileNameWithoutExtension(asset);
//                     newGUIDs.Add(fileName, AssetDatabase.AssetPathToGUID(asset));
//                     Logger.Log("Dependency: " + fileName + " -> " + newGUIDs[fileName]);
//                 }
//
//             // set updated uids
//             foreach (var asset in assets)
//             {
//                 if (!new[]
//                     {
//                         ".unity", ".prefab", ".asset",
//                         ".mat", ".anim", ".controller"
//                     }.Contains(Path.GetExtension(asset))) continue;
//                 var text = File.ReadAllText(asset);
//                 text = newGUIDs.Aggregate(text, (current, guid) => new Regex(guid.Key).Replace(current, guid.Value));
//                 File.WriteAllText(asset, text);
//             }
//
//             AssetDatabase.Refresh();
//
//             var loadedScenes = new List<Scene>() { EditorSceneManager.OpenScene(scenes[0], OpenSceneMode.Single) };
//             loadedScenes.AddRange(scenes.Skip(1)
//                 .Select(scene => EditorSceneManager.OpenScene(scene, OpenSceneMode.Additive)));
//
//             MainDescriptor mD = null;
//             foreach (var root in loadedScenes[0].GetRootGameObjects())
//             {
//                 mD = root.GetComponentInChildren<MainDescriptor>();
//                 if (mD) break;
//             }
//
//             List<SubDescriptor> sDs = new();
//             foreach (var scene in loadedScenes.Skip(1))
//             {
//                 SubDescriptor sD = null;
//                 foreach (var root in scene.GetRootGameObjects())
//                 {
//                     sD = root.GetComponentInChildren<SubDescriptor>();
//                     if (sD) break;
//                 }
//
//                 if (sD) sDs.Add(sD);
//                 else return EndBuild(null, "Sub Descriptor not found in scene: " + scene.name, dialog, currentScenes);
//             }
//
//             // compile all descriptors
//             foreach (var desc in new BaseDescriptor[] { mD }.Concat(sDs))
//             {
//                 Logger.Log("Compiling " + desc.name + " in " + desc.gameObject.scene.name);
//                 desc.Compile();
//             }
//
//             // get all scripts
//             // List<JintScript> jSs = new();
//             // foreach (var scene in loadedScenes)
//             // foreach (var root in scene.GetRootGameObjects())
//             // {
//             //     var jS = root.GetComponentInChildren<JintScript>();
//             //     if (jS) jSs.Add(jS);
//             // }
//             //
//             // // compile all scripts
//             // foreach (var script in jSs)
//             // {
//             //     Logger.Log("Compiling " + script.name + " in " + script.gameObject.scene.name);
//             //     script.Compile();
//             // }
//
//
//             // save all scenes and await
//             EditorSceneManager.SaveOpenScenes();
//             AssetDatabase.Refresh();
//
//             foreach (var scene in loadedScenes)
//                 EditorSceneManager.CloseScene(scene, false);
//
//             _lastbuild_scene = scenes.ToArray();
//             _lastbuild_id = buildId;
//             _lastbuild_directory = pathBuild;
//             _lastbuild_target = (BuildTarget)buildTarget;
//
//             if (!Compile(scenes.ToArray(), buildId, assetBundleDirectory, (BuildTarget)buildTarget))
//                 return EndBuild(assetBundleDirectory + buildId + ".noxw", "Failed to compile asset bundle", dialog,
//                     currentScenes);
//
//             if (!dialog) return EndBuild(assetBundleDirectory + buildId + ".noxw", null, dialog, currentScenes);
//             EditorUtility.DisplayDialog("Nox Build Success", "Build successful", "OK");
//             EditorUtility.RevealInFinder(assetBundleDirectory);
//             return EndBuild(assetBundleDirectory + buildId + ".noxw", null, dialog, currentScenes);
//         }
//
//         private static string[] _lastbuild_scene;
//         private static string _lastbuild_id;
//         private static string _lastbuild_directory;
//         private static BuildTarget _lastbuild_target;
//
//         [MenuItem("Nox/CCK/World/Get Descriptor")]
//         public static void GetMenuWorldDescriptor()
//         {
//             var descriptors = GetWorldDescriptors();
//             if (descriptors.Length == 0) return;
//             Selection.activeObject = descriptors[0];
//         }
//
//         public static MainDescriptor[] GetWorldDescriptors(bool dialog = true)
//         {
//             var currentScene = SceneManager.GetActiveScene();
//             var descriptors = currentScene.GetRootGameObjects()
//                 .SelectMany(root => root.GetComponentsInChildren<MainDescriptor>()).ToArray();
//             switch (descriptors.Length)
//             {
//                 case 0 when dialog:
//                     EditorUtility.DisplayDialog("No World Descriptor Found",
//                         "No World Descriptor was found in the current scene.", "OK");
//                     break;
//                 case > 1 when dialog:
//                     EditorUtility.DisplayDialog("Multiple Descriptors Found",
//                         "Multiple World Descriptors were found in the current scene.", "OK");
//                     break;
//             }
//
//             if (!dialog) return descriptors;
//             foreach (var descriptor in descriptors)
//                 Selection.activeObject = descriptor;
//             return descriptors;
//         }
//
//         [MenuItem("Nox/CCK/World/Build Last")]
//         public static void BuildLast()
//         {
//             if (_lastbuild_scene == null || _lastbuild_scene.Length == 0)
//             {
//                 EditorUtility.DisplayDialog("No Last Build", "No last build was found.", "OK");
//                 return;
//             }
//
//             if (!Compile(_lastbuild_scene, _lastbuild_id, "Assets/Output/", _lastbuild_target))
//             {
//                 EditorUtility.DisplayDialog("Failed to compile asset bundle", "Failed to compile asset bundle", "OK");
//                 return;
//             }
//
//             EditorUtility.DisplayDialog("Nox Build Success", "Build successful", "OK");
//             EditorUtility.RevealInFinder(_lastbuild_directory);
//         }
//
//         private static bool Compile(string[] scenes, string buildId, string assetBundleDirectory,
//             BuildTarget buildTarget)
//         {
//             var assetSet = new HashSet<string>();
//             foreach (var scene in scenes)
//             foreach (var dependency in AssetDatabase.GetDependencies(scene))
//             {
//                 if (Path.GetExtension(dependency) == ".cs") continue;
//                 if (Path.GetExtension(dependency) == ".dll") continue;
//                 if (Path.GetExtension(dependency) == ".meta") continue;
//                 if (Path.GetExtension(dependency) == ".unity") continue;
//                 assetSet.Add(dependency);
//             }
//
//             AssetBundleBuild definition = new()
//             {
//                 assetBundleName = buildId + ".noxw",
//                 assetNames = scenes.ToArray(),
//                 addressableNames = assetSet.ToArray()
//             };
//
//             BuildAssetBundlesParameters input = new()
//             {
//                 outputPath = assetBundleDirectory,
//                 targetPlatform = buildTarget,
//
//                 options = BuildAssetBundleOptions.None,
//                 bundleDefinitions = new AssetBundleBuild[] { definition }
//             };
//
//             Logger.Log("Building Asset Bundle: " + input.outputPath);
//             foreach (var scene in input.bundleDefinitions)
//             {
//                 Logger.Log("Scene: " + scene.assetBundleName);
//                 foreach (var asset in scene.assetNames)
//                     Logger.Log("Asset: " + asset);
//                 foreach (var asset in scene.addressableNames)
//                     Logger.Log("Addressable: " + asset);
//             }
//
//
//             AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(input);
//
//             return manifest != null;
//         }
//
//         // check BuildTarget buildTarget is disponible
//         public static bool IsBuildTargetSupported(BuildTarget buildTarget)
//         {
//             return BuildPipeline.IsBuildTargetSupported(
//                 BuildPipeline.GetBuildTargetGroup(buildTarget),
//                 buildTarget
//             );
//         }
//
//         [MenuItem("Nox/CCK/World/Make Descriptor")]
//         public static MainDescriptor MakeWorldDescriptor()
//         {
//             var root = new GameObject("World Descriptor");
//             var descriptor = root.AddComponent<MainDescriptor>();
//             Selection.activeObject = descriptor;
//             return descriptor;
//         }
//     }
//
//     public class BuildResult
//     {
//         public bool Success;
//         public string ErrorMessage;
//         public string path;
//     }
// }
// #endif