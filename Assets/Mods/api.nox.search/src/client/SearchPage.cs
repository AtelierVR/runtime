using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Events;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;
using Transform = UnityEngine.Transform;

namespace api.nox.search.client
{
    public class SearchPage
    {
        private static string GetKey() => "search";
        private static EventSubscription _listener;

        internal readonly UnityEvent<WorkerTask> OnWorkerTaskUpdate = new();
        internal readonly UnityEvent<WorkerTask[]> OnWorkerTaskStart = new();
        internal readonly UnityEvent<Handler[]> OnHandlerUpdate = new();

        public static void Listen()
        {
            Logger.LogDebug("SearchPage.Listen");
            _listener = SearchSystem.CoreAPI.EventAPI.Subscribe("goto_page", OnGotoEvent);
        }

        public static void StopListen()
        {
            SearchSystem.CoreAPI.EventAPI.Unsubscribe(_listener);
        }

        private static void OnGotoEvent(EventData context)
        {
            if (!context.TryGet(0, out int menuId)) return;
            if (!context.TryGet(1, out string pageKey)) return;
            if (pageKey != GetKey()) return;
            var handler = !context.TryGet(2, out string h)
                ? Config.Load().Get<string>("search.last_handler")
                : h;
            var query = context.TryGet(3, out string q) ? q : null;
            var auto = context.TryGet(4, out bool a) && a;
            var page = new SearchPage
            {
                MenuId = menuId,
                HandlerId = handler,
                Query = query ?? string.Empty,
                LastQuery = (query ?? string.Empty) + " ",
            };
            page.Display();
            if (auto) page.Submit().Forget();
        }

        private void Display()
            => SearchSystem.CoreAPI.EventAPI.Emit("display_page", MenuId, new Dictionary<string, object>
            {
                {
                    "key", GetKey()
                }, // id of the page
                {
                    "content", new Func<Transform, GameObject>(OnContent)
                }, // called when the menu need the content of the page (first call)
                /*
                 {
                    "open", new Action<string, GameObject>(OnOpen)
                }, // called once when the page is display for the first time
                 {
                    "restore", (string key, GameObject go) => OnRestore(key, go)
                }, // called when the menu go back from history and display the page again
                {
                    "remove", (GameObject go) => OnRemove(go)
                }, // called when the menu remove the page from history (last call)
                {
                    "display", (string key, GameObject go) => OnDisplay(key, go)
                }, // called when the page is displayed
                {
                    "hide", (string key, GameObject go) => OnHide(key, go)
                } // called when another page is displayed
                */
            });

        internal int MenuId;
        private SearchComponent _comportment;
        private string _handlerId;

        internal string HandlerId
        {
            get => _handlerId;
            set
            {
                _handlerId = value;
                var config = Config.Load();
                config.Set("search.last_handler", value);
                config.Save();
            }
        }

        internal string Query = string.Empty;
        internal string LastQuery = string.Empty + " ";

        internal bool IsEmptyQuery => string.IsNullOrEmpty(Query);
        internal bool IsNewQuery => Query != LastQuery;

        internal Handler Handler
        {
            get
            {
                var handler = HandlerId != null
                    ? SearchSystem.Instance.GetHandler(HandlerId)
                    : null;
                handler ??= SearchSystem.Instance.Handlers.FirstOrDefault();
                return handler;
            }
            set
            {
                HandlerId = value?.Id;
                LastQuery = null;
            }
        }


        private GameObject OnContent(Transform transform)
        {
            var asset = SearchSystem.CoreAPI.AssetAPI.GetAsset<GameObject>("prefabs/content.prefab");
            asset.SetActive(false);
            var content = Object.Instantiate(asset, transform);
            _comportment = content.GetComponent<SearchComponent>();
            _comportment.Initiate(this);
            _comportment.UpdateData();
            Logger.LogDebug($"SearchPage.OnGetContent: {asset.name} {content.name}");
            content.name = $"{GetKey()}_{content.name}";
            return content;
        }

        internal bool IsFetching
            => _tasks.Count > 0
               && _tasks.Any(t => t.Status is WorkerTaskStatus.Fetching or WorkerTaskStatus.Pending);

        private readonly List<WorkerTask> _tasks = new();

        internal void Cancel()
        {
            foreach (var cancel in _tasks)
                cancel.Cancel();
            _tasks.Clear();
        }

        internal async UniTask Submit()
        {
            if (IsFetching) return;
            LastQuery = Query;

            var handler = Handler;
            if (handler == null)
            {
                Logger.LogDebug($"No handler found with id {HandlerId}");
                OnWorkerTaskStart.Invoke(Array.Empty<WorkerTask>());
                return;
            }

            if (handler.GetWorkers == null)
            {
                Logger.LogDebug($"No function GetWorkers found for handler {handler.Id}");
                OnWorkerTaskStart.Invoke(Array.Empty<WorkerTask>());
                return;
            }

            var workers = handler.GetWorkers();
            if (workers.Length == 0)
            {
                Logger.LogDebug($"No workers found for handler {handler.Id}");
                OnWorkerTaskStart.Invoke(Array.Empty<WorkerTask>());
                return;
            }

            Cancel();

            foreach (var worker in workers.Where(w => w != null))
                _tasks.Add(new WorkerTask()
                {
                    Worker = worker,
                    Data = new Dictionary<string, object> { { "query", Query } },
                    CancellationToken = new CancellationTokenSource(),
                    Timeout = 10d,
                });

            OnWorkerTaskStart.Invoke(_tasks.ToArray());
            await UniTask.WhenAll(_tasks.Select(t => t.Execute(this)));
        }

