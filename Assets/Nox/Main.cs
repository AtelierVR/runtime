using Nox.Network;
using Nox.Network.Instances;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Nox.CCK;
using Nox.Users;
using System.Threading.Tasks;
using Nox.Worlds;
using Nox.Events;
using Nox.UI;

namespace Nox
{
    public class Main : MonoBehaviour
    {
        public Autohand.AutoHandPlayer player;
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
            RelayManager.Update();
            ConnectorManager.Update();
            InstanceManager.Update();
            EventEmitter.Emit("update", Time.deltaTime);
        }

        private static async UniTaskVoid Init()
        {
            EventEmitter.Emit<object>("start");

            NavigationElement.Create("Test", null);

            // return;

            // var config = Config.Load();
            // await Goto.GotoCache();
            // var userMe = await UserManager.FetchMe();
            // var world = userMe.HomePatern();
            // var asset = await world.Asset();
            // if (asset != null && !asset.is_empty)
            // {
            //     var dl = await asset.Download();
            //     if (dl)
            //     {
            //         config.Set("fallback_scene", asset.hash);
            //         config.Save();
            //         await Goto.GotoCache(asset.hash);
            //     }
            // }

            // await Task.Delay(5000);

            // var world2 = await WorldManager.GetOrFetch(1, userMe.server);
            // if (world2 != null)
            // {
            //     var asset2 = WorldAsset.From(world2.GetAsset(ushort.MaxValue));
            //     if (asset2 != null && !asset2.is_empty)
            //     {
            //         var dl2 = await asset2.Download();
            //         if (dl2)
            //         {
            //             await Goto.GotoCache(asset2.hash);
            //         }
            //     }
            // }



        }
    }
}