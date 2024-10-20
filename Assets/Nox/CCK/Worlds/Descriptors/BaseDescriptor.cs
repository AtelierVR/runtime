
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace Nox.CCK.Worlds
{
    [SerializeField]
    public abstract class BaseDescriptor : MonoBehaviour
    {
        [SerializeField] private SpawnType data_SpawnType;
        [SerializeField] private NetworkObject[] data_NetworkObjects;
        [SerializeField] private GameObject[] data_Spawns;
        [SerializeField] private double data_RespawnHeight;
        [SerializeField] protected DescriptorType data_Type = DescriptorType.None;
        [SerializeField] private AudioChannelDescriptor[] data_audios;

        public static BaseDescriptor[] GetDescriptors()
        {
            var descriptors = new List<BaseDescriptor>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                var descriptor = GetDescriptor(scene);
                if (descriptor != null) descriptors.Add(descriptor);
            }
            return descriptors.ToArray();
        }

        public static BaseDescriptor GetDescriptor(Scene scene)
        {
            BaseDescriptor descriptor = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                descriptor = root.GetComponentInChildren<BaseDescriptor>();
                if (descriptor != null) break;
            }
            return descriptor;
        }

        public static AudioChannel[] GetAudioChannels()
        {
            List<AudioChannel> channels = new();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                var fcs = GetAudioChannels(scene);
                foreach (var fc in fcs)
                    channels.Add(fc);
            }
            return channels.ToArray();
        }

        public static AudioChannel[] GetAudioChannels(Scene scene)
        {
            List<AudioChannel> channels = new();
            foreach (var root in scene.GetRootGameObjects())
            {
                var channel = Finder.FindComponent<AudioChannel>(root);
                if (channel != null) channels.Add(channel);
            }
            return channels.ToArray();
        }

        public uint SequentialIndex = 0;
        public GameObject ChoiceSpawn(List<GameObject> spawns = null, SpawnType type = SpawnType.None)
        {
            spawns ??= GetSpawns();
            if (type == SpawnType.None) type = GetSpawnType();
            if (spawns.Count == 0) return gameObject;
            var gameobject = type switch
            {
                SpawnType.First => spawns[0],
                SpawnType.Random => spawns[Random.Range(0, spawns.Count)],
                SpawnType.Free => spawns.FirstOrDefault(spawn => !spawn.activeSelf),
                SpawnType.Sequential => spawns[(int)(SequentialIndex++ % spawns.Count)],
                _ => null,
            };
            return gameobject == null ? gameObject : gameobject;
        }

