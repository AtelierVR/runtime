using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.network.HTTP
{
    public class Request
    {
        public Method Method;
        public Uri Url;
        internal UnityWebRequest RequestObject;

        public Request(string url) : this(new Uri(url)) { }
        public Request(Uri url) : this(Method.GET, url) { }
        public Request(Method method, string url) : this(method, new Uri(url)) { }
        public Request(Method method, Uri url)
        {
            Method = method;
            Url = url;
        }

        public async UniTask<TRes> Send<TReq, TRes>(TReq body = default, Dictionary<string, string> headers = null)
        {
            Logger.Log($"Fetching [{Method}] {Url}...");
            var t0 = DateTime.Now;
            var req = new UnityWebRequest(Url, Method.ToString()) { downloadHandler = new DownloadHandlerBuffer() };
            foreach (var key in DefaultHeaders)
                req.SetRequestHeader(key.Key, key.Value);
            if (body == null)
            { }
            else if (body is string str)
                req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(str));
            else if (body is byte[] bytes)
                req.uploadHandler = new UploadHandlerRaw(bytes);
            else if (body is WWWForm form)
            {
                req.uploadHandler = new UploadHandlerRaw(form.data);
                foreach (var header in form.headers)
                    req.SetRequestHeader(header.Key, header.Value);
            }
            else if (body is UploadHandler raw)
                req.uploadHandler = raw;
            else
            {
                req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(
                   JsonConvert.SerializeObject(body)
               ));
                req.SetRequestHeader("Content-Type", "application/json");
            }

            if (headers != null)
                foreach (var key in headers)
                    req.SetRequestHeader(key.Key, key.Value);

            RequestObject = null;
            try { await req.SendWebRequest(); }
            catch
            {
                Logger.LogError($"Failed to fetch [{Method}] {Url}");
                Logger.LogError(req.error);
                Logger.LogError(req.downloadHandler.text);
                return default;
            }
            RequestObject = req;
            var t1 = DateTime.Now;
            Logger.Log($"Fetched [{Method}] {Url} in {(t1 - t0).TotalMilliseconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)}ms");

            if (typeof(TRes) == typeof(long))
                return (TRes)(object)StatusCode;
            else if (typeof(TRes) == typeof(string))
                return (TRes)(object)Response;
            else if (typeof(TRes) == typeof(bool))
                return (TRes)(object)!IsError;
            else if (typeof(TRes) == typeof(UnityWebRequest))
                return (TRes)(object)RequestObject;
            else if (typeof(TRes) == typeof(byte[]))
                return (TRes)(object)RequestObject.downloadHandler.data;
            else if (typeof(TRes) == typeof(DownloadHandler))
                return (TRes)(object)RequestObject.downloadHandler;
            return JsonUtility.FromJson<TRes>(Response);
        }
        public bool IsError => RequestObject?.responseCode != 200;
        public string Response => RequestObject?.downloadHandler.text;
        public long StatusCode => RequestObject?.responseCode ?? 0;

        static readonly Dictionary<string, string> DefaultHeaders = new() {
            { "User-Agent", $"{Application.productName}/{Application.version} (Client)" },
        };

        public static string MergeUrl(Uri url, string path) => MergeUrl(url.ToString(), path);
        public static string MergeUrl(string url, string path)
        {
            if (url.EndsWith("/"))
                url = url.Substring(0, url.Length - 1);
            if (path.StartsWith("/"))
                path = path.Substring(1);
            return $"{url}/{path}";
        }
    }

    public enum Method
    {
        GET,
        POST,
        PUT,
        DELETE
    }
}