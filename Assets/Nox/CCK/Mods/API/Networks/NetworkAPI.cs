using Cysharp.Threading.Tasks;
using Nox.CCK.Servers;
using Nox.CCK.Users;
using Nox.CCK.Worlds;
using Nox.CCK.Instances;
using UnityEngine;

namespace Nox.CCK.Mods.Networks
{
    public interface NetworkAPI
    {
        public UserMe GetCurrentUser();
        public Server GetCurrentServer();
        public NetworkAPIWorld WorldAPI { get; }
        public NetworkAPIUser UserAPI { get; }
        public NetworkAPIServer ServerAPI { get; }
        public NetworkAPIInstance InstanceAPI { get; }
        public UniTask<Texture2D> FetchTexture(string url);
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
        
        public UniTask<World[]> GetWorlds(string server, uint[] worldIds);
        public UniTask<WorldSearch> SearchWorlds(string server, string query, uint offset = 0, uint limit = 10);
    }

    public interface NetworkAPIServer
    {
        public UniTask<Server> GetMyServer();
        public UniTask<WellKnownServer> GetWellKnown(string address);
        public UniTask<Server> GetServer(string address);
        public UniTask<ServerSearch> SearchServers(string server, string query, uint offset = 0, uint limit = 10);
    }

    public interface NetworkAPIInstance
    {
        public UniTask<Instance> GetInstance(string server, uint instanceId);
        public UniTask<InstanceSearch> SearchInstances(string server, string query, uint offset = 0, uint limit = 10);
    }

    public interface NetworkAPIUser
    {
        public UniTask<UserMe> GetMyUser();
        public UniTask<Response<bool>> GetLogout();
        public UniTask<Response<Login>> PostLogin(string server, string username, string password);
        public UniTask<UserSearch> SearchUsers(string server, string query, uint offset = 0, uint limit = 10);
    }

    [System.Serializable]
    public class UserSearch : ShareObject
    {
        public User[] users;
        public uint total;
        public uint limit;
        public uint offset;
    }

    [System.Serializable]
    public class WorldSearch : ShareObject
    {
        public World[] worlds;
        public uint total;
        public uint limit;
        public uint offset;
    }

    [System.Serializable]
    public class ServerSearch : ShareObject
    {
        public Server[] servers;
        public uint total;
        public uint limit;
        public uint offset;
    }

    [System.Serializable]
    public class InstanceSearch : ShareObject
    {
        public Instance[] instances;
        public uint total;
        public uint limit;
        public uint offset;
    }



    [System.Serializable]
    public class Login : ShareObject
    {
        public string token;
        public UserMe user;
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