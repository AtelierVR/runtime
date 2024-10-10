using System;
using System.Collections.Generic;
using api.nox.network.Utils;
using Cysharp.Threading.Tasks;
using Nox.CCK;
using Nox.CCK.Mods;
using UnityEngine;
using UnityEngine.Networking;

namespace api.nox.network
{
    public class InstanceAPI : ShareObject, IDisposable
    {
        private readonly NetworkSystem _mod;

        internal Server server;

        internal InstanceAPI(NetworkSystem mod) => _mod = mod;

        public async UniTask<Instance> GetInstance(string server, uint instanceId)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return null;
            Debug.Log("Getting instance");
            var config = Config.Load();
            var gateway = server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/instances/{instanceId}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
            var token = await _mod._auth.GetToken(server);
            if (token != null) req.SetRequestHeader("Authorization", token.ToHeader());
            try { await req.SendWebRequest(); }
            catch { return null; }
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<Instance>>(req.downloadHandler.text);
            if (res.IsError) return null;
            res.data.networkSystem = _mod;
            SetOrAddInstance(res.data);
            _mod._api.EventAPI.Emit(new NetEventContext("instance_fetch", res.data));
            return res.data;
        }

        public async UniTask<InstanceSearch> SearchInstances(SearchInstanceData data)
        {
            // GET /api/instances/search?query={query}&offset={offset}&limit={limit}
            var User = _mod.GetCurrentUser();
            var config = Config.Load();
            var gateway = data.server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(data.server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/instances/search?{data.ToParams()}", "GET") { downloadHandler = new DownloadHandlerBuffer() };
            var token = await _mod._auth.GetToken(data.server);
            if (token != null) req.SetRequestHeader("Authorization", token.ToHeader());
            try { await req.SendWebRequest(); }
            catch { return null; }
            Debug.Log(req.downloadHandler.text);
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<InstanceSearch>>(req.downloadHandler.text);
            if (res.IsError) return null;
            res.data.netSystem = _mod;
            foreach (var instance in res.data.instances)
            {
                instance.networkSystem = _mod;
                SetOrAddInstance(instance);
            }
            _mod._api.EventAPI.Emit(new NetEventContext("network.search.instances", server, res.data));
            foreach (var instance in res.data.instances)
                _mod._api.EventAPI.Emit(new NetEventContext("instance_fetch", instance));
            return res.data;
        }

        public async UniTask<Instance> CreateInstance(CreateInstanceData data)
        {
            var User = _mod.GetCurrentUser();
            if (User == null) return null;
            var config = Config.Load();
            var gateway = data.server == User?.server ? config.Get<string>("gateway") : (await Gateway.FindGatewayMaster(data.server))?.OriginalString;
            if (gateway == null) return null;
            var req = new UnityWebRequest($"{gateway}/api/instances", "PUT") { downloadHandler = new DownloadHandlerBuffer() };
            var token = await _mod._auth.GetToken(data.server);
            if (token != null) req.SetRequestHeader("Authorization", token.ToHeader());
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(data.ToJSON()));
            req.uploadHandler.contentType = "application/json";
            try { await req.SendWebRequest(); }
            catch { return null; }
            if (req.responseCode != 200) return null;
            var res = JsonUtility.FromJson<Response<Instance>>(req.downloadHandler.text);
            if (res.IsError) return null;
            res.data.networkSystem = _mod;
            _mod._api.EventAPI.Emit(new NetEventContext("network.create.instance", data.server, res.data.id, res.data));
            _mod._api.EventAPI.Emit(new NetEventContext("instance_fetch", res.data));
            SetOrAddInstance(res.data);
            return res.data;
        }

        internal List<Instance> instances = new();

        internal void SetOrAddInstance(Instance instance)
        {
            var i = instances.FindIndex(i => i.server == instance.server && i.id == instance.id);
            if (i == -1) instances.Add(instance);
            else instances[i] = instance;
        }

        public Instance GetInstanceInCache(string server, uint instanceId) => instances.Find(i => i.server == server && i.id == instanceId);

        [ShareObjectExport] public Func<ShareObject, UniTask<ShareObject>> SharedSearchInstances;
        [ShareObjectExport] public Func<string, uint, UniTask<ShareObject>> SharedGetInstance;
        [ShareObjectExport] public Func<ShareObject, UniTask<ShareObject>> SharedCreateInstance;
        [ShareObjectExport] public Func<string, uint, ShareObject> SharedGetInstanceInCache;

        public void BeforeExport()
        {
            SharedSearchInstances = async (data) => await SearchInstances(data.Convert<SearchInstanceData>());
            SharedGetInstance = async (server, instanceId) => await GetInstance(server, instanceId);
            SharedCreateInstance = async data => await CreateInstance(data.Convert<CreateInstanceData>());
            SharedGetInstanceInCache = (server, instanceId) => GetInstanceInCache(server, instanceId);
        }

        public void AfterExport()
        {
            SharedSearchInstances = null;
            SharedGetInstance = null;
            SharedCreateInstance = null;
            SharedGetInstanceInCache = null;

        }

        public void Dispose()
        {
            instances.Clear();
        }
    }
}