        internal enum WorkerTaskStatus
        {
            Pending,
            Fetching,
            Canceled,
            CompletedWithoutResult,
            Completed,
            Faulted
        }

        public class WorkerTask
        {
            internal int Uid = Guid.NewGuid().GetHashCode();

            internal Worker Worker;
            internal double Timeout;
            internal Dictionary<string, object> Data;
            internal CancellationTokenSource CancellationToken;
            internal Result Result;
            internal WorkerTaskStatus Status;
            private DateTime _t0 = DateTime.MinValue;
            private DateTime _t1 = DateTime.MinValue;

            private double Elapsed
                => Status != WorkerTaskStatus.Pending && Status != WorkerTaskStatus.Fetching
                    ? (float)(_t1 - _t0).TotalSeconds
                    : (float)(DateTime.Now - _t0).TotalSeconds;


            internal string MessageKey = string.Empty;
            internal string[] MessageArgs = Array.Empty<string>();

            internal void Cancel() => CancellationToken.Cancel();

            internal async UniTask Execute(SearchPage page)
            {
                if (CancellationToken.Token.IsCancellationRequested)
                {
                    Status = WorkerTaskStatus.Canceled;
                    MessageKey = "search.worker.canceled";
                    MessageArgs = Array.Empty<string>();
                    page.OnWorkerTaskUpdate.Invoke(this);
                    return;
                }

                Status = WorkerTaskStatus.Pending;
                MessageKey = string.Empty;
                MessageArgs = Array.Empty<string>();
                Result = null;
                page.OnWorkerTaskUpdate.Invoke(this);

                _t0 = DateTime.Now;

                if (Worker.Fetch == null)
                {
                    Status = WorkerTaskStatus.Faulted;
                    MessageKey = "search.worker.error.no_fetch";
                    MessageArgs = Array.Empty<string>();
                    page.OnWorkerTaskUpdate.Invoke(this);
                    return;
                }

                var result = Worker.Fetch(Data).AttachExternalCancellation(CancellationToken.Token);

                Status = WorkerTaskStatus.Fetching;
                MessageKey = "search.worker.fetching";
                MessageArgs = Array.Empty<string>();
                page.OnWorkerTaskUpdate.Invoke(this);
                await UniTask.WaitUntil(() => result.Status != UniTaskStatus.Pending || Elapsed > Timeout);

                if (CancellationToken.Token.IsCancellationRequested)
                {
                    Status = WorkerTaskStatus.Canceled;
                    MessageKey = "search.worker.canceled";
                    page.OnWorkerTaskUpdate.Invoke(this);
                    return;
                }

                if (result.Status != UniTaskStatus.Pending)
                    CancellationToken.Cancel();

                switch (result.Status)
                {
                    case UniTaskStatus.Canceled:
                        Status = WorkerTaskStatus.Canceled;
                        MessageKey = "search.worker.canceled";
                        MessageArgs = Array.Empty<string>();
                        page.OnWorkerTaskUpdate.Invoke(this);
                        return;
                    case UniTaskStatus.Faulted:
                        try
                        {
                            await result;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogException(ex);
                            Status = WorkerTaskStatus.Faulted;
                            MessageKey = "search.worker.error";
                            MessageArgs = new[] { ex.Message };
                            page.OnWorkerTaskUpdate.Invoke(this);
                            return;
                        }

                        break;
                    case UniTaskStatus.Succeeded:
                        Logger.LogDebug("WorkerTask: result is succeeded");
                        break;
                    case UniTaskStatus.Pending: // hum ?
                    default:
                        Status = WorkerTaskStatus.CompletedWithoutResult;
                        MessageKey = "search.worker.no_message";
                        MessageArgs = Array.Empty<string>();
                        page.OnWorkerTaskUpdate.Invoke(this);
                        return;
                }

                Result = await result;
                _t1 = DateTime.Now;
                Logger.LogDebug($"WorkerTask: {(_t1 - _t0).TotalMilliseconds}ms");

                if (Result == null)
                {
                    Status = WorkerTaskStatus.Faulted;
                    MessageKey = "search.worker.error.no_result";
                    MessageArgs = Array.Empty<string>();
                    page.OnWorkerTaskUpdate.Invoke(this);
                    return;
                }

                if (!string.IsNullOrEmpty(Result.Error))
                {
                    Status = WorkerTaskStatus.Faulted;
                    MessageKey = "search.worker.error";
                    MessageArgs = new[] { Result.Error };
                    page.OnWorkerTaskUpdate.Invoke(this);
                    return;
                }

                if (Result.Data == null || Result.Data.Length == 0)
                {
                    Status = WorkerTaskStatus.CompletedWithoutResult;
                    MessageKey = "search.worker.empty";
                    MessageArgs = Array.Empty<string>();
                    page.OnWorkerTaskUpdate.Invoke(this);
                    return;
                }

                Status = WorkerTaskStatus.Completed;
                MessageKey = "search.worker.no_message";
                MessageArgs = Array.Empty<string>();
                page.OnWorkerTaskUpdate.Invoke(this);
            }
        }
    }
}