using System;
using Cysharp.Threading.Tasks;

namespace api.nox.game.Tiles
{
    public class NavigationHandler 
    {
        public string id;
        public string text_key;
        public string title_key;
        public Func<NavigationWorker[]> GetWorkers;
    }

    public class NavigationWorker
    {
        public string server_address;
        public string server_title;
        public Func<string, UniTask<NavigationResult>> Fetch;
    }

    public class NavigationResult
    {
        public string error;
        public NavigationResultData[] data;
    }

    public class NavigationResultData
    {
        public string title;
        public string imageUrl;
        public string goto_id;
        public object[] goto_data;
    }

    
    [Serializable]
    public class NavigationWorkerInfo
    {
        public string address;
        public string title;
        public string[] features;
        public bool navigation;
    }
}