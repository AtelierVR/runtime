using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.network.Utils
{
    public class Gateway
    {
        public const ushort DefaultPortMaster = 53032;
        public const string SrvMaster = "_noxmaster._{1}.{0}";

        public static async UniTask<Uri> FindGatewayMaster(string address)
        {
            if (string.IsNullOrEmpty(address)) return null;
            var host = address.Split(':');
            var uriType = Uri.CheckHostName(host[0]);
            if (uriType is UriHostNameType.IPv4 or UriHostNameType.IPv6)
            {
                var uri = new Uri($"tcp://{address}");
                if (uri.Port == -1) uri = new Uri($"tcp://{address}:{DefaultPortMaster}");
                Logger.LogDebug($"Finding gateway for {uri.Host}:{uri.Port} (IP)");
                var fmg = await FindGm($"{uri.Host}:{uri.Port}", true);
                return fmg != null ? fmg : null;
            }

            if (host[0] == "localhost")
            {
                var uri = new Uri($"tcp://{address}");
                if (uri.Port == -1) uri = new Uri($"tcp://{address}:{DefaultPortMaster}");
                Logger.LogDebug($"Finding gateway for {uri.Host}:{uri.Port} (localhost)");
                var fmg = await FindGm($"{uri.Host}:{uri.Port}", true);
                return fmg != null ? fmg : null;
            }

            if (uriType == UriHostNameType.Dns)
            {
                var uri = new Uri($"tcp://{address}");
                if (uri.Port == -1) uri = new Uri($"tcp://{address}:{DefaultPortMaster}");
                var fmg = await FindGm($"{uri.Host}:{uri.Port}");
                Logger.LogDebug($"Finding gateway for {uri.Host}:{uri.Port} (DNS)");
                if (fmg != null)
                {
                    Logger.LogDebug($"{fmg.Host}:{fmg.Port} (DNS)");
                    return fmg;
                }
                var srv = await FindSrv(uri.Host, SrvMaster);
                if (srv.Length <= 0)
                {
                    Logger.LogError($"Failed to find SRV record for {uri.Host}");
                    return null;
                }
                foreach (var answer in srv)
                {
                    var fmg2 = await FindGm($"{answer.GetTarget()}:{answer.GetPort()}");
                    if (fmg2 != null) return fmg2;
                }
            }

            return null;
        }

        private static async UniTask<SrvAnswer[]> FindSrv(string domain, string service, string protocol = "tcp")
        {
            try
            {
                Logger.Log($"https://dns.google/resolve?name={string.Format(service, domain)}&type=SRV");
                var req = new UnityWebRequest(
                    $"https://dns.google/resolve?name={string.Format(service, domain, protocol)}&type=SRV",
                    UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer() };
                await req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    Logger.Log(req.downloadHandler.text);
                    var srv = JsonUtility.FromJson<Srv>(req.downloadHandler.text);
                    if (srv.Status != 0) return Array.Empty<SrvAnswer>();
                    return srv.Answer ?? Array.Empty<SrvAnswer>();
                }
            }
            catch (UriFormatException)
            {
                return Array.Empty<SrvAnswer>();
            }
            catch
            {
                return Array.Empty<SrvAnswer>();
            }

            return Array.Empty<SrvAnswer>();
        }


        private static async UniTask<Uri> FindGm(string domain, bool forceHttp = false)
        {
            var protos = forceHttp ? new[] { "http" } : new[] { "https", "http" };
            foreach (var protocol in protos)
                try
                {
                    var uri = new Uri($"{protocol}://{domain}/.well-known/nox");
                    Logger.LogDebug($"Finding for {uri}");
                    var req = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbGET)
                        { downloadHandler = new DownloadHandlerBuffer() };
                    await req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success)
                        return new Uri($"{protocol}://{domain}");
                }
                catch (UriFormatException)
                {
                    return null;
                }
                catch (Exception e)
                {
                    Logger.LogError($"Error finding {protocol}://{domain}/");
                    Logger.LogException(e);
                }

            Logger.LogError($"Failed to find gateway for {domain}");
            return null;
        }
    }

    [Serializable]
    public class Srv
    {
        public int Status;
        public bool TC;
        public bool RD;
        public bool RA;
        public bool AD;
        public bool CD;
        public SrvQuestion[] Question;
        public SrvAnswer[] Answer;
        public string Comment;
    }

    [Serializable]
    public class SrvQuestion
    {
        public string name;
        public int type;
    }

    [Serializable]
    public class SrvAnswer
    {
        public string name;
        public int type;
        public int TTL;
        public string data;

        public string[] ToDataArray() => data.Split(' ');

        public ushort GetPriority() => ushort.Parse(ToDataArray()[0]);
        public ushort GetWeight() => ushort.Parse(ToDataArray()[1]);
        public ushort GetPort() => ushort.Parse(ToDataArray()[2]);
        public string GetTarget() => ToDataArray()[3].TrimEnd('.');
    }


    [Serializable]
    public class Response<T>
    {
        public T data;
        public ResponseError error;
        public bool IsError => error != null && error.code != 0 || data == null;
    }

    [Serializable]
    public class ResponseError
    {
        public string message;
        public ushort code;
        public ushort status;
    }
}