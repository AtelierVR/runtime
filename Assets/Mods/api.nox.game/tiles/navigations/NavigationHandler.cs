using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods;
using UnityEngine;

namespace api.nox.game
{
    internal class NavigationHandler : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string id;
        [ShareObjectImport, ShareObjectExport] public string text_key;
        [ShareObjectImport, ShareObjectExport] public string title_key;
        [ShareObjectImport, ShareObjectExport] public Func<NavigationWorker[]> GetWorkers;
    }

    internal class NavigationWorker : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string server_address;
        [ShareObjectImport, ShareObjectExport] public string server_title;
        [ShareObjectImport, ShareObjectExport] public Func<string, UniTask<NavigationResult>> Fetch;
    }

    internal class NavigationResult : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string error;
        [ShareObjectImport, ShareObjectExport] public NavigationResultData[] data;
    }

    internal class NavigationResultData : ShareObject
    {
        [ShareObjectImport, ShareObjectExport] public string title;
        [ShareObjectImport, ShareObjectExport] public string imageUrl;
        [ShareObjectImport, ShareObjectExport] public string goto_id;
        [ShareObjectImport, ShareObjectExport] public object[] goto_data;
    }
}