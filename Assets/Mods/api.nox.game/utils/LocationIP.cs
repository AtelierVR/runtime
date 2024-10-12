using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace api.nox.game.LocationIP
{
    // http://ipwho.is/<ip>

    // {"ip":"194.164.206.168","success":true,"type":"IPv4","continent":"Europe","continent_code":"EU","country":"Germany","country_code":"DE","region":"Berlin","region_code":"BE","city":"Berlin","latitude":52.5200066,"longitude":13.404954,"is_eu":true,"postal":"10178","calling_code":"49","capital":"Berlin","borders":"AT,BE,CH,CZ,DK,FR,LU,NL,PL","flag":{"img":"https:\/\/cdn.ipwhois.io\/flags\/de.svg","emoji":"\ud83c\udde9\ud83c\uddea","emoji_unicode":"U+1F1E9 U+1F1EA"},"connection":{"asn":8560,"org":"IONOS SE","isp":"IONOS SE","domain":"ionos.com"},"timezone":{"id":"Europe\/Berlin","abbr":"CEST","is_dst":true,"offset":7200,"utc":"+02:00","current_time":"2024-09-04T20:48:52+02:00"}}

    [System.Serializable]
    public class IPData
    {
        public string ip;
        public bool success;
        public string type;
        public string continent;
        public string continent_code;
        public string country;
        public string country_code;
        public string region;
        public string region_code;
        public string city;
        public float latitude;
        public float longitude;
        public bool is_eu;
        public string postal;
        public string calling_code;
        public string capital;
        public string borders;
        public Flag flag;
        public Connection connection;
        public Timezone timezone;

        public string GetFlagImg() => $"https://raw.githubusercontent.com/hampusborgos/country-flags/main/png100px/{country_code.ToLower()}.png";
    }

    [System.Serializable]
    public class Flag
    {
        public string img;
        public string emoji;
        public string emoji_unicode;
    }

    [System.Serializable]
    public class Connection
    {
        public int asn;
        public string org;
        public string isp;
        public string domain;
    }

    [System.Serializable]
    public class Timezone
    {
        public string id;
        public string abbr;
        public bool is_dst;
        public int offset;
        public string utc;
        public string current_time;
    }

    public class LocationIP
    {
        public static async UniTask<IPData> FetchLocation(string ip)
        {
            UriHostNameType uriType = Uri.CheckHostName(ip);
            switch (uriType)
            {
                case UriHostNameType.IPv4:
                case UriHostNameType.IPv6:
                    break;
                case UriHostNameType.Dns:
                    ip = System.Net.Dns.GetHostAddresses(ip)[0].ToString();
                    break;
                default:
                    return null;
            }

            var url = $"https://ipwho.is/{ip}";
            var request = new UnityEngine.Networking.UnityWebRequest(url)
            { downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer() };
            try { await request.SendWebRequest(); }
            catch (System.Exception e)
            { Debug.Log(e); return null; }
            if (request.responseCode != 200) return null;
            Debug.Log("Localisation " + request.downloadHandler.text);
            return JsonUtility.FromJson<IPData>(request.downloadHandler.text);
        }
    }
}