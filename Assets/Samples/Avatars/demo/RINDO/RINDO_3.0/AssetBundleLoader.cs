using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetBundleLoader : MonoBehaviour
{
    public string path;
    
    void OnValidate()
    {
        if (path.StartsWith("\"") && path.EndsWith("\""))
            path = path.Substring(1, path.Length - 2);
    }
    
    void Start()
    {
        var request = AssetBundle.LoadFromFile(path);
        
        foreach (var assetName in request.GetAllAssetNames())
        {
            if (!assetName.EndsWith(".prefab")) continue;
            var prefab = request.LoadAsset<GameObject>(assetName);
            Instantiate(prefab);
            break;
        }
    }
}
