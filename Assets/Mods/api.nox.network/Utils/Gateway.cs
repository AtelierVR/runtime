using System;
using api.nox.network.Relays;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace api.nox.network.Utils
{
    public class Gateway
    {
        public const ushort DefaultPortMaster = 53032;
        public const string SRVMaster = "_noxmaster._tcp.{0}";
        public static async UniTask<Uri> FindGatewayMaster(string address)
        {
            if (string.IsNullOrEmpty(address)) return null;
            var host = address.Split(':');
            UriHostNameType uriType = Uri.CheckHostName(host[0]);
            if (uriType == UriHostNameType.IPv4 || uriType == UriHostNameType.IPv6)
            {
                var uri = new Uri($"tcp://{address}");
                if (uri.Port == -1) uri = new Uri($"tcp://{address}:{DefaultPortMaster}");
                var fmg = await FindGM($"{uri.Host}:{uri.Port}", true);
                if (fmg != null) return fmg;
                return null;
            }

            if (host[0] == "localhost")
            {
                var uri = new Uri($"tcp://{address}");
                if (uri.Port == -1) uri = new Uri($"tcp://{address}:{DefaultPortMaster}");
                var fmg = await FindGM($"{uri.Host}:{uri.Port}", true);
                if (fmg != null) return fmg;
                return null;
            }

            else if (uriType == UriHostNameType.Dns)
            {
                var uri = new Uri($"tcp://{address}");
                if (uri.Port == -1) uri = new Uri($"tcp://{address}:{DefaultPortMaster}");
                var fmg = await FindGM($"{uri.Host}:{uri.Port}");
                if (fmg != null) return fmg;
                var srv = await FindSRV(uri.Host, SRVMaster);
                if (srv.Length > 0)
                    foreach (var answer in srv)
                    {
                        var fmg2 = await FindGM($"{answer.GetTarget()}:{answer.GetPort()}");
                        if (fmg2 != null) return fmg2;
                    }
            }
            return null;
        }

        public const ushort DefaultPortRelay = 54032;
        public const string SRVRelay = "_noxrelay._tcp.{0}";

        public static async UniTask<Uri> FindGatewayRelay(string address)
        {
            if (string.IsNullOrEmpty(address)) return null;
            var host = address.Split(':');
            UriHostNameType uriType = Uri.CheckHostName(host[0]);
            if (uriType == UriHostNameType.IPv4 || uriType == UriHostNameType.IPv6)
            {
                var uri = new Uri($"tcp://{address}");
                if (uri.Port == -1) uri = new Uri($"tcp://{address}:{DefaultPortRelay}");
                var fmg = await FindGR(uri.Host, (ushort)uri.Port);
                if (fmg != null) return fmg;
                return null;
            }

            if (host[0] == "localhost")
            {
                var uri = new Uri($"tcp://{address.Replace("localhost", "127.0.0.1")}");
                if (uri.Port == -1) uri = new Uri($"tcp://{address}:{DefaultPortRelay}");
                var fmg = await FindGR(uri.Host, (ushort)uri.Port);
                if (fmg != null) return fmg;
                return null;
            }

            else if (uriType == UriHostNameType.Dns)
            {
                var uri = new Uri($"tcp://{address}");
                if (uri.Port == -1) uri = new Uri($"tcp://{address}:{DefaultPortRelay}");
                var fmg = await FindGR(uri.Host, (ushort)uri.Port);
                if (fmg != null) return fmg;
                var srv = await FindSRV(uri.Host, SRVRelay);
                if (srv.Length > 0)
                    foreach (var answer in srv)
                    {
                        var fmg2 = await FindGR(answer.GetTarget(), answer.GetPort());
                        if (fmg2 != null) return fmg2;
                    }
            }
            return null;
        }

        public static async UniTask<Uri> FindGR(string host, ushort port, bool forceTCP = false)
        {
            var protos = forceTCP ? new[] { RelayProtocol.TCP } : new[] { RelayProtocol.UDP, RelayProtocol.TCP };
            foreach (var protocol in protos)
                try
                {
                    var connector = RelayAPI.ConnectorFromEnum(protocol);
                    if (connector == null) return null;
                    var relay = new Relay(connector);
                    Debug.Log($"Connecting to {host}:{port} with {protocol}");
                    if (relay.Connect(host, port))
                    {
                        var handshake = await relay.RequestHandshake();
                        if (handshake != null)
                        {
                            relay.Dispose();
                            return new Uri($"{protocol}://{host}:{port}");
                        }
                    }
                }
                catch (UriFormatException) { return null; }
                catch (Exception e)
                {
                    Debug.Log(e);
                    continue;
                }
            return null;
        }

        public static async UniTask<SRVAnswer[]> FindSRV(string domain, string service)
        {
            try
            {
                Debug.Log($"https://dns.google/resolve?name={string.Format(service, domain)}&type=SRV");
                var req = new UnityWebRequest($"https://dns.google/resolve?name={string.Format(service, domain)}&type=SRV", UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer() };
                await req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log(req.downloadHandler.text);
                    var srv = JsonUtility.FromJson<SRV>(req.downloadHandler.text);
                    if (srv.Status != 0) return new SRVAnswer[0];
                    return srv.Answer;
                }
            }
            catch (UriFormatException) { return new SRVAnswer[0]; }
            catch { return new SRVAnswer[0]; }
            return new SRVAnswer[0];
        }


        private static async UniTask<Uri> FindGM(string domain, bool forceHTTP = false)
        {
            var protos = forceHTTP ? new[] { "http" } : new[] { "https", "http" };
            foreach (var protocol in protos)
                try
                {
                    var uri = new Uri($"{protocol}://{domain}/.well-known/nox");
                    var req = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer() };
                    await req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success)
                        return new Uri($"{protocol}://{domain}");
                }
                catch (UriFormatException) { return null; }
                catch (Exception e)
                {
                    Debug.Log(e);
                    continue;
                }
            return null;
        }
    }

    [Serializable]
    public class SRV
    {
        public int Status;
        public bool TC;
        public bool RD;
        public bool RA;
        public bool AD;
        public bool CD;
        public SRVQuestion[] Question;
        public SRVAnswer[] Answer;
        public string Comment;
    }

    [Serializable]
    public class SRVQuestion
    {
        public string name;
        public int type;
    }

    [Serializable]
    public class SRVAnswer
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