using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

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
            Debug.Log($"Fetching [{Method}] {Url}...");
            var req = new UnityWebRequest(Url, Method.ToString()) { downloadHandler = new DownloadHandlerBuffer() };
            foreach (var key in DefaultHeaders)
                req.SetRequestHeader(key.Key, key.Value);
            if (headers != null)
                foreach (var key in headers)
                    req.SetRequestHeader(key.Key, key.Value);
            if (body == null)
            { }
            else if (body is string str)
                req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(str));
            else if (body is byte[] bytes)
                req.uploadHandler = new UploadHandlerRaw(bytes);
            else if (body is WWWForm form)
                req.uploadHandler = new UploadHandlerRaw(form.data);
            else if (body is UploadHandler raw)
                req.uploadHandler = raw;
            else req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(
                   JsonConvert.SerializeObject(body)
               ))
            { contentType = "application/json" };

            RequestObject = null;
            try { await req.SendWebRequest(); }
            catch
            {
                Debug.LogError($"Failed to fetch [{Method}] {Url}");
                Debug.LogError(req.error);
                Debug.LogError(req.downloadHandler.text);
                return default;
            }
            RequestObject = req;

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

        static Dictionary<string, string> DefaultHeaders = new Dictionary<string, string> {
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