using Nox.Network;
using Nox.Network.Instances;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Nox.Events;
using Nox.UI;

namespace Nox
{
    public class Main : MonoBehaviour
    {        
        public static Main Instance { get; private set; }

        public void Start()
        {
            EventEmitter.Clear();
            DontDestroyOnLoad(gameObject);
            Instance = this;
            Init().Forget();
        }

        private void Update()
        {
        }

        private static async UniTaskVoid Init()
        {
        
        }
    }
}