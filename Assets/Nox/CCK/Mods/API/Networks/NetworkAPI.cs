using Cysharp.Threading.Tasks;
using Nox.CCK.Users;
using Nox.CCK.Worlds;

namespace Nox.CCK.Mods.Networks
{
    public interface NetworkAPI
    {
        public User GetCurrentUser();
        public NetworkAPIWorld WorldAPI { get; }
        public NetworkAPIUser UserAPI { get; }
    }

    public interface NetworkAPIWorld
    {
        public UniTask<World> GetWorld(string server, uint worldId, bool withEmpty = false);
        public UniTask<Asset> GetAsset(string server, uint worldId, uint assetId);
        public UniTask<Asset> UpdateAsset(UpdateAssetData asset);
        public UniTask<bool> DeleteAsset(string server, uint worldId, uint assetId);
        public UniTask<bool> DeleteWorld(string server, uint worldId);
        public UniTask<bool> UploadAssetFile(string server, uint worldId, uint assetId, string path);
        public UniTask<bool> UploadWorldThumbnail(string server, uint worldId, string path);
        public UniTask<Asset> CreateAsset(CreateAssetData asset);
        public UniTask<World> CreateWorld(CreateWorldData world);
        public UniTask<World> UpdateWorld(UpdateWorldData world, bool withEmpty = false);
    }

    public interface NetworkAPIUser
    {
        public UniTask<User> FetchUserMe();
        public UniTask<Response<bool>> FetchLogout();
        public UniTask<Response<Login>> FetchLogin(string server, string username, string password);
    }

    
    [System.Serializable]
    public class Login : ShareObject
    {
        public string token;
        public User user;
    }
    
    [System.Serializable]
    public class UpdateWorldData : ShareObject
    {
        [System.NonSerialized] public string server;
        [System.NonSerialized] public uint id;
        public string title;
        public string description;
        public ushort capacity;
        public string thumbnail;
    }

    [System.Serializable]
    public class CreateWorldData : ShareObject
    {
        [System.NonSerialized] public string server;
        public uint id;
        public string title;
        public string description;
        public ushort capacity;
        public string thumbnail;
    }

    [System.Serializable]
    public class UpdateAssetData : ShareObject
    {
        [System.NonSerialized] public string server;
        [System.NonSerialized] public uint worldId;
        [System.NonSerialized] public uint id;
        public string url;
        public string hash;
        public uint size;
    }

    [System.Serializable]
    public class CreateAssetData : ShareObject
    {
        [System.NonSerialized] public string server;
        [System.NonSerialized] public uint worldId;
        public string platform;
        public string engine;
        public ushort version;
        public uint id;
        public string url;
        public string hash;
        public uint size;
    }
}