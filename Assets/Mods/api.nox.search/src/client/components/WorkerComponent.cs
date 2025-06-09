// using System;
// using System.Collections.Generic;
// using Nox.CCK.Language;
// using Nox.CCK.Utils;
// using UnityEngine;
// using Logger = Nox.CCK.Utils.Logger;
// using Transform = UnityEngine.Transform;
//
// namespace api.nox.search.client
// {
//     public class WorkerComponent : MonoBehaviour
//     {
//         public TextLanguage title;
//         public TextLanguage message;
//         internal SearchPage.WorkerTask Task;
//         public SearchComponent search;
//         public RectTransform resultContainer;
//
//         public GridFitter fitter;
//
//         public void Initiate(SearchComponent s, SearchPage.WorkerTask task)
//         {
//             search = s;
//             UpdateData(task);
//         }
//
//         private void OnDestroy() => Task = null;
//         private void Awake() => UpdateData(Task);
//
//
//         public void UpdateData(SearchPage.WorkerTask task)
//         {
//             Task = task;
//             if (Task == null)
//             {
//                 Logger.LogError("WorkerComponent: Task is not initiated.");
//                 return;
//             }
//             
//             Logger.LogDebug($"WorkerComponent: UpdateData {Task.Worker.ServerTitle} {Task.Status}");
//
//             title.UpdateText("search.worker.title", new[] { Task.Worker.ServerTitle, Task.Status.ToString() });
//             message.UpdateText(Task.MessageKey, Task.MessageArgs);
//             if (Task.Status == SearchPage.WorkerTaskStatus.Completed)
//             {
//                 List<int> ids = new();
//                 var datas = Task.Result?.Data ?? Array.Empty<ResultData>();
//                 fitter.ratio = Task.Result?.Ratio ?? 1f;
//                 foreach (Transform tf in resultContainer)
//                 {
//                     var result = tf.GetComponent<ResultComponent>();
//                     if (!result)
//                     {
//                         Destroy(tf.gameObject);
//                         continue;
//                     }
//
//                     var id = result.Data.Id;
//                     var data = Array.Find(datas, d => d.Id == id);
//                     if (data == null)
//                     {
//                         Destroy(tf.gameObject);
//                         continue;
//                     }
//
//                     ids.Add(id);
//                     result.UpdateData(data);
//                 }
//
//                 // add missing results
//                 foreach (var data in datas)
//                 {
//                     if (ids.Contains(data.Id)) continue;
//                     var asset = Main.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/result.prefab");
//                     asset.SetActive(false);
//                     var content = Instantiate(asset, resultContainer);
//                     var component = content.GetComponent<ResultComponent>();
//                     component.Initiate(this, data);
//                     content.name = $"{data.Id}_{content.name}";
//                     content.SetActive(true);
//                     ids.Add(data.Id);
//                 }
//
//                 resultContainer.gameObject.SetActive(true);
//                 message.gameObject.SetActive(false);
//             }
//             else
//             {
//                 resultContainer.gameObject.SetActive(false);
//                 message.gameObject.SetActive(true);
//             }
//
//             UpdateLayout.UpdateManually(transform as RectTransform);
//         }
//     }
// }