#if UNITY_EDITOR

        public bool IsCompiled => data_Type != DescriptorType.None;
        public virtual void Compile()
        {
            var netSet = EstimateNetworkObjects();
            foreach (var entry in netSet)
                entry.Value.networkId = entry.Key;
            NetworkObjects = netSet.Values.ToList();
            data_Type = DescriptorType.Main;
            data_NetworkObjects = NetworkObjects.ToArray();
            data_Spawns = EstimateSpawns().Values.ToArray();
            data_SpawnType = SpawnType;
            data_RespawnHeight = RespawnHeight;

            data_audios = EstimateAudioChannels().Values.ToArray();
        }

        public List<NetworkObject> NetworkObjects = new();
        public List<GameObject> Spawns = new();
        public List<AudioChannelDescriptor> AudioChannels = new();
        public SpawnType SpawnType = SpawnType.First;
        public double RespawnHeight = -100;

        private T choice<T>(T a, T b) => a == null ? b : a;
        public Dictionary<ushort, NetworkObject> EstimateNetworkObjects()
        {
            var netSet = new HashSet<NetworkObject>();
            foreach (var obj in NetworkObjects)
                if (obj != null && !netSet.Contains(obj)) netSet.Add(obj);
            var networkObjectsDict = new Dictionary<ushort, NetworkObject>();
            for (ushort i = 0; i < netSet.Count; i++)
            {
                ushort estimatedId = choice(netSet.ToArray()[i].networkId, i);
                while (networkObjectsDict.ContainsKey(choice(estimatedId, i))) estimatedId++;
                networkObjectsDict.Add(choice(estimatedId, (ushort)(i + 1)), netSet.ToArray()[i]);
            }
            return networkObjectsDict;
        }

        public Dictionary<byte, GameObject> EstimateSpawns()
        {
            var spawnSet = new HashSet<GameObject> { gameObject };
            foreach (var spawnObj in Spawns)
                if (spawnObj != null && !spawnSet.Contains(spawnObj)) spawnSet.Add(spawnObj);
            var spawnsDict = new Dictionary<byte, GameObject>();
            for (byte i = 0; i < spawnSet.Count; i++)
            {
                byte estimatedId = i;
                while (spawnsDict.ContainsKey(estimatedId)) estimatedId++;
                spawnsDict.Add(estimatedId, spawnSet.ToArray()[i]);
            }
            return spawnsDict;
        }

        public Dictionary<string, AudioChannelDescriptor> EstimateAudioChannels()
        {
            var audioSet = new HashSet<AudioChannelDescriptor>();
            foreach (var audio in AudioChannels)
                if (audio != null && !audioSet.Contains(audio)) audioSet.Add(audio);
            var audioDict = new Dictionary<string, AudioChannelDescriptor>();
            for (byte i = 0; i < audioSet.Count; i++)
            {
                string estimatedId = choice(audioSet.ToArray()[i].id, i.ToString());
                while (audioDict.ContainsKey(estimatedId)) estimatedId += "_";
                audioDict.Add(estimatedId, audioSet.ToArray()[i]);
            }
            return audioDict;
        }

        public List<NetworkObject> GetNetworkObjects()
        {
            if (IsCompiled) return (data_NetworkObjects ?? new NetworkObject[0]).ToList();
            return NetworkObjects;
        }

        public List<GameObject> GetSpawns()
        {
            if (IsCompiled) return (data_Spawns ?? new GameObject[0]).ToList();
            return Spawns;
        }

        public SpawnType GetSpawnType()
        {
            if (IsCompiled) return data_SpawnType;
            return SpawnType;
        }

        public double GetRespawnHeight()
        {
            if (IsCompiled) return data_RespawnHeight;
            return RespawnHeight;
        }

        public List<AudioChannelDescriptor> GetAudioChannelDescriptors()
        {
            if (IsCompiled) return (data_audios ?? new AudioChannelDescriptor[0]).ToList();
            return AudioChannels;
        }

        public virtual void OnDrawGizmos()
        {
            if (UnityEditor.Selection.activeGameObject != gameObject
                && !InputSystem.GetDevice<Keyboard>().leftShiftKey.isPressed)
            {
                var isSpawnSelected = false;
                foreach (var spawn in Spawns)
                    if (UnityEditor.Selection.activeGameObject == spawn)
                    {
                        isSpawnSelected = true;
                        break;
                    }
                if (!isSpawnSelected) return;
            }

            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, 0.1f);

            Gizmos.color = new Color(
                Color.yellow.r,
                Color.yellow.g,
                Color.yellow.b,
                0.1f
            );
            var cameraPosition = UnityEditor.SceneView.currentDrawingSceneView.camera.transform.position;
            var size = 10;
            Gizmos.DrawCube(
                new Vector3(
                cameraPosition.x,
                (float)RespawnHeight,
                cameraPosition.z
            ), new Vector3(size, 0, size));

            var spawns = GetSpawns();
            foreach (var spawn in spawns)
            {
                if (spawn == null) continue;
                Gizmos.color = Color.white;
                Gizmos.DrawLine(transform.position, spawn.transform.position);

                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(spawn.transform.position, 0.5f);

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(spawn.transform.position, spawn.transform.position + spawn.transform.forward);
            }
        }
#else
        public List<NetworkObject> GetNetworkObjects() => ( data_NetworkObjects ?? new NetworkObject[0]).ToList();
        public List<GameObject> GetSpawns() => ( data_Spawns ?? new GameObject[0]).ToList();
        public SpawnType GetSpawnType() => data_SpawnType == SpawnType.None ? SpawnType.First : data_SpawnType;
        public double GetRespawnHeight() => data_RespawnHeight;
        public List<AudioChannelDescriptor> GetAudioChannelDescriptors() => (data_audios ?? new AudioChannelDescriptor[0]).ToList();
#endif
    }

    public enum SpawnType
    {
        None = 0,
        First = 1,
        Random = 2,
        Free = 3,
        Sequential = 4
    }

    public enum DescriptorType
    {
        None = 0,
        Main = 1,
        Sub = 2
    }

    public class AudioChannelDescriptor
    {
        public string id;
        public string title_key;
        public bool is_world_channel;
    }
}
