using System;
using Nox.CCK.Build;
using UnityEngine;
using UnityEngine.Serialization;

namespace Nox.CCK.Worlds
{
	public abstract class BaseSceneDescriptor : MonoBehaviour, ICompilable
	{
		public GameObject[] spawns;
		public SpawnType spawnType = SpawnType.Random;
		public int spawnIndex = 0;

		public SpawnType GetSpawnType()
			=> spawnType;

#if UNITY_EDITOR
		public GameObject[] GetSpawns() {
			var sp = spawns ?? Array.Empty<GameObject>();
			return sp.Length > 0 ? sp : new[] { gameObject };
		}
#else
		public GameObject[] GetSpawns()
			=> spawns ?? Array.Empty<GameObject>();
#endif
	}
}