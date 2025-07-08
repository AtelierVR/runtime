// #if UNITY_EDITOR
// using UnityEditor;
// #endif
//
// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
// using System;
// using Nox.CCK.Utils;
//
// namespace Nox.CCK.Worlds
// {
//     [SerializeField]
//     public class MainDescriptor : BaseDescriptor
//     {
//         [SerializeField] private string[] data_Scenes;
//         [SerializeField] private string[] data_Features;
//         [SerializeField] private string[] data_Mods;
//         [SerializeField] private double data_crouchSpeed;
//         [SerializeField] private double data_walkSpeed;
//         [SerializeField] private double data_SprintSpeed;
//         [SerializeField] private double data_jumpForce;
//         [SerializeField] private double data_flySpeed;
//         [SerializeField] private double data_gravity;
//         [SerializeField] private ShowingType data_showingOnJoin;
//         [SerializeField] private double data_showingDistance;
//
//
// #if UNITY_EDITOR
//         [SerializeField] public List<SceneAsset> Scenes = new();
//         [SerializeField] public List<string> Features = new();
//         [SerializeField] public List<ModRequirement> ModRequirements = new();
//         [SerializeField] private Platform _target = Platform.None;
//         [SerializeField] public string ServerPublisher = null;
//         [SerializeField] public uint IdPublisher = 0;
//         [SerializeField] public ushort VersionPublisher = 0;
//
//         public Platform Target
//         {
//             get => _target == Platform.None ? PlatformExtensions.GetCurrentTarget().GetPlatform() : _target;
//             set
//             {
//                 _target = value;
//                 EditorUtility.SetDirty(this);
//             }
//         }
//
//
//         public override void Compile()
//         {
//             base.Compile();
//             var sceneSet = EstimateScenes();
//             Scenes = sceneSet.Values.ToList();
//             var lscenes = new List<string>();
//             foreach (var scene in Scenes)
//                 lscenes.Add(AssetDatabase.GetAssetPath(scene));
//             data_Scenes = lscenes.ToArray();
//             data_Type = DescriptorType.Main;
//             data_Features = EstimateFeatures().Values.ToArray();
//             data_Mods = EstimateMods().Values.Select(mod => mod.Id).ToArray();
//             EditorUtility.SetDirty(this);
//         }
//
//         public Dictionary<byte, SceneAsset> EstimateScenes()
//         {
//             var sceneSet = new HashSet<SceneAsset> { AssetDatabase.LoadAssetAtPath<SceneAsset>(gameObject.scene.path) };
//             foreach (var scenePath in Scenes)
//                 if (scenePath != null && !sceneSet.Contains(scenePath))
//                     sceneSet.Add(scenePath);
//             var scenesDict = new Dictionary<byte, SceneAsset>();
//             for (byte i = 0; i < sceneSet.Count; i++)
//             {
//                 byte estimatedId = i;
//                 while (scenesDict.ContainsKey(estimatedId)) estimatedId++;
//                 scenesDict.Add(estimatedId, sceneSet.ToArray()[i]);
//             }
//
//             return scenesDict;
//         }
//
//         public Dictionary<ushort, string> EstimateFeatures()
//         {
//             var featureSet = new HashSet<string>();
//             foreach (var feature in Features)
//                 if (!string.IsNullOrWhiteSpace(feature) && !featureSet.Contains(feature))
//                     featureSet.Add(feature.Trim());
//             var featuresDict = new Dictionary<ushort, string>();
//             for (ushort i = 0; i < featureSet.Count; i++)
//             {
//                 ushort estimatedId = i;
//                 while (featuresDict.ContainsKey(estimatedId)) estimatedId++;
//                 featuresDict.Add(estimatedId, featureSet.ToArray()[i]);
//             }
//
//             return featuresDict;
//         }
//
//         public Dictionary<ushort, ModRequirement> EstimateMods()
//         {
//             var modSet = new HashSet<ModRequirement>();
//             foreach (var mod in ModRequirements)
//                 if (mod != null && !modSet.Contains(mod))
//                     modSet.Add(mod);
//             var modsDict = new Dictionary<ushort, ModRequirement>();
//             for (ushort i = 0; i < modSet.Count; i++)
//             {
//                 ushort estimatedId = i;
//                 while (modsDict.ContainsKey(estimatedId)) estimatedId++;
//                 modsDict.Add(estimatedId, modSet.ToArray()[i]);
//             }
//
//             return modsDict;
//         }
//
//         public List<string> GetScenes()
//         {
//             if (IsCompiled) return (data_Scenes ?? new string[0]).ToList();
//             return Scenes.Select(scene => AssetDatabase.GetAssetPath(scene)).ToList();
//         }
//
//         public List<string> GetFeatures()
//         {
//             if (IsCompiled) return (data_Features ?? new string[0]).ToList();
//             return Features;
//         }
//
//         public List<ModRequirement> GetRequirementMods()
//         {
//             if (IsCompiled)
//                 return (data_Mods?.Select(s => new ModRequirement { Id = s }) ?? new ModRequirement[0]).ToList();
//             return ModRequirements;
//         }
//
//         public List<string> GetMods()
//         {
//             if (IsCompiled) return (data_Mods ?? new string[0]).ToList();
//             return ModRequirements.Select(mod => mod?.Id).ToList();
//         }
//
//         public Platform GetBuildPlatform() => Target;
//
//         public double GetCrouchSpeed() => CrouchSpeed;
//
//         public double CrouchSpeed
//         {
//             get => data_crouchSpeed < 0 ? 0 : data_crouchSpeed;
//             set
//             {
//                 if (IsCompiled || data_crouchSpeed < 0) return;
//                 data_crouchSpeed = value;
//             }
//         }
//
//         public double GetWalkSpeed() => WalkSpeed;
//
//         public double WalkSpeed
//         {
//             get => data_walkSpeed < 0 ? 0 : data_walkSpeed;
//             set
//             {
//                 if (IsCompiled || data_walkSpeed < 0) return;
//                 data_walkSpeed = value;
//             }
//         }
//
//         public double GetSprintSpeed() => SprintSpeed;
//
//         public double SprintSpeed
//         {
//             get => data_SprintSpeed < 0 ? 0 : data_SprintSpeed;
//             set
//             {
//                 if (IsCompiled || data_SprintSpeed < 0) return;
//                 data_SprintSpeed = value;
//             }
//         }
//
//         public double GetJumpForce() => JumpForce;
//
//         public double JumpForce
//         {
//             get => data_jumpForce < 0 ? 0 : data_jumpForce;
//             set
//             {
//                 if (IsCompiled || data_jumpForce < 0) return;
//                 data_jumpForce = value;
//             }
//         }
//
//         public double GetFlySpeed() => FlySpeed;
//
//         public double FlySpeed
//         {
//             get => data_flySpeed < 0 ? 0 : data_flySpeed;
//             set
//             {
//                 if (IsCompiled || data_flySpeed < 0) return;
//                 data_flySpeed = value;
//             }
//         }
//
//         public double GetGravity() => Gravity;
//
//         public double Gravity
//         {
//             get => data_gravity;
//             set
//             {
//                 if (IsCompiled) return;
//                 data_gravity = value;
//             }
//         }
//
//         public ShowingType GetShowingOnJoin() => ShowingOnJoin;
//         public ShowingType ShowingOnJoin
//         {
//             get => data_showingOnJoin;
//             set
//             {
//                 if (IsCompiled) return;
//                 data_showingOnJoin = value;
//             }
//         }
//         
//         public double GetShowingDistance() => ShowingDistance;
//         public double ShowingDistance
//         {
//             get => data_showingDistance;
//             set
//             {
//                 if (IsCompiled) return;
//                 data_showingDistance = value;
//             }
//         }
// #else
//         public List<string> GetScenes() => ( data_Scenes ?? new string[0]).ToList();
//         public List<string> GetFeatures() => ( data_Features ?? new string[0]).ToList();
//         public List<string> GetMods() => ( data_Mods ?? new string[0]).ToList();
//         public double GetCrouchSpeed() => data_crouchSpeed < 0 ? 0 : data_crouchSpeed;
//         public double GetWalkSpeed() => data_walkSpeed < 0 ? 0 : data_walkSpeed;
//         public double GetSprintSpeed() => data_SprintSpeed < 0 ? 0 : data_SprintSpeed;
//         public double GetJumpForce() => data_jumpForce < 0 ? 0 : data_jumpForce;
//         public double GetFlySpeed() => data_flySpeed < 0 ? 0 : data_flySpeed;
//         public ShowingType GetShowingOnJoin() => data_showingOnJoin;
//         public double GetGravity() => data_gravity;
//         public double GetShowingDistance() => data_showingDistance;
// #endif
//     }
//
//
//     [Serializable]
//     public class ModRequirement
//     {
//         public string Id;
//         public ModRequirmentFlags Flags;
//     }
//
//     [Flags]
//     public enum ModRequirmentFlags
//     {
//         None = 0,
//         IsRequiredForMaster = 2,
//         IsRequiredForBot = 4,
//         IsRequiredForPlayer = 8,
//         IsRequiredForAll = 14
//     }
//
//     public enum ShowingType
//     {
//         Default = 0,
//         ForceHide = 1,
//         ForceShow = 2
//     }
// }