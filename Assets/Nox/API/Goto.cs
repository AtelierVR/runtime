using Cysharp.Threading.Tasks;
using Nox.CCK.Worlds;
using Nox.Scenes;

namespace Nox.API
{
    public class Goto
    {
        public static async UniTask<BaseDescriptor> GotoCache(string hash = null)
        {
            var scene = hash == null ? await SceneManager.GotoFallback() : await SceneManager.GotoAssetBundle(hash, 0);
            if (scene == null || !scene.IsValid() || !scene.isLoaded) return null;
            var descriptor = BaseDescriptor.GetDescriptor(scene);
            if (descriptor == null) return null;
            var spawn = descriptor.ChoiceSpawn();
            Main.Instance.player.SetPosition(spawn.transform.position);
            Main.Instance.player.SetRotation(spawn.transform.rotation);
            return descriptor;
        }
    }
}