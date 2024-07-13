using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.game
{
    internal class NavigationHandler : ShareObject
    {
        public string id;
        public string text_key;
        public string title_key;
        public Func<NavigationWorker[]> GetWorkers;
    }

    internal class NavigationWorker : ShareObject
    {
        public string server_address;
        public string server_title;
        public Func<string, UniTask<NavigationResult>> Fetch;
    }

    internal class NavigationResult : ShareObject
    {
        public string error;
        public NavigationResultData[] data;
    }

    internal class NavigationResultData : ShareObject
    {
        public string title;
        public string imageUrl;
        public string goto_id;
        public object[] goto_data;
    }
}