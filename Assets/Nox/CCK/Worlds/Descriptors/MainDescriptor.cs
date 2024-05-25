#if UNITY_EDITOR
using UnityEditor;
#endif

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace Nox.CCK.Worlds
{
    [SerializeField]
    public class MainDescriptor : BaseDescriptor
    {

        [SerializeField] private string[] data_Scenes;
        [SerializeField] private string[] data_Features;
        [SerializeField] private ModRequirement[] data_Mods;


#if UNITY_EDITOR
        [SerializeField] public List<SceneAsset> Scenes = new();
        [SerializeField] public List<string> Features = new();
        [SerializeField] public List<ModRequirement> ModRequirements = new();
        [SerializeField] private SuppordBuildTarget _target = SuppordBuildTarget.NoTarget;
        [SerializeField] public string ServerPublisher = null;
        [SerializeField] public uint IdPublisher = 0;
        [SerializeField] public ushort VersionPublisher = 0;

        public SuppordBuildTarget target
        {
            get => _target == SuppordBuildTarget.NoTarget ? SuppordTarget.GetCurrentTarget() : _target;
            set
            {
                _target = value;
                EditorUtility.SetDirty(this);
            }
        }


        public override void Compile()
        {
            base.Compile();
            var sceneSet = EstimateScenes();
            Scenes = sceneSet.Values.ToList();
            var lscenes = new List<string>();
            foreach (var scene in Scenes)
                lscenes.Add(AssetDatabase.GetAssetPath(scene));
            data_Scenes = lscenes.ToArray();
            data_Type = DescriptorType.Main;
            EditorUtility.SetDirty(this);
        }

        public Dictionary<byte, SceneAsset> EstimateScenes()
        {
            var sceneSet = new HashSet<SceneAsset> { AssetDatabase.LoadAssetAtPath<SceneAsset>(gameObject.scene.path) };
            foreach (var scenePath in Scenes)
                if (scenePath != null && !sceneSet.Contains(scenePath))
                    sceneSet.Add(scenePath);
            var scenesDict = new Dictionary<byte, SceneAsset>();
            for (byte i = 0; i < sceneSet.Count; i++)
            {
                byte estimatedId = i;
                while (scenesDict.ContainsKey(estimatedId)) estimatedId++;
                scenesDict.Add(estimatedId, sceneSet.ToArray()[i]);
            }
            return scenesDict;
        }

        public Dictionary<ushort, string> EstimateFeatures()
        {
            var featureSet = new HashSet<string>();
            foreach (var feature in Features)
                if (!string.IsNullOrWhiteSpace(feature) && !featureSet.Contains(feature))
                    featureSet.Add(feature.Trim());
            var featuresDict = new Dictionary<ushort, string>();
            for (ushort i = 0; i < featureSet.Count; i++)
            {
                ushort estimatedId = i;
                while (featuresDict.ContainsKey(estimatedId)) estimatedId++;
                featuresDict.Add(estimatedId, featureSet.ToArray()[i]);
            }
            return featuresDict;
        }

        public Dictionary<ushort, ModRequirement> EstimateMods()
        {
            var modSet = new HashSet<ModRequirement>();
            foreach (var mod in ModRequirements)
                if (mod != null && !modSet.Contains(mod))
                    modSet.Add(mod);
            var modsDict = new Dictionary<ushort, ModRequirement>();
            for (ushort i = 0; i < modSet.Count; i++)
            {
                ushort estimatedId = i;
                while (modsDict.ContainsKey(estimatedId)) estimatedId++;
                modsDict.Add(estimatedId, modSet.ToArray()[i]);
            }
            return modsDict;
        }

        public List<string> GetScenes()
        {
            if (IsCompiled) return (data_Scenes ?? new string[0]).ToList();
            return Scenes.Select(scene => AssetDatabase.GetAssetPath(scene)).ToList();
        }

        public List<string> GetFeatures()
        {
            if (IsCompiled) return (data_Features ?? new string[0]).ToList();
            return Features;
        }

        public List<ModRequirement> GetMods()
        {
            if (IsCompiled) return (data_Mods ?? new ModRequirement[0]).ToList();
            return ModRequirements;
        }

        public SuppordBuildTarget GetBuildPlatform() => target;
#else
        public List<string> GetScenes() => ( data_Scenes ?? new string[0]).ToList();
        public List<string> GetFeatures() => ( data_Features ?? new string[0]).ToList();
        public List<ModRequirement> GetMods() => ( data_Mods ?? new ModRequirement[0]).ToList();
#endif
    }


    [Serializable]
    public class ModRequirement
    {
        public string Id;
        public uint Version;
        public ModRequirmentFlags Flags;
    }

    [Flags]
    public enum ModRequirmentFlags
    {
        None = 0,
        IsRequired = 1,
        CustomVersion = 2
    }

